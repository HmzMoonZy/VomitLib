using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Twenty2.VomitLib
{
    using System;
    using UnityEngine;
    
    public class LogKit
    {
        private static LogLevel mLogLevel = LogLevel.Info;

        public static LogLevel Level
        {
            get => mLogLevel;
            set => mLogLevel = value;
        }
        
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Editor(object msg, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                Debug.Log(msg);
            }
            else
            {
                Debug.LogFormat(msg.ToString(), args);
            }
        }
        
        public static void I(object msg, params object[] args)
        {
            if (mLogLevel < LogLevel.Info)
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


        public enum LogLevel
        {
            Disable = 0,
            Exception = 1,
            Error = 2,
            Warning = 3,
            Info = 4,
        }
    }
}