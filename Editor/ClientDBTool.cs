﻿using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Twenty2.VomitLib.Editor
{
    public class ClientDBTool
    {
        [MenuItem("VomitLib/ClientDB/打开数据配置目录")]
        private static void OpenDataTableFolder()
        {
            var directoryInfo = new FileInfo(Vomit.RuntimeConfig.ClientDBConfig.ConfigPath).Directory;
            if (directoryInfo != null)
                Process.Start(directoryInfo.FullName);
        }

        [MenuItem("VomitLib/ClientDB/生成数据层(Clean)")]
        public static void ClearAndGenerateData()
        {
            var config = Vomit.RuntimeConfig.ClientDBConfig;
            
            DirectoryInfo dir = new DirectoryInfo(config.JsonOutputPath);
        
            foreach (var fileInfo in dir.GetFiles())
            {
                File.Delete(fileInfo.FullName);
            }

            GenerateData();
        }

        [MenuItem("VomitLib/ClientDB/生成数据层")]
        public static void GenerateData()
        {
            var config = Vomit.RuntimeConfig.ClientDBConfig;

            //https://luban.doc.code-philosophy.com/docs/manual/commandtools#unity--c--json
            string cmd =
@$"dotnet {config.ClientServerDllPath} -t all --conf {config.ConfigPath} -c cs-simple-json -d json -x outputCodeDir={config.GenCodePath} -x outputDataDir={config.JsonOutputPath} -x l10n.textProviderFile=*@{config.LocalizationPath} -d text-list -x l10n.textListFile=textList.txt

pause";
            Debug.Log(cmd);
            
            // 运行 bat
            ProcessStartInfo startInfo = new ProcessStartInfo 
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal, 
                FileName = "cmd.exe",
                Arguments = "/k " + cmd,
            };

            var process = Process.Start(startInfo);

            if (process == null) return;
            
            process.WaitForExit();
            
            process.Dispose();

            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
    }
}