using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using HybridCLR;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = System.Object;

public class HotUpdateHandler : IHotUpdateHandler
{
    private const string PreloadKey = "check_update";
    
    // 提示回调
    private Action<string> _onTip;
    // 要更新的列表
    private List<string> _updateCatalogs = new();
    // 首包包含的清单
    private HashSet<string> _bundleSet = new();
    // 下载的大小
    private long _downloadSize = 0;
    
    public HotUpdateHandler(Action<string> onTip)
    {
        _onTip = onTip;
    }

    public IEnumerator CheckResources()
    {
        yield return LoadCache();

        yield return InitAddressables();

        yield return CheckForCatalogUpdates();
        
        if (_updateCatalogs.Count > 0)
        {
            yield return UpdateCatalogs();
        }
        
        yield return TryDownloadBundle();
    }

    public IEnumerator LoadMetaData()
    {
        Debug.Log("补充元数据...");
        const string label = "dll_metadata";

        var handle = Addressables.LoadAssetsAsync(label, (TextAsset dll) =>
        {
            byte[] bytes = new byte[dll.bytes.Length];
            Array.Copy(dll.bytes, bytes, bytes.Length);
            Debug.Log($"load meta data : {dll.name}");
            HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet);
        });

        yield return handle;
        
        handle.Release();
    }

    public IEnumerator LoadDll()
    {
        Debug.Log("加载热更新程序集...");
        const string label = "dll_hotfix";

        var handle = Addressables.LoadAssetsAsync(label, (TextAsset dll) =>
        {
            byte[] bytes = new byte[dll.bytes.Length];
            Array.Copy(dll.bytes, bytes, bytes.Length);
            Debug.Log($"load assembly : {dll.name}");
            Assembly.Load(bytes);
        });

        yield return handle;
        
        handle.Release();
        
        yield break;
    }
    
    /// <summary>
    /// 读取本地缓存列表
    /// </summary>
    private IEnumerator LoadCache()
    {
        Debug.Log("检查本地资源缓存.....");
        
        var result = string.Empty;
        var fullPath = Path.Combine(Application.streamingAssetsPath, "FullRes", "__bundle_list__.txt");

        using (var www = UnityWebRequest.Get(fullPath))
        {
            www.SendWebRequest();

            yield return www;
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                result = www.downloadHandler.text;
            }
        }
        
        if (result == null)
        {
            Debug.LogError("没有找到 __bundle_list__");
            yield break;
        }
        
        // Update Bundle List
        foreach (var bundleName in result.Split('\n'))
        {
            if (!string.IsNullOrEmpty(bundleName))
            {
                _bundleSet.Add(bundleName.Trim());
            }
        }

        Debug.Log($"Find Bundle Cache Count : {_bundleSet.Count}");
    }

    /// <summary>
    /// 初始化Addressables
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private IEnumerator InitAddressables()
    {
        Debug.Log("初始化资源管理器......");
        
        // 重定向远程资源包
        Addressables.InternalIdTransformFunc = InternalIdTransformFunc;
        
        // 手动初始化
        var handle = Addressables.InitializeAsync(false);

        yield return handle;
        
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            handle.Release();
            throw new Exception("初始化失败!");
        }
        
        handle.Release();
    }
    
    /// <summary>
    /// 检查云端和本地的 Catalog.Hash.
    /// </summary>
    /// <returns>返回不一致的 Catalog 列表</returns>
    private IEnumerator CheckForCatalogUpdates()
    {
        Debug.Log("检查服务器......");
        
        var handle = Addressables.CheckForCatalogUpdates(false);
        
        yield return handle;
        
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            handle.Release();
            throw new Exception("CheckForCatalogUpdates Error\n" + handle.OperationException);
        }
        
        _updateCatalogs = handle.Result;
        handle.Release();
    }
    
    /// <summary>
    /// 更新本地的 Catalogs 文件
    /// </summary>
    private IEnumerator UpdateCatalogs()
    {
        Debug.Log("UpdateCatalogs");
        
        var handle = Addressables.UpdateCatalogs(_updateCatalogs, false);
        
        yield return handle;
        
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            handle.Release();
            throw new Exception("UpdateCatalogs Error\n" + handle.OperationException);
        }
        
        handle.Release();
    }

    /// <summary>
    /// 尝试下载资源包
    /// </summary>
    /// <returns></returns>
    private IEnumerator TryDownloadBundle()
    {
        // 计算大小
        var sizeHandle = Addressables.GetDownloadSizeAsync(PreloadKey);
        
        yield return sizeHandle;

        _downloadSize = sizeHandle.Result;
        
        sizeHandle.Release();
        if (_downloadSize <= 0)
        {
            Debug.Log("没有要更新的资源");
            yield break;
        }
        
        Debug.Log($"更新资源{_downloadSize} bytes");

        var loadHandle = Addressables.LoadResourceLocationsAsync(PreloadKey);
        yield return loadHandle;

        if (loadHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("没有找到资源");
            loadHandle.Release();
            yield break;
        }

        var downloadHandle = Addressables.DownloadDependenciesAsync(loadHandle.Result, false);
        
        while (!downloadHandle.IsDone)
        {
            if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                yield return null;
                Debug.Log($"{downloadHandle.GetDownloadStatus().DownloadedBytes / 1000000f:0.00}Mb / {_downloadSize / 1000000f:0.00}Mb");
            }
        }
        
        downloadHandle.Release();
        loadHandle.Release();
    }
    
    /// <summary>
    /// 重定向远程资源包
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    private string InternalIdTransformFunc(UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation location)
    {
        //判定是否是一个AB包的请求
        if (location.Data is AssetBundleRequestOptions option)
        {
            string result = null;
            if (_bundleSet.Contains(location.PrimaryKey))
            {
                result = Path.Combine(Application.streamingAssetsPath, "FullRes", location.PrimaryKey);
                Debug.Log($"首包缓存! {result}");
                return result;
            }
            // 检查缓存资源
            result = Path.Combine(Caching.currentCacheForWriting.path, option.BundleName, option.Hash, "__data");
            if (File.Exists(result))
            {
                Debug.Log($"更新缓存! {location.PrimaryKey}");
                return result;
            }
                
            Debug.Log($"无缓存! {location.PrimaryKey}");
        }
            
        return location.InternalId;
    }
    
    
}