using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Twenty2.VomitLib.Editor
{
    public class ClientDBTool
    {
        [MenuItem("VomitLib/ClientDB/打开数据配置目录")]
        private static void OpenDataTableFolder() =>
            Process.Start(Path.Combine(Application.dataPath[..^6], Vomit.RuntimeConfig.ClientDBConfig.ExcelPath));

        [MenuItem("VomitLib/ClientDB/生成数据层")]
        public static void GenerateDataTool()
        {
            var config = Vomit.RuntimeConfig.ClientDBConfig;
            
            //https://luban.doc.code-philosophy.com/docs/manual/commandtools#unity--c--json
            string cmd = 
                $" {config.ClientServerDllPath} -t all -c cs-simple-json -d json --conf {config.ConfigPath} -x outputCodeDir={config.GenCodePath} -x outputDataDir={config.JsonOutputPath} -x l10n.textProviderFile=*@{config.LocalizationPath}";

            Debug.Log(cmd);
            
            var process = _Run(
                "dotnet.exe",
                cmd,
                ".",
                true
            );

            
            #region 捕捉生成错误

            string[] log = new string[2];
            log[0] = process.StandardOutput.ReadToEnd();
            if (process.ExitCode != 0)
            {
                log[1] = process.StandardError.ReadToEnd();
                EditorUtility.DisplayDialog("ClientDBTool Error", log[1], "ok");
                Debug.Log(log[0]);
                Debug.LogError(log[1]);
                return;
            }
            #endregion
            
            Debug.Log(log[0]);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


        private static Process _Run(string exe,
            string arguments,
            string working_dir = ".",
            bool wait_exit = false)
        {
            try
            {
                bool redirect_standard_output = true;
                bool redirect_standard_error = true;
                bool use_shell_execute = false;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    redirect_standard_output = false;
                    redirect_standard_error = false;
                    use_shell_execute = true;
                }

                if (wait_exit)
                {
                    redirect_standard_output = true;
                    redirect_standard_error = true;
                    use_shell_execute = false;
                }

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = use_shell_execute,
                    WorkingDirectory = working_dir,
                    RedirectStandardOutput = redirect_standard_output,
                    RedirectStandardError = redirect_standard_error,
                };

                Debug.Log($"dir: {Path.GetFullPath(working_dir)}, command: {exe} {arguments}");
                
                Process process = Process.Start(info);

                if (wait_exit)
                {
                    WaitForExitAsync(process).ConfigureAwait(false);
                }

                return process;
            }
            catch (Exception e)
            {
                throw new Exception($"dir: {Path.GetFullPath(working_dir)}, command: {exe} {arguments}", e);
            }
        }
        
        private static async Task WaitForExitAsync(Process self)
        {
            if (!self.HasExited)
            {
                return;
            }

            try
            {
                self.EnableRaisingEvents = true;
            }
            catch (InvalidOperationException)
            {
                if (self.HasExited)
                {
                    return;
                }

                throw;
            }

            var tcs = new TaskCompletionSource<bool>();

            void Handler(object s, EventArgs e) => tcs.TrySetResult(true);

            self.Exited += Handler;

            try
            {
                if (self.HasExited)
                {
                    return;
                }

                await tcs.Task;
            }
            finally
            {
                self.Exited -= Handler;
            }
        }
    }
}