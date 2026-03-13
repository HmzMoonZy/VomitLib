using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using FluentAPI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Twenty2.VomitLib.HotUpdate
{
    /// <summary>
    /// 基于Addressable的热更新处理器
    /// 维护一个BundleSet描述首包资源, 全量更新
    /// </summary>
    public class AddressableHotUpdateHandler : IHotUpdateHandler
    {
        private enum ErrCode
        {
            Success = 0,
            
            // 1xx: 强更相关
            /// <summary>
            /// 检查强更失败
            /// </summary>
            CheckForceUpdateFailed = 101,
            /// <summary>
            /// 版本低, 需要强更
            /// </summary>
            NeedForceUpdate = 102,
            
            // 2xx: 网络/超时
            /// <summary>
            /// 超时错误
            /// </summary>
            TimeOut = 201,
            /// <summary>
            /// 网络无连接
            /// </summary>
            NotReachable = 202,
            
            // 3xx: 用户操作
            /// <summary>
            /// 用户不同意使用移动数据
            /// </summary>
            NotAllowUseMobileData = 301,
            
            // 4xx: 资源更新流程
            /// <summary>
            /// Addressable初始化失败
            /// </summary>
            InitializeFailed = 401,
            /// <summary>
            /// Catalog检查失败
            /// </summary>
            CatalogCheckFailed = 402,
            /// <summary>
            /// Catalog更新失败
            /// </summary>
            CatalogUpdateFailed = 403,
            /// <summary>
            /// 获取下载大小失败
            /// </summary>
            GetDownloadSizeFailed = 404,
            /// <summary>
            /// 下载错误
            /// </summary>
            DownloadError = 405,
            
            /// <summary>
            /// 内部错误
            /// </summary>
            InternalError = 10000,
        }
        
        private string _checkForceUrl;

        private Func<string, bool> _onForceCheck;

        private Func<string, bool> _onCheckOriginBundle;

        private Action<long, Action<bool>> _onConfirmMobileData;
        
        private Action<float> _onProgress;
        
        /// <summary>
        /// 多次初始化AA标记
        /// </summary>
        private bool _isInited;
        /// <summary>
        /// 需要下载的key
        /// </summary>
        private HashSet<object> _downloadKeys = new();

        // 总下载大小
        private long _totalDownloadSize;
        
        // 是否同意移动数据下载
        private bool _agreeMobileDataDownload;

        /// <summary>
        /// 地址资源处理器
        /// </summary>
        /// <param name="checkForceUrl">强更检查</param>
        /// <param name="onForceCheck">版本比对回调, true : 需要强更</param>
        /// <param name="onCheckOriginBundle">检查是否是首包资源</param>
        /// <param name="onProgress">下载进度回调</param>
        /// <param name="onConfirmMobileData">移动数据确认回调</param>
        public AddressableHotUpdateHandler(string checkForceUrl, Func<string, bool> onForceCheck, Func<string, bool> onCheckOriginBundle, Action<float> onProgress, Action<long, Action<bool>> onConfirmMobileData = null)
        {
            _checkForceUrl = checkForceUrl;
            _onForceCheck = onForceCheck;
            _onCheckOriginBundle = onCheckOriginBundle;
            _onConfirmMobileData = onConfirmMobileData;
            _onProgress = onProgress;

            _isInited = false;
            _agreeMobileDataDownload = false;
            
            Addressables.InternalIdTransformFunc = LocationTransformPath;       // 重定向资源
        }
        
        public async UniTask HotUpdate(Action<IHotUpdateHandler.Status> onStatus, Action<int> onFail)
        {
            _downloadKeys.Clear();
            _totalDownloadSize = 0;
            
            // 强更检查
            {
                if (_checkForceUrl.IsNotNullAndEmpty() && _onForceCheck != null)
                {
                    Log.Debug($"检查强制更新, Url : {_checkForceUrl}");
                    onStatus?.Invoke(IHotUpdateHandler.Status.CheckingForceUpdate);
                    var errCode = await CheckForceUpdate(_checkForceUrl);
                    if (errCode != AddressableHotUpdateHandler.ErrCode.Success)
                    {
                        onFail?.Invoke((int)errCode);
                        return;
                    }
                }
            }
            
            onStatus?.Invoke(IHotUpdateHandler.Status.CheckingResourceUpdate);

            // 初始化资源管理器
            {
                Log.Debug("初始化资源管理器");
                var errCode = await InitAddressable();
                if (errCode != AddressableHotUpdateHandler.ErrCode.Success)
                {
                    onFail?.Invoke((int)errCode);
                    return;
                }
            }

            // 检查更新
            {
                Log.Debug("检查资源更新");
                var errCode = await CheckForCatalogUpdate(30);
                if (errCode != AddressableHotUpdateHandler.ErrCode.Success)
                {
                    onFail?.Invoke((int)errCode);
                    return;
                }

                if (_downloadKeys.Count <= 0)
                {
                    Log.Debug("检查本地资源,无资源可下载!");
                    onStatus?.Invoke(IHotUpdateHandler.Status.Done);
                    return;
                }
            }
            
            onStatus?.Invoke(IHotUpdateHandler.Status.CheckingResourceSize);

            // 获取更新大小
            {
                Log.Debug("获取更新大小");
                var errCode = await GetUpdateSizeFlow();
                if (errCode != AddressableHotUpdateHandler.ErrCode.Success)
                {
                    onFail?.Invoke((int)errCode);
                    return;
                }

                if (_totalDownloadSize <= 0)
                {
                    Log.Debug("资源大小为 0!");
                    onStatus?.Invoke(IHotUpdateHandler.Status.Done);
                    return;
                }
            }

            // 流量下载确认
            {
                if (!_agreeMobileDataDownload && _onConfirmMobileData != null && _totalDownloadSize > 1048576
                    && Application.internetReachability ==
                    NetworkReachability.ReachableViaCarrierDataNetwork) // 1024 * 1024
                {
                    Debug.Log("是否同意移动数据下载?");
                    onStatus?.Invoke(IHotUpdateHandler.Status.ConfirmMobileData);
                    await WaitConfirmMobileData();
                    if (!_agreeMobileDataDownload)
                    {
                        onFail?.Invoke((int)AddressableHotUpdateHandler.ErrCode.NotAllowUseMobileData);
                        return;
                    }
                }
            }

            onStatus?.Invoke(IHotUpdateHandler.Status.Downloading);

            // 下载资源
            {
                Log.Debug("下载资源");
                var errCode = await DownloadRes(3);
                if (errCode != AddressableHotUpdateHandler.ErrCode.Success)
                {
                    onFail?.Invoke((int)errCode);
                    return;
                }
                onStatus?.Invoke(IHotUpdateHandler.Status.Done);
            }
        }
        
        private async UniTask<AddressableHotUpdateHandler.ErrCode> CheckForceUpdate(string checkUrl)
        {
            using var uwr = UnityWebRequest.Get(checkUrl);
            var result = await uwr.SendWebRequest().ToUniTask();
            if (result == null || result.error != null || uwr.result != UnityWebRequest.Result.Success)
            {
                return AddressableHotUpdateHandler.ErrCode.CheckForceUpdateFailed;
            }

            var needUpdate = _onForceCheck?.Invoke(uwr.downloadHandler.text);
            return needUpdate.HasValue && needUpdate.Value
                ? AddressableHotUpdateHandler.ErrCode.NeedForceUpdate
                : AddressableHotUpdateHandler.ErrCode.Success;
        }
        
        private string LocationTransformPath(IResourceLocation location)
        {
            //判定是否是一个AB包的请求
            if (location.Data is AssetBundleRequestOptions option)
            {
                if (_onCheckOriginBundle.Invoke(location.PrimaryKey))
                {
                    var result = Path.Combine(Application.streamingAssetsPath, "FullRes", location.PrimaryKey);
                    Log.Debug($"首包缓存! {result}");
                    return result;
                }
                Log.Debug($"无缓存! {location.PrimaryKey} ");
            }
            return location.InternalId;
        }
        
        private async UniTask<AddressableHotUpdateHandler.ErrCode> InitAddressable()
        {
            if (_isInited)
            {
                return AddressableHotUpdateHandler.ErrCode.Success;
            }
            
            var initHandle = Addressables.InitializeAsync(false) ;
            await initHandle.Task;
            if (initHandle.Status == AsyncOperationStatus.Succeeded)
            {
                _isInited = true;
                Addressables.Release(initHandle);
                return AddressableHotUpdateHandler.ErrCode.Success;
            }

            Log.Error("Addressables.InitializeAsync Fail!");
            Addressables.Release(initHandle);
            return AddressableHotUpdateHandler.ErrCode.InitializeFailed;
        }

        private async UniTask<AddressableHotUpdateHandler.ErrCode> CheckForCatalogUpdate(double timeoutSeconds)
        {
            // 对比文件
            var checkHandle = Addressables.CheckForCatalogUpdates(false);

            var (checkFinish, _) = await UniTask.WhenAny(
                checkHandle.Task.AsUniTask(),
                UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds))
            );

            if (!checkFinish)
            {
                Log.Error("Addressables.CheckForCatalogUpdates Timeout!");
                Addressables.Release(checkHandle);
                return AddressableHotUpdateHandler.ErrCode.TimeOut;
            }

            if (checkHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error("Addressables.CheckForCatalogUpdates Fail!");
                Addressables.Release(checkHandle);
                return AddressableHotUpdateHandler.ErrCode.CatalogCheckFailed;
            }

            // 更新catalog
            if (checkHandle.Result.Count > 0)
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    Log.Error("网络未连接");
                    Addressables.Release(checkHandle);
                    return AddressableHotUpdateHandler.ErrCode.NotReachable;
                }

                Log.Debug($"检测到{checkHandle.Result.Count}个Catalog需要更新");
                var updateHandle = Addressables.UpdateCatalogs(checkHandle.Result, false);

                var (updateFinished, _) = await UniTask.WhenAny(
                    updateHandle.Task.AsUniTask(),
                    UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds))
                );

                if (!updateFinished)
                {
                    Log.Error("Addressables.UpdateCatalogs Timeout!");
                    Addressables.Release(updateHandle);
                    Addressables.Release(checkHandle);
                    return AddressableHotUpdateHandler.ErrCode.TimeOut;
                }

                if (updateHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Log.Error("Addressables.UpdateCatalogs Fail!");
                    Addressables.Release(updateHandle);
                    Addressables.Release(checkHandle);
                    return AddressableHotUpdateHandler.ErrCode.CatalogUpdateFailed;
                }

                Addressables.Release(updateHandle);
            }

            Addressables.Release(checkHandle);

            // 断点续传
            var resourceLocators = Addressables.ResourceLocators;
            foreach (var locator in resourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    _downloadKeys.Add(key);
                }
            }

            return AddressableHotUpdateHandler.ErrCode.Success;
        }

        private async UniTask<AddressableHotUpdateHandler.ErrCode> GetUpdateSizeFlow()
        {
            // 计算大小
            var sizeHandle = Addressables.GetDownloadSizeAsync((IEnumerable)_downloadKeys.ToList());
            await sizeHandle.Task;
            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error("Addressables.GetDownloadSizeAsync Fail!");
                Addressables.Release(sizeHandle);
                return AddressableHotUpdateHandler.ErrCode.GetDownloadSizeFailed;
            }
            
            _totalDownloadSize = sizeHandle.Result;
            Addressables.Release(sizeHandle);
            return AddressableHotUpdateHandler.ErrCode.Success;
        }

        private async UniTask WaitConfirmMobileData()
        {
            var tcs = new UniTaskCompletionSource<bool>();
            
            _onConfirmMobileData?.Invoke(_totalDownloadSize, isConfirm =>
            {
                tcs.TrySetResult(isConfirm);
            });
            
            var result = await tcs.Task;
            _agreeMobileDataDownload = result;
        }


        private async UniTask<AddressableHotUpdateHandler.ErrCode> DownloadRes(int retryCount)
        {
            var retry = 0;
            var downHandle = new AsyncOperationHandle();
            while (retry < retryCount)
            {
                if (downHandle.IsValid())
                {
                    Addressables.Release(downHandle);
                }

                downHandle = Addressables.DownloadDependenciesAsync((IEnumerable)_downloadKeys, Addressables.MergeMode.Union);
                
                while (!downHandle.IsDone)
                {
                    _onProgress?.Invoke(downHandle.PercentComplete);
                    await UniTask.Yield();
                }
                
                // 失败重试
                if (downHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    retry++;
                    Log.Error($"下载失败, 重试 {retry}/{retryCount}, 网络状态: {Application.internetReachability}");
                    // 还有重试次数，重试
                    if (retry < retryCount)
                    {
                        await UniTask.Delay(500);
                        continue;
                    }
                    
                    // 重试耗尽，释放并返回错误
                    if (downHandle.IsValid())
                    {
                        Addressables.Release(downHandle);
                    }

                    return AddressableHotUpdateHandler.ErrCode.DownloadError;
                }
                
                // 下载完成
                if (downHandle.IsValid())
                {
                    Addressables.Release(downHandle);
                }
                _onProgress?.Invoke(1);
                return AddressableHotUpdateHandler.ErrCode.Success;
            }

            return AddressableHotUpdateHandler.ErrCode.InternalError;
        }

    }
}