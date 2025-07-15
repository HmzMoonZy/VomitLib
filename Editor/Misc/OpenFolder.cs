//------------------------------------------------------------
// Copy by Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using LubanSupport.Editor;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor
{
    /// <summary>
    /// 打开文件夹相关的实用函数。
    /// </summary>
    public static class OpenFolder
    {
        /// <summary>
        /// 打开 配置表 文件夹。
        /// </summary>
        [MenuItem("VomitLib/Open Folder/Design Path", false, 9)]
        public static void OpenFolderDesign()
        {
            var directoryInfo = new FileInfo(LubanTool.Config.ConfigPath).Directory;
            if (directoryInfo != null)
            {
                Execute(directoryInfo.FullName);
            }
        }

        [MenuItem("VomitLib/Open Folder/Server Path", false, 10)]
        public static void OpenFolderServer()
        {
            var directoryInfo = new FileInfo(VomitEditor.Config.NetConfig.ServerPath).Directory;
            if (directoryInfo != null)
            {
                Execute(directoryInfo.FullName);
            }
        }
        
        /// <summary>
        /// 打开 Data Path 文件夹。
        /// </summary>
        [MenuItem("VomitLib/Open Folder/Data Path", false, 10)]
        public static void OpenFolderDataPath()
        {
            Execute(Application.dataPath);
        }

        /// <summary>
        /// 打开 Persistent Data Path 文件夹。
        /// </summary>
        [MenuItem("VomitLib/Open Folder/Persistent Data Path", false, 11)]
        public static void OpenFolderPersistentDataPath()
        {
            Execute(Application.persistentDataPath);
        }

        /// <summary>
        /// 打开 Streaming Assets Path 文件夹。
        /// </summary>
        [MenuItem("VomitLib/Open Folder/Streaming Assets Path", false, 12)]
        public static void OpenFolderStreamingAssetsPath()
        {
            Execute(Application.streamingAssetsPath);
        }

        /// <summary>
        /// 打开 Temporary Cache Path 文件夹。
        /// </summary>
        [MenuItem("VomitLib/Open Folder/Temporary Cache Path", false, 13)]
        public static void OpenFolderTemporaryCachePath()
        {
            Execute(Application.temporaryCachePath);
        }

#if UNITY_2018_3_OR_NEWER

        /// <summary>
        /// 打开 Console Log Path 文件夹。
        /// </summary>
        [MenuItem("VomitLib/Open Folder/Console Log Path", false, 14)]
        public static void OpenFolderConsoleLogPath()
        {
            Execute(System.IO.Path.GetDirectoryName(Application.consoleLogPath));
        }

#endif

        /// <summary>
        /// 打开指定路径的文件夹。
        /// </summary>
        /// <param name="folder">要打开的文件夹的路径。</param>
        public static void Execute(string folder)
        {
            folder = $"\"{folder}\"";
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    Process.Start("Explorer.exe", folder.Replace('/', '\\'));
                    break;

                case RuntimePlatform.OSXEditor:
                    Process.Start("open", folder);
                    break;

                default:
                    throw new Exception($"Not support open folder on '{Application.platform}' platform.");
            }
        }
    }
}
