using System.Diagnostics;
using System.IO;
using System.Text;
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
            var directoryInfo = new FileInfo(Vomit.RuntimeConfig.ClientDBConfig.ConfigPath).Directory;
            if (directoryInfo != null)
            {
                Process.Start(directoryInfo.FullName);
            }
        }
        
        
        [MenuItem("VomitLib/ClientDB/生成客户端数据")]
        private static void GenerateData()
        {
            var config = Vomit.RuntimeConfig.ClientDBConfig;
            
            string cmd = GenerateCmd(config.GenCodePath, config.JsonOutputPath, true);
            RunCmd(cmd);
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("VomitLib/ClientDB/生成客户端数据(Clean)")]
        private static void ClearAndGenerateData()
        {
            var config = Vomit.RuntimeConfig.ClientDBConfig;
            
            DirectoryInfo dir = new DirectoryInfo(config.JsonOutputPath);
        
            foreach (var fileInfo in dir.GetFiles())
            {
                File.Delete(fileInfo.FullName);
            }

            GenerateData();
        }
        
        [MenuItem("VomitLib/ClientDB/生成服务器数据")]
        private static void ClearAndGenerateServerData()
        {
            var config = Vomit.RuntimeConfig.NetConfig;
            string cmd = GenerateCmd(config.ServerScriptPath, config.ServerDataPath, true);
            RunCmd(cmd);
        }
        
        private static string GenerateCmd(string outputCodeDir, string outputDataDir, bool enableL10N)
        {
            var config = Vomit.RuntimeConfig.ClientDBConfig;

            //https://luban.doc.code-philosophy.com/docs/manual/commandtools#unity--c--json
            StringBuilder cmd = new();
            cmd.Append($"dotnet \"{config.ClientServerDllPath}\" -t all --conf \"{config.ConfigPath}\" ");
            cmd.Append("-c cs-simple-json -d json ");
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