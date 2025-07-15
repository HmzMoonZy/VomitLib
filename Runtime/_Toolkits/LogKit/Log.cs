//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Diagnostics;

namespace Twenty2.VomitLib
{
    /// <summary>
    /// 日志工具集。
    /// </summary>
    public static class Log
    {
        private static ILogHelper s_LogHelper = null;

        #if UNITY_EDITOR
        static Log()
        {
            s_LogHelper ??= new DefaultLogHelper();
        }
        #endif

        public static void SetLogHelper(ILogHelper logHelper)
        {
            s_LogHelper = logHelper ?? new DefaultLogHelper();
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug(object message)
        {
            s_LogHelper?.Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录一般性日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Info(object message)
        {
            s_LogHelper?.Log(LogLevel.Info, message);
        }

        /// <summary>
        /// 打印警告级别日志，用于记录警告类日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Warning(object message)
        {
            s_LogHelper?.Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// 打印错误级别日志，用于记录错误类日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Error(object message)
        {
            s_LogHelper?.Log(LogLevel.Error, message);
        }

        /// <summary>
        /// 打印严重错误级别日志，用于记录严重错误类日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Fatal(object message)
        {
            s_LogHelper?.Log(LogLevel.Fatal, message);
        }
    }
}