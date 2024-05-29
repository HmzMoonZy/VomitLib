using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using QFramework;
using Twenty2.VomitLib.Config;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor
{
    public class NetEditor : UnityEditor.Editor
    {
        [MenuItem("VomitLib/Net/启动服务器")]
        private static void LaunchServer()
        {
            var config = Vomit.RuntimeConfig.NetConfig;

            if (string.IsNullOrEmpty(config.ServerPath))
            {
                LogKit.E("未配置服务器路径");
                return;
            }
            
            var workPath = Path.Combine(Application.dataPath[..^6], config.ServerPath, "bin", "app_debug");
            var filePath = Path.Combine(workPath, "Geek.Server.App.exe");

            if (!File.Exists(filePath))
            {
                LogKit.E("未找到服务器文件");
                return;
            }
            
            try
            {
                using Process server = new Process();
                
                server.StartInfo.UseShellExecute = true;
                server.StartInfo.FileName = filePath;
                server.StartInfo.CreateNoWindow = false;
                server.StartInfo.WorkingDirectory = workPath;
                server.Start();
            }
            catch (Exception e)
            {
                LogKit.E(e.Message);
            }
        }
        
        [MenuItem("VomitLib/Net/Build Server")]
        private static void BuildServer()
        {
            var config = Vomit.RuntimeConfig.NetConfig;
            // 运行 bat
            ProcessStartInfo startInfo = new ProcessStartInfo 
            {
                
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal, 
                FileName = "cmd.exe",
                Arguments = "/k " + $"dotnet build {Path.Combine(config.ServerPath, "Geek.Server.sln")}",
                CreateNoWindow = false,
            };
            
            var process = Process.Start(startInfo);
            
            if (process == null) return;
            
            process.WaitForExit();
            
            process.Dispose();
        }

        [MenuItem("VomitLib/Net/同步协议")]
        private static void SynchronizeProtocol()
        {
            var config = Vomit.RuntimeConfig.NetConfig;
            var workPath = Path.Combine(Application.dataPath[..^6], config.ServerPath, "Tools", "Geek.MsgPackTool");
            var filePath = Path.Combine(workPath, "MessagePack.Generator.exe");
            
            using Process tool = new Process();
            tool.StartInfo.CreateNoWindow = false;
            tool.StartInfo.UseShellExecute = true;
            tool.StartInfo.FileName = filePath;
            tool.StartInfo.CreateNoWindow = false;
            tool.StartInfo.WorkingDirectory = workPath;
            tool.Start();
        }
    }
}