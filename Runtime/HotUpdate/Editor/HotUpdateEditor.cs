using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Application = UnityEngine.Device.Application;

public partial class HotUpdateEditor : Editor
{
    private const string PreloadKey = "check_update";
    
    /// <summary>
    /// bundle 文件复制目标路径
    /// </summary>
    private static string _sCopyBundleTargetPath = Path.Combine(Application.streamingAssetsPath, "FullRes");
        
    /// <summary>
    /// bundle 文件复制源路径
    /// </summary>
    private static string _sCopyBundleSourcePath = Path.Combine(Environment.CurrentDirectory, "Bundles", EditorUserBuildSettings.activeBuildTarget.ToString());

    /// <summary>
    /// bundle 列表文件路径
    /// </summary>
    private static string _sBundleListPath = Path.Combine(Application.streamingAssetsPath, "FullRes", "__bundle_list__.txt");

    /// <summary>
    /// Dll 资源文件夹路径
    /// </summary>
    private static string _sDllResPath = Path.Combine(Application.dataPath, "_Resources", "Dll");
    
    /// <summary>
    /// AA 配置
    /// </summary>
    private static string _sResProfile = "HotFix";

    /// <summary>
    /// AA 设置文件路径
    /// </summary>
    private static string _sResSettingsAssetPath = Path.Combine("Assets", "AddressableAssetsData", "AddressableAssetSettings.asset");
    
    /// <summary>
    /// AA 构建配置路径
    /// </summary>
    private static string _sResBuildAssetPath = Path.Combine("Assets", "AddressableAssetsData", "DataBuilders", "BuildScriptPackedMode.asset");
    
    
    /// <summary>
    /// 生成所有的DLL
    /// </summary>
    [MenuItem("HotUpdate/1.GenerateDll", false, 1)]
    private static void GenerateDll()
    {
        HybridCLR.Editor.Commands.PrebuildCommand.GenerateAll();
    }

    /// <summary>
    /// 复制 DLL 到资源目录
    /// </summary>
    [MenuItem("HotUpdate/2.CopyDll", false, 2)]
    private static void CopyDll()
    {
        // 删除旧的DLL
        var dllDir = new DirectoryInfo(_sDllResPath);
        foreach (var file in dllDir.GetFiles())
        {
            file.Delete();
        }
        
        // 复制 HotFix Dll
        foreach (var assemblyName in HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyNamesExcludePreserved)
        {
            var source = Path.Combine(HybridCLR.Editor.SettingsUtil.HotUpdateDllsRootOutputDir, EditorUserBuildSettings.activeBuildTarget.ToString(), $"{assemblyName}.dll");
            var target = Path.Combine(_sDllResPath, $"{assemblyName}.dll.bytes");

            if (File.Exists(source))
            {
                File.Copy(source, target);
                Debug.Log($"{source} ==> {target}");
                // TODO 修改AA label
            }
            else
            {
                Debug.LogError($"补充元数据{source}找不到!");
            }
        }
        
        // 复制补充元数据的 Dll
        foreach (var dll in HybridCLRSettings.Instance.patchAOTAssemblies)
        {
            var source = Path.Combine(HybridCLR.Editor.SettingsUtil.AssembliesPostIl2CppStripDir, EditorUserBuildSettings.activeBuildTarget.ToString(), $"{dll}.dll");
            var target = Path.Combine(_sDllResPath, $"{dll}.dll.bytes");

            if (File.Exists(source))
            {
                File.Copy(source, target);
                Debug.Log($"{source} ==> {target}");
                // TODO 修改AA label
            }
            else
            {
                Debug.LogError($"补充元数据{source}找不到!");
            }
        }
        
        // 刷新 AssetDatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    
    /// <summary>
    /// 设置预加载资源标签
    /// </summary>
    [MenuItem("HotUpdate/3.SetPreloadLabel", false, 3)]
    private static void SetPreloadLabel()
    {
        foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
        {
            foreach (var entry in group.entries)
            {
                entry.labels.Add(PreloadKey);
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 生成 bundle
    /// </summary>
    [MenuItem("HotUpdate/4.GenerateBundle", false, 4)]
    private static void GenerateBundle()
    {
        var settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(_sResSettingsAssetPath);

        settings.activeProfileId = settings.profileSettings.GetProfileId(_sResProfile);

        var builder = AssetDatabase.LoadAssetAtPath<ScriptableObject>(_sResBuildAssetPath) as IDataBuilder;

        if (builder == null)
        {
            Debug.LogError(_sResSettingsAssetPath + " couldn't be found or isn't a build script.");
            return;
        }

        int index = settings.DataBuilders.IndexOf((ScriptableObject)builder);

        if (index > 0)
        {
            settings.ActivePlayerDataBuilderIndex = index;
        }
        else
        {
            Debug.LogWarning($"{builder} must be added to the " +
                             $"DataBuilders list before it can be made " +
                             $"active. Using last run builder instead.");
        }

        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError("Addressables build error encountered: " + result.Error);
            return;
        }
    }
    
    /// <summary>
    /// 将远程 bundle 复制到 streamingAssetsPath 对应的文件夹下
    /// 并在 streamingAssetsPath 生成对应的 __bundle_list__.txt
    /// </summary>
    [MenuItem("HotUpdate/5.CopyBundle", false, 5)]
    private static void CopyBundle()
    {
        var tDir = new DirectoryInfo(_sCopyBundleTargetPath);
        if (Directory.Exists(_sCopyBundleTargetPath))
        {
            tDir.Delete(true);  // 删除文件夹
        }
        
        // 复制到 streamingAssetsPath
        CopyBundle(_sCopyBundleSourcePath, _sCopyBundleTargetPath, true);
        
        // 记录 bundle 列表
        WriteBundleList();
        
        // 刷新 AssetDatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    [MenuItem("HotUpdate/6.Build", false, 6)]
    private static void Build()
    {
        
        
    }
    
    [MenuItem("HotUpdate/7.Upload", false, 7)]
    private static void Upload()
    {
        const string bucketPath = "E:\\adventurelegend_upgrade\\Remote";        // 本地模拟的桶

        string resPath = Path.Combine(bucketPath, EditorUserBuildSettings.activeBuildTarget.ToString());
        // 获取当前服务器上的版本
        int version = 0;
        foreach (var directoryInfo in new DirectoryInfo(resPath).GetDirectories())
        {
            if (directoryInfo.Name.StartsWith("hotfix"))
            {
                version = Mathf.Max(version, int.Parse(directoryInfo.Name.Split('_')[1]));
            }
        }
        version++;
        
        // 备份catlog
        const string catlogName = "catalog_hotfix.json";
        const string hashName = "catalog_hotfix.hash";
        var backupPath = Path.Combine(resPath, $"hotfix_{version}");
        Directory.CreateDirectory(backupPath);
        File.Copy(Path.Combine(_sCopyBundleSourcePath, catlogName), Path.Combine(backupPath, catlogName));
        File.Copy(Path.Combine(_sCopyBundleSourcePath, hashName), Path.Combine(backupPath, hashName));
        
        // 上传到桶
        CopyBundle(_sCopyBundleSourcePath, resPath, true);
    }
    
    /// <summary>
    /// 复制bundle
    /// </summary>
    private static void CopyBundle(string sourceDirName, string destDirName, bool copySubDirs)
    {
        // 获取源目录信息
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        // 检查源目录是否存在
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException("源目录不存在或无法找到: " + sourceDirName);
        }

        // 获取目标目录信息
        DirectoryInfo[] dirs = dir.GetDirectories();

        // 如果目标目录不存在，则创建它
        Directory.CreateDirectory(destDirName);

        // 获取源目录中的所有文件并复制它们
        foreach (FileInfo file in dir.GetFiles())
        {
            string target = Path.Combine(destDirName, file.Name);
            file.CopyTo(target, true);
        }

        // 如果需要复制子目录，则递归调用此方法
        if (copySubDirs)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                CopyBundle(subDir.FullName, Path.Combine(destDirName, subDir.Name), true);
            }
        }
        
        
    }
    
    /// <summary>
    /// 写入数据
    /// </summary>
    private static void WriteBundleList()
    {
        var dir = new DirectoryInfo(_sCopyBundleTargetPath);
        
        // 写入清单
        using var stream = new StreamWriter(_sBundleListPath);
        
        // 遍历所有 .bundle 文件
        foreach (var file in dir.GetFiles("*.bundle", SearchOption.AllDirectories))
        {
            // 计算相对路径
            string relativePath = Path.GetRelativePath(_sCopyBundleTargetPath, file.FullName);
            stream.WriteLine(relativePath.Replace('\\', '/').Trim());
        }
        
    }
}