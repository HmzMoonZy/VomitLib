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
            GUILayout.BeginHorizontal(GUILayout.Width(350));
            
            if (GUILayout.Button("数据文件夹", GUILayout.Width(130)))
            {
                ClientDBEditor.OpenDataTableFolder();
            }
        
            if (GUILayout.Button("生成数据", GUILayout.Width(130)))
            {
                ClientDBEditor.GenerateData();
            }
      
            GUILayout.Space(5);

            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
        }

        static void OnDrawRightToolBar(){}
    }
}