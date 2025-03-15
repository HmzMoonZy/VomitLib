using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Twenty2.VomitLib.Config;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Twenty2.VomitLib.Editor
{
    // TODO 支持 luban 特性
    public class ClientDBEditor : UnityEditor.Editor
    {
        [MenuItem("VomitLib/ClientDB/打开数据配置目录")]
        public static void OpenDataTableFolder()
        {
            var directoryInfo = new FileInfo(Vomit.GetConfigInEditor().ClientDatabaseConfig.ConfigPath).Directory;
            if (directoryInfo != null)
            {
                Process.Start(directoryInfo.FullName);
            }
        }
        
        
        [MenuItem("VomitLib/ClientDB/生成客户端数据")]
        public static async void GenerateData()
        {
            try
            {
                var config = Vomit.GetConfigInEditor().ClientDatabaseConfig;
            
                string cmd = GenerateCmd(config.GenCodePath, config.GenDataPath, true, config.NoneStyle, config.Format);
                await RunCmd(cmd);
                EditorUtility.DisplayDialog("生成客户端数据", "生成客户端数据成功", "确定");
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                
                
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        [MenuItem("VomitLib/ClientDB/生成客户端数据(Clean)")]
        public static void ClearAndGenerateData()
        {
            var config = Vomit.GetConfigInEditor().ClientDatabaseConfig;
            
            DirectoryInfo dir = new DirectoryInfo(config.GenDataPath);
        
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
        
        private static string GenerateCmd(string outputCodeDir, string outputDataDir, bool enableL10N, bool useNoneStyle, ClientDBConfig.JsonFormat format)
        {
            var config = Vomit.GetConfigInEditor().ClientDatabaseConfig;

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
            cmd.Append($"-x \"{strFormatD}.fileExt=bytes\" ");

            if (useNoneStyle)
            {
                cmd.Append($"-x \"codeStyle=none\" ");    
            }
            
            if(enableL10N)
            {
                cmd.Append($"-x l10n.provider=default -x \"l10n.textFile.path={config.LocalizationPath}\" -x l10n.textFile.keyFieldName=key");
            }
            // cmd.AppendLine("\n pause");

            return cmd.ToString();
        }

        private static async Task RunCmd(string cmd)
        {
            Debug.Log($"RunCmd : {cmd}");
            // 运行 bat
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C " + cmd,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            EditorUtility.DisplayProgressBar("正在生成客户端数据...", "", 0);
            
            using var process = new Process();
            var tcs = new TaskCompletionSource<int>();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Debug.Log(e.Data);      // 正常日志
                    //EditorUtility.DisplayProgressBar("正在生成客户端数据...", e.Data, 0);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Debug.LogError(e.Data); // 错误日志
                }
            };
            
            process.Exited += (sender, e) =>
            {
                tcs.SetResult(process.ExitCode);
            };
            
            
            process.EnableRaisingEvents = true;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await tcs.Task;
            EditorUtility.ClearProgressBar();
        }
    }



}