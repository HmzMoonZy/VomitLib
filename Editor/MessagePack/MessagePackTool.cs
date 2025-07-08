using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Twenty2.VomitLib.Editor.MessagePack
{
    public class MessagePackTool : UnityEditor.Editor
    {
        private static string MessagePackGeneratorPath => Path.Combine(Application.dataPath.Replace("/Assets", ""), "Tools/Geek.MsgPackTool/MessagePack.Generator.exe");
        
        private static string ProtoGenPath => Path.Combine(Application.dataPath, "GameMain/Scripts/Proto/Gen");

        [MenuItem("VomitLib/Protocol/生成MessagePack协议")]
        public static void GenerateMessagePackProtocol()
        {
            try
            {
                if (!File.Exists(MessagePackGeneratorPath))
                {
                    Debug.LogError($"MessagePack.Generator.exe未找到，路径: {MessagePackGeneratorPath}");
                    EditorUtility.DisplayDialog("错误", "MessagePack.Generator.exe未找到", "确定");
                    return;
                }

                if (!Directory.Exists(ProtoGenPath))
                {
                    Debug.LogError($"协议生成目录不存在，路径: {ProtoGenPath}");
                    EditorUtility.DisplayDialog("错误", "协议生成目录不存在", "确定");
                    return;
                }

                RunToolDirectly();
            }
            catch (Exception e)
            {
                Debug.LogError($"生成MessagePack协议失败: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"生成协议失败: {e.Message}", "确定");
            }
        }

        private static void RunToolDirectly()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = MessagePackGeneratorPath,
                    WorkingDirectory = Path.GetDirectoryName(MessagePackGeneratorPath),
                    UseShellExecute = true,  // 这样会在新的控制台窗口中打开
                    CreateNoWindow = false
                };

                Process.Start(startInfo);
            }
            catch (Exception e)
            {
                Debug.LogError($"启动MessagePack生成工具失败: {e.Message}");
                throw;
            }
        }

    }
}