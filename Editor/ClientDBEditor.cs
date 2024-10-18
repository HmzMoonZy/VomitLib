using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Twenty2.VomitLib.Config;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Twenty2.VomitLib.Editor
{
    // TODO 支持 luban 特性
    public class ClientDBEditor : UnityEditor.Editor
    {
        [MenuItem("VomitLib/ClientDB/打开数据配置目录")]
        private static void OpenDataTableFolder()
        {
            var directoryInfo = new FileInfo(Vomit.GetConfigInEditor().ClientDBConfig.ConfigPath).Directory;
            if (directoryInfo != null)
            {
                Process.Start(directoryInfo.FullName);
            }
        }
        
        
        [MenuItem("VomitLib/ClientDB/生成客户端数据")]
        private static void GenerateData()
        {
            var config = Vomit.GetConfigInEditor().ClientDBConfig;
            
            string cmd = GenerateCmd(config.GenCodePath, config.JsonOutputPath, true, config.Format);
            RunCmd(cmd);
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("VomitLib/ClientDB/生成客户端数据(Clean)")]
        private static void ClearAndGenerateData()
        {
            var config = Vomit.GetConfigInEditor().ClientDBConfig;
            
            DirectoryInfo dir = new DirectoryInfo(config.JsonOutputPath);
        
            foreach (var fileInfo in dir.GetFiles())
            {
                File.Delete(fileInfo.FullName);
            }

            GenerateData();
        }
        
        // [MenuItem("VomitLib/ClientDB/生成服务器数据")]
        // private static void ClearAndGenerateServerData()
        // {
        //     var config = Vomit.GetConfigInEditor().NetConfig;
        //     string cmd = GenerateCmd(config.ServerScriptPath, config.ServerDataPath, true, config.Format);
        //     RunCmd(cmd);
        // }
        
        private static string GenerateCmd(string outputCodeDir, string outputDataDir, bool enableL10N, ClientDBConfig.JsonFormat format)
        {
            var config = Vomit.GetConfigInEditor().ClientDBConfig;

            string strFormatC = format switch
            {
                ClientDBConfig.JsonFormat.SimpleJson => "cs-simple-json",
                ClientDBConfig.JsonFormat.NewtonsoftJson => "cs-newtonsoft-json",
                ClientDBConfig.JsonFormat.Bin => "cs-bin",
            };
            
            string strFormatD = format switch
            {
                ClientDBConfig.JsonFormat.SimpleJson => "json",
                ClientDBConfig.JsonFormat.NewtonsoftJson => "json",
                ClientDBConfig.JsonFormat.Bin => "bin",
            };

            //https://luban.doc.code-philosophy.com/docs/manual/commandtools#unity--c--json
            StringBuilder cmd = new();
            cmd.Append($"dotnet \"{config.ClientServerDllPath}\" -t all --conf \"{config.ConfigPath}\" ");
            cmd.Append($"-c {strFormatC} -d {strFormatD} ");
            cmd.Append($"-x \"outputCodeDir={outputCodeDir}\" ");
            cmd.Append($"-x \"outputDataDir={outputDataDir}\" ");
            if(enableL10N)
            {
                cmd.Append($"-x l10n.provider=default -x \"l10n.textFile.path={config.LocalizationPath}\" -x l10n.textFile.keyFieldName=key ");
            }
            cmd.AppendLine("\n pause");

            return cmd.ToString();
        }

        private static void RunCmd(string cmd)
        {
            Debug.Log($"RunCmd : {cmd}");
            // 运行 bat
            ProcessStartInfo startInfo = new ProcessStartInfo 
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal, 
                FileName = "cmd.exe",
                Arguments = "/k " + cmd,
                CreateNoWindow = false,
            };
            
            var process = Process.Start(startInfo);
            
            if (process == null) return;
            
            process.WaitForExit();
            
            process.Dispose();
        }
    }



}