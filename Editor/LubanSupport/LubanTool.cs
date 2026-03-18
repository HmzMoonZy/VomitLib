using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAPI;
using Luban;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Twenty2.VomitLib;
using Twenty2.VomitLib.Config;
using Twenty2.VomitLib.Editor;
using Twenty2.VomitLib.LubanSupport;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LubanSupport.Editor
{
    // TODO 支持Group配置
    public class LubanTool : UnityEditor.Editor
    {
        public static LubanConfig Config
        {
            get
            {
                return VomitEditor.Config.LubanConfig;
            }
        }

        private enum GenMode { Client, Server, Backup }

        /// <summary>
        /// 在编辑器下直接读取配置
        /// </summary>
        public static T GetTable<T>(string sourceName = null)
        {
            var constructor = typeof(T).GetConstructors()[0];
            var parameter = constructor.GetParameters()[0];
            sourceName ??= typeof(T).Name.ToLower().Substring(2); 
            
            if (parameter.ParameterType == typeof(Luban.ByteBuf))
            {
                return (T)constructor.Invoke(new object[] { BinLoader()});
            }

            if (parameter.ParameterType == typeof(JArray))
            {
                return (T)constructor.Invoke(new object[] { JsonLoader()});
            }

            throw new NotImplementedException();
            
            ByteBuf BinLoader()
            {
                var source = GetTableAssetInEditor(sourceName);
                return new Luban.ByteBuf(source.bytes);
            }

            JArray JsonLoader()
            {
                var source = GetTableAssetInEditor(sourceName);
                return JsonConvert.DeserializeObject<JArray>(source.text);
            }
        }

        private static TextAsset GetTableAssetInEditor(string filename)
        {
            // 遍历 Config.GenDataPath 文件夹, 用 AssetsDatabase
            var results = AssetDatabase.FindAssets($"t:TextAsset tb{filename}", new[] { Config.GenDataPath });
            if (results.Length <= 0)
            {
                throw new Exception($"{filename} not found");
            }
            
            foreach (var ret in results)
            {
                return AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(ret));
            }

            throw new Exception($"{filename} not found");
        }
        
        
        [MenuItem("VomitLib/LubanSupport/生成客户端数据")]
        public static async void GenerateClientData()
        {
            try
            {
                var config = Config;
            
                string cmd = GenerateCmd(GenMode.Client);
                await RunLuban(cmd);
                EditorUtility.DisplayDialog("生成客户端数据", "生成客户端数据成功", "确定");
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        [MenuItem("VomitLib/LubanSupport/生成服务器数据")]
        public static async void GenerateServerData()
        {
            try
            {
                var config = Config;

                if (config.GenServerCodePath.IsTrimNullOrEmpty() || config.GenServerDataPath.IsTrimNullOrEmpty())
                {
                    Log.Debug("服务器代码目录或数据目录为空, 不生成.");
                    return;
                }

                string cmd = GenerateCmd(GenMode.Server);
                await RunLuban(cmd);
                EditorUtility.DisplayDialog("生成服务器数据", "生成服务器数据成功", "确定");
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
        
        [MenuItem("VomitLib/LubanSupport/生成备份数据")]
        public static async void GenerateBackupData()
        {
            try
            {
                var config = Config;

                if (config.JsonBackupPath.IsNullOrEmpty())
                {
                    Log.Debug("备份数据目录为空, 不生成.");
                    return;
                }

                string cmd = GenerateCmd(GenMode.Backup);
                await RunLuban(cmd);
                EditorUtility.DisplayDialog("生成客户端数据", "生成客户端数据成功", "确定");
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        private static string GenerateCmd(GenMode genMode)
        {
            var config = Config;
            
            StringBuilder cmd = new();
            
            // 系统配置
            cmd.Append($"-t all ");                             // 目前全导出
            cmd.Append($"--conf \"{config.ConfigPath}\" ");     // Luban配置
            cmd.Append($"-x lineEnding=LF ");                   // 格式化
            // cmd.Append($"-x \"tableImporter.name=dxx\" ");   // 导入器
            
            // 代码生成
            if (genMode != GenMode.Backup)
            {
                var codeTarget = FormatCodeTarget(config.Format);
                cmd.Append($"-c {codeTarget} ");
                if (genMode == GenMode.Client)
                {
                    cmd.Append($"-x \"outputCodeDir={config.GenCodePath}\" ");
                }

                if (genMode == GenMode.Server)
                {
                    cmd.Append($"-x \"outputCodeDir={config.GenServerCodePath}\" ");
                }
            }
            
            // 数据生成
            var dataTarget = FormatDataTarget(genMode == GenMode.Backup ? LubanFormat.NewtonsoftJson : config.Format);
            cmd.Append($"-d \"{dataTarget}\" ");
            cmd.Append($"-x \"{dataTarget}.fileExt={(genMode == GenMode.Backup ? "json" : "bytes")}\" ");
            if (genMode == GenMode.Client)
            {
                cmd.Append($"-x \"{dataTarget}.outputDataDir={config.GenDataPath}\" ");    
            }
            else if (genMode == GenMode.Server)
            {
                cmd.Append($"-x \"{dataTarget}.outputDataDir={config.GenServerDataPath}\" ");    
            }
            else if (genMode == GenMode.Backup)
            {
                cmd.Append($"-x \"{dataTarget}.outputDataDir={config.JsonBackupPath}\" ");    
            }
            
            // 本地化
            if(config.LocalizationPath.IsNotNullAndEmpty())
            {
                cmd.Append($"-x l10n.provider=default -x \"l10n.textFile.path={config.LocalizationPath}\" -x l10n.textFile.keyFieldName=key");
            }
            
            return cmd.ToString();
            
            string FormatCodeTarget(LubanFormat format)
            {
                return format switch
                {
                    LubanFormat.NewtonsoftJson => "cs-newtonsoft-json",
                    LubanFormat.Bin => "cs-bin",
                    _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
                };
            }
            
            string FormatDataTarget(LubanFormat format)
            {
                return format switch
                {
                    LubanFormat.NewtonsoftJson => "json",
                    LubanFormat.Bin => "bin",
                    _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
                };
            }
        }

        private static async Task RunLuban(string cmd)
        {
            Debug.Log($"RunCmd : {cmd}");
            // 运行 bat
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C dotnet \"{Config.DllPath}\" " + cmd,       // 运行Luban
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
                    if (e.Data.Contains("|ERROR|"))
                    {
                        Debug.LogError($"<color=red>{e.Data}</color>");
                    }
                    else if (e.Data.Contains("|WARN|"))
                    {
                        Debug.Log($"<color=yellow>{e.Data}</color>");
                    }
                    else
                    {
                        Debug.Log(e.Data);    
                    }
                      
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