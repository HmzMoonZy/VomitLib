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

            string cmd = 
                @$" ExternalTools/Luban/Luban.ClientServer.dll -j cfg -- ^
 -d {config.RootXmlPath} ^
 --input_data_dir {config.ExcelPath} ^
 --gen_types code_cs_unity_json,data_json ^
 -s all ^
 --output_data_dir {config.JsonOutputPath} ^
 --output_code_dir {config.GenCodePath} ^
 --output:data:file_extension {config.Suffix} ^
 --l10n:input_text_files {config.LocalizationPath}  ^
 --l10n:text_field_name text_cn  ^
 --l10n:output_not_translated_text_file {config.JsonOutputPath}/_not_translated_text.txt";

            Debug.Log(cmd);
            
            var process = _Run(
                "dotnet.exe",
                cmd,
                ".",
                true
            );

            
            #region 捕捉生成错误
            string processLog = process.StandardOutput.ReadToEnd();
            Debug.Log(processLog);
            
            if (process.ExitCode != 0)
            {
                Debug.LogError("Error  生成出现错误");
                EditorUtility.DisplayDialog("ClientDBTool Error", processLog, "ok");
                Debug.LogError(processLog);
            }
            #endregion

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
                    Verb = "runas",
                };

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