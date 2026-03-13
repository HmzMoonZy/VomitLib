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

namespace Twenty2.VomitLib.HotFix
{
    /// <summary>
    /// 基于Addressable的热更新处理器
    /// 维护一个BundleSet描述首包资源, 全量更新
    /// </summary>
    public class AddressableHotUpdateHandler : IHotUpdateHandler
    {
        private string _checkForceUrl;

        private Func<string, bool> _onForceCheck;

        private Func<string, bool> _onCheckOriginBundle;

        private Action<long, Action<bool>> _onConfirmMobileData;
        
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
        /// 地址资源处理器, 全量资源放置在
        /// </summary>
        /// <param name="checkForceUrl">强更检查</param>
        /// <param name="onForceCheck">版本比对回调, true : 需要强更</param>
        /// <param name="onCheckOriginBundle">检查是否是首包资源</param>
        /// <param name="onConfirmMobileData">移动数据确认回调</param>
        public AddressableHotUpdateHandler(string checkForceUrl, Func<string, bool> onForceCheck, Func<string, bool> onCheckOriginBundle, Action<long, Action<bool>> onConfirmMobileData = null)
        {
            _checkForceUrl = checkForceUrl;
            _onForceCheck = onForceCheck;
            _onCheckOriginBundle = onCheckOriginBundle;
            _onConfirmMobileData = onConfirmMobileData;

            _isInited = false;
            _agreeMobileDataDownload = false;
            
            Addressables.InternalIdTransformFunc = LocationTransformPath;       // 重定向资源
        }
        
        public async UniTask HotUpdate(Action<IHotUpdateHandler.Status> onStatus, Action<IHotUpdateHandler.ErrCode> onFail)
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
                    if (errCode != IHotUpdateHandler.ErrCode.Success)
                    {
                        onFail?.Invoke(errCode);
                        return;
                    }
                }
            }
            
            onStatus?.Invoke(IHotUpdateHandler.Status.CheckingResourceUpdate);

            // 初始化资源管理器
            {
                Log.Debug("初始化资源管理器");
                var errCode = await InitAddressable();
                if (errCode != IHotUpdateHandler.ErrCode.Success)
                {
                    onFail?.Invoke(errCode);
                    return;
                }
            }

            // 检查更新
            {
                Log.Debug("检查资源更新");
                var errCode = await CheckForCatalogUpdate(30);
                if (errCode != IHotUpdateHandler.ErrCode.Success)
                {
                    onFail?.Invoke(errCode);
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
                if (errCode != IHotUpdateHandler.ErrCode.Success)
                {
                    onFail?.Invoke(errCode);
                    return;
                }

                if (_totalDownloadSize <= 0)
                {
                    Log.Debug("资源大小为 0!");
                    onStatus?.Invoke(IHotUpdateHandler.Status.Done);
                    return;
                }
            }

            if (!_agreeMobileDataDownload && _onConfirmMobileData != null && _totalDownloadSize > 1048576
                && Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)  // 1024 * 1024
            {
                Debug.Log("是否同意移动数据下载?");
                onStatus?.Invoke(IHotUpdateHandler.Status.ConfirmMobileData);
                await WaitConfirmMobileData();
                if (!_agreeMobileDataDownload)
                {
                    onFail?.Invoke(IHotUpdateHandler.ErrCode.NotAllowUseMobileData);
                    return;
                }
            }
            
            onStatus?.Invoke(IHotUpdateHandler.Status.Downloading);
            
            
        }
        
        private async UniTask<IHotUpdateHandler.ErrCode> CheckForceUpdate(string checkUrl)
        {
            using var uwr = UnityWebRequest.Get(checkUrl);
            var result = await uwr.SendWebRequest().ToUniTask();
            if (result == null || result.error != null || uwr.result != UnityWebRequest.Result.Success)
            {
                return IHotUpdateHandler.ErrCode.CheckForceUpdateFailed;
            }

            var needUpdate = _onForceCheck?.Invoke(uwr.downloadHandler.text);
            return needUpdate.HasValue && needUpdate.Value
                ? IHotUpdateHandler.ErrCode.NeedForceUpdate
                : IHotUpdateHandler.ErrCode.Success;
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
        
        private async UniTask<IHotUpdateHandler.ErrCode> InitAddressable()
        {
            if (_isInited)
            {
                return IHotUpdateHandler.ErrCode.Success;
            }
            
            var initHandle = Addressables.InitializeAsync(false) ;
            await initHandle.Task;
            if (initHandle.Status == AsyncOperationStatus.Succeeded)
            {
                _isInited = true;
                Addressables.Release(initHandle);
                return IHotUpdateHandler.ErrCode.Success;
            }

            Log.Error("Addressables.InitializeAsync Fail!");
            Addressables.Release(initHandle);
            return IHotUpdateHandler.ErrCode.InternalError;
        }

        private async UniTask<IHotUpdateHandler.ErrCode> CheckForCatalogUpdate(double timeoutSeconds)
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
                return IHotUpdateHandler.ErrCode.TimeOut;
            }

            if (checkHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error("Addressables.CheckForCatalogUpdates Fail!");
                Addressables.Release(checkHandle);
                return IHotUpdateHandler.ErrCode.InternalError;
            }

            // 更新catalog
            if (checkHandle.Result.Count > 0)
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    Log.Error("网络未连接");
                    Addressables.Release(checkHandle);
                    return IHotUpdateHandler.ErrCode.NotReachable;
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
                    return IHotUpdateHandler.ErrCode.TimeOut;
                }

                if (updateHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Log.Error("Addressables.UpdateCatalogs Fail!");
                    Addressables.Release(updateHandle);
                    Addressables.Release(checkHandle);
                    return IHotUpdateHandler.ErrCode.InternalError;
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

            return IHotUpdateHandler.ErrCode.Success;
        }

        private async UniTask<IHotUpdateHandler.ErrCode> GetUpdateSizeFlow()
        {
            // 计算大小
            var sizeHandle = Addressables.GetDownloadSizeAsync((IEnumerable)_downloadKeys.ToList());
            await sizeHandle.Task;
            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error("Addressables.GetDownloadSizeAsync Fail!");
                Addressables.Release(sizeHandle);
                return IHotUpdateHandler.ErrCode.InternalError;
            }
            
            _totalDownloadSize = sizeHandle.Result;
            Addressables.Release(sizeHandle);
            return IHotUpdateHandler.ErrCode.Success;
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


        private async UniTask DownloadRes()
        {
            
        }
        
    }
}