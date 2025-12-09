using System.Diagnostics;
using System.IO;
using LubanSupport.Editor;
using Twenty2.VomitLib.Config;
using Twenty2.VomitLib.Editor.MessagePack;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor
{
    [InitializeOnLoad]
    public class ToolBarMenu
    {  
        static ToolBarMenu()
        {
            if (Application.isBatchMode)
            {
                return;
            }
            ToolbarExtender.LeftToolbarGUI.Add(OnDrawLeftToolBar);
            ToolbarExtender.RightToolbarGUI.Add(OnDrawRightToolBar);
        }

        static void OnDrawLeftToolBar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginHorizontal(GUILayout.Width(500));
            
            if (VomitEditor.Config.ShowDataDirButton)
            {
                if (GUILayout.Button("数据文件夹", GUILayout.Width(80)))
                {
                    OpenFolder.OpenFolderDesign();
                }
            }

            if (VomitEditor.Config.ShowGenerateDataButton)
            {
                if (GUILayout.Button("生成数据",  GUILayout.Width(80)))
                {
                    LubanTool.GenerateClientData();
                    LubanTool.GenerateServerData();
                }
            }
            
            if (VomitEditor.Config.ShowGenerateMsgButton)
            {
                if(GUILayout.Button("生成协议",  GUILayout.Width(80)))
                {
                    MessagePackTool.GenerateMessagePackProtocol();
                }    
            }

            if (VomitEditor.Config.ShowSlecteConfigButton)
            {
                if (GUILayout.Button("配置文件",  GUILayout.Width(80)))
                {
                    var findAsset = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(VomitConfig)}")[0];
                    var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(UnityEditor.AssetDatabase.GUIDToAssetPath(findAsset));
                    UnityEditor.EditorGUIUtility.PingObject(obj);
                    //在Project面板自动选中，并在Inspector面板显示详情
                    UnityEditor.Selection.activeObject = obj;
                }
            }

            if (VomitEditor.Config.ShowServerDirButton)
            {
                if (GUILayout.Button("服务器文件夹", GUILayout.Width(80)))
                {
                    OpenFolder.OpenFolderServer();
                }
            }
            
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
        }

        static void OnDrawRightToolBar(){}
    }
}