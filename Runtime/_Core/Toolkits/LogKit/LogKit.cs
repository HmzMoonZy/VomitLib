/****************************************************************************
 * Copyright (c) 2016 - 2023 liangxiegame UNDER MIT License
 *
 * https://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 *
 ****************************************************************************/

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace QFramework
{
    using System;
    using UnityEngine;


    public class LogKit
    {

        public static void I(object msg, params object[] args)
        {
            if (mLogLevel < LogLevel.Normal)
            {
                return;
            }

            if (args == null || args.Length == 0)
            {
                Debug.Log(msg);
            }
            else
            {
                Debug.LogFormat(msg.ToString(), args);
            }
        }
        
        public static void W(object msg, params object[] args)
        {
            if (mLogLevel < LogLevel.Warning)
            {
                return;
            }

            if (args == null || args.Length == 0)
            {
                Debug.LogWarning(msg);
            }
            else
            {
                Debug.LogWarningFormat(msg.ToString(), args);
            }
        }
        
        public static void E(object msg, params object[] args)
        {
            if (mLogLevel < LogLevel.Error)
            {
                return;
            }

            if (args == null || args.Length == 0)
            {
                Debug.LogError(msg);
            }
            else
            {
                Debug.LogError(string.Format(msg.ToString(), args));
            }
        }
        
        public static void E(Exception e)
        {
            if (mLogLevel < LogLevel.Exception)
            {
                return;
            }

            Debug.LogException(e);
        }
        
        public static StringBuilder Builder()
        {
            return new StringBuilder();
        }


        public enum LogLevel
        {
            None = 0,
            Exception = 1,
            Error = 2,
            Warning = 3,
            Normal = 4,
            Max = 5,
        }

        private static LogLevel mLogLevel = LogLevel.Normal;

        public static LogLevel Level
        {
            get => mLogLevel;
            set => mLogLevel = value;
        }
    }

    public static class LogKitExtension
    {
        public static StringBuilder GreenColor(this StringBuilder self, Action<StringBuilder> childContent)
        {
            self.Append("<color=green>");
            childContent?.Invoke(self);
            self.Append("</color>");
            return self;
        }

        public static void LogInfo<T>(this T selfMsg)
        {
            LogKit.I(selfMsg);
        }

        public static void LogWarning<T>(this T selfMsg)
        {
            LogKit.W(selfMsg);
        }

        public static void LogError<T>(this T selfMsg)
        {
            LogKit.E(selfMsg);
        }

        public static void LogException(this Exception selfExp)
        {
            LogKit.E(selfExp);
        }
    }

#if UNITY_EDITOR
    // * 参考: https://zhuanlan.zhihu.com/p/92291084
    // 感谢 https://github.com/clksaaa 提供的 issue 反馈 和 解决方案
    public static class OpenAssetLogLine
    {
        private static bool m_hasForceMono = false;

        // 处理asset打开的callback函数
        [UnityEditor.Callbacks.OnOpenAssetAttribute(-1)]
        static bool OnOpenAsset(int instance, int line)
        {
            if (m_hasForceMono) return false;

            // 自定义函数，用来获取log中的stacktrace，定义在后面。
            string stack_trace = GetStackTrace();
            // 通过stacktrace来定位是否是自定义的log，log中有LogKit/LogKit.cs，很好识别
            if (!string.IsNullOrEmpty(stack_trace) && stack_trace.Contains("LogKit/LogKit.cs"))
            {
                // 正则匹配at xxx，在第几行
                Match matches = Regex.Match(stack_trace, @"\(at (.+)\)", RegexOptions.IgnoreCase);
                string pathline = "";
                while (matches.Success)
                {
                    pathline = matches.Groups[1].Value;

                    // 找到不是我们自定义log文件的那行，重新整理文件路径，手动打开
                    if (!pathline.Contains("LogKit/LogKit.cs") && !string.IsNullOrEmpty(pathline))
                    {
                        int split_index = pathline.LastIndexOf(":");
                        string path = pathline.Substring(0, split_index);
                        line = Convert.ToInt32(pathline.Substring(split_index + 1));
                        m_hasForceMono = true;
                        //方式一
                        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path), line);
                        m_hasForceMono = false;
                        //方式二
                        //string fullpath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets"));
                        // fullpath = fullpath + path;
                        //  UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fullpath.Replace('/', '\\'), line);
                        return true;
                    }

                    matches = matches.NextMatch();
                }

                return true;
            }

            return false;
        }

        static string GetStackTrace()
        {
// 找到类UnityEditor.ConsoleWindow
            var type_console_window = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
// 找到UnityEditor.ConsoleWindow中的成员ms_ConsoleWindow
            var filedInfo =
                type_console_window.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
// 获取ms_ConsoleWindow的值
            var ConsoleWindowInstance = filedInfo.GetValue(null);
            if (ConsoleWindowInstance != null)
            {
                if ((object)EditorWindow.focusedWindow == ConsoleWindowInstance)
                {
// 找到类UnityEditor.ConsoleWindow中的成员m_ActiveText
                    filedInfo = type_console_window.GetField("m_ActiveText",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    string activeText = filedInfo.GetValue(ConsoleWindowInstance).ToString();
                    return activeText;
                }
            }

            return null;
        }
    }
#endif
}