using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using QFramework;
using Twenty2.VomitLib.Config;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Server
{
    public static class ServerManagerTool
    {
        private static Process _serverProcess;
        private static bool _isServerRunning;
        private static ServerOutputWindow _outputWindow;

        public static bool IsServerRunning => _isServerRunning && _serverProcess != null && !_serverProcess.HasExited;

        public static event Action<bool> OnServerStatusChanged;
        public static event Action<string> OnServerOutput;

        [MenuItem("VomitLib/Server/启动服务器 %#S")]
        public static async void StartServer()
        {
            if (IsServerRunning)
            {
                Log.Warning("服务器已在运行中");
                return;
            }

            var config = VomitEditor.Config.NetConfig;
            if (config == null)
            {
                Log.Error("未找到服务器配置，请检查VomitConfig");
                return;
            }

            if (string.IsNullOrEmpty(config.ServerPath))
            {
                Log.Error("未配置服务器路径，请在VomitConfig中设置ServerPath");
                return;
            }

            try
            {
                // 确保先构建服务器
                if (!await BuildServer())
                {
                    Log.Error("服务器构建失败");
                    return;
                }

                await StartServerProcess(config);
            }
            catch (Exception e)
            {
                Log.Error($"启动服务器失败: {e.Message}");
            }
        }

        [MenuItem("VomitLib/Server/停止服务器 %#X")]
        public static void StopServer()
        {
            if (!IsServerRunning)
            {
                Log.Warning("服务器未运行");
                return;
            }

            try
            {
                _serverProcess?.Kill();
                _serverProcess?.Dispose();
                _serverProcess = null;
                _isServerRunning = false;
                
                OnServerStatusChanged?.Invoke(false);
                Log.Info("服务器已停止");
            }
            catch (Exception e)
            {
                Log.Error($"停止服务器失败: {e.Message}");
            }
        }

        [MenuItem("VomitLib/Server/重启服务器 %#R")]
        public static async void RestartServer()
        {
            if (IsServerRunning)
            {
                StopServer();
                await Task.Delay(2000); // 等待2秒确保进程完全退出
            }
            StartServer();
        }

        [MenuItem("VomitLib/Server/构建服务器")]
        public static async void BuildServerMenu()
        {
            await BuildServer();
        }

        [MenuItem("VomitLib/Server/服务器输出窗口")]
        public static void ShowOutputWindow()
        {
            if (_outputWindow == null)
            {
                _outputWindow = EditorWindow.GetWindow<ServerOutputWindow>("服务器输出");
            }
            _outputWindow.Show();
        }

        [MenuItem("VomitLib/Server/打开服务器文件夹")]
        public static void OpenServerFolder()
        {
            var config = VomitEditor.Config.NetConfig;
            if (config == null || string.IsNullOrEmpty(config.ServerPath))
            {
                Log.Error("未配置服务器路径");
                return;
            }

            var serverPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), config.ServerPath);
            if (Directory.Exists(serverPath))
            {
                EditorUtility.RevealInFinder(serverPath);
            }
            else
            {
                Log.Error($"服务器路径不存在: {serverPath}");
            }
        }

        public static async Task<bool> BuildServer()
        {
            var config = VomitEditor.Config.NetConfig;
            if (config == null || string.IsNullOrEmpty(config.ServerPath))
            {
                Log.Error("未配置服务器路径");
                return false;
            }

            var serverPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), config.ServerPath);
            var solutionPath = Path.Combine(serverPath, "Geek.Server.sln");

            if (!File.Exists(solutionPath))
            {
                Log.Error($"未找到服务器解决方案文件: {solutionPath}");
                return false;
            }

            try
            {
                Log.Info("开始构建服务器...");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{solutionPath}\" --configuration Debug",
                    WorkingDirectory = serverPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                
                var outputReceived = false;
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputReceived = true;
                        Log.Info($"[Build] {e.Data}");
                        OnServerOutput?.Invoke($"[Build] {e.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputReceived = true;
                        Log.Warning($"[Build Error] {e.Data}");
                        OnServerOutput?.Invoke($"[Build Error] {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode == 0)
                {
                    Log.Info("服务器构建成功");
                    return true;
                }
                else
                {
                    Log.Error($"服务器构建失败，退出代码: {process.ExitCode}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Error($"构建服务器时发生异常: {e.Message}");
                return false;
            }
        }

        private static async Task StartServerProcess(NetConfig config)
        {
            var serverPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), config.ServerPath);
            var workPath = Path.Combine(serverPath, "bin", "app_debug");
            var exePath = Path.Combine(workPath, "Geek.Server.App.exe");

            if (!File.Exists(exePath))
            {
                Log.Error($"未找到服务器可执行文件: {exePath}");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = workPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _serverProcess = new Process { StartInfo = startInfo };

            _serverProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log.Info($"[Server] {e.Data}");
                    OnServerOutput?.Invoke($"[Server] {e.Data}");
                }
            };

            _serverProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log.Warning($"[Server Error] {e.Data}");
                    OnServerOutput?.Invoke($"[Server Error] {e.Data}");
                }
            };

            _serverProcess.Exited += (sender, e) =>
            {
                _isServerRunning = false;
                OnServerStatusChanged?.Invoke(false);
                Log.Info("服务器进程已退出");
            };

            _serverProcess.EnableRaisingEvents = true;

            try
            {
                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();
                
                _isServerRunning = true;
                OnServerStatusChanged?.Invoke(true);
                
                Log.Info("服务器启动成功");
            }
            catch (Exception e)
            {
                Log.Error($"启动服务器进程失败: {e.Message}");
                _serverProcess?.Dispose();
                _serverProcess = null;
            }
        }

        // 清理资源
        static ServerManagerTool()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += OnEditorQuitting;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 可选：当进入播放模式时停止服务器以避免冲突
            // if (state == PlayModeStateChange.EnteredPlayMode && IsServerRunning)
            // {
            //     StopServer();
            // }
        }

        private static void OnEditorQuitting()
        {
            if (IsServerRunning)
            {
                StopServer();
            }
        }
    }
}