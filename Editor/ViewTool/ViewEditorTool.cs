using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using QFramework;
using Twenty2.VomitLib.Config;
using Twenty2.VomitLib.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using ViewConfig = Twenty2.VomitLib.View.ViewConfig;

namespace Twenty2.VomitLib.Editor
{
    public class ViewEditorTool
    {
        [MenuItem("VomitLib/View/生成ViewSortLayer")]
        public static void GenerateViewSortLayer()
        {
            var type = typeof(ViewSortLayer);

            // 先遍历枚举拿到枚举的字符串
            List<string> priorities = new List<string>();
            foreach (int v in Enum.GetValues(type))
            {
                priorities.Add(Enum.GetName(type, v));
            }

            // 清除数据
            SerializedObject tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty it = tagManager.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.name != "m_SortingLayers")
                {
                    continue;
                }

                // 先删除所有
                while (it.arraySize > 0)
                {
                    it.DeleteArrayElementAtIndex(0);
                }

                // 重新插入
                // 将枚举字符串生成到 sortingLayer
                foreach (var s in priorities)
                {
                    it.InsertArrayElementAtIndex(it.arraySize);
                    SerializedProperty dataPoint = it.GetArrayElementAtIndex(it.arraySize - 1);

                    while (dataPoint.NextVisible(true))
                    {
                        if (dataPoint.name == "name")
                        {
                            dataPoint.stringValue = s;
                        }
                        else if (dataPoint.name == "uniqueID")
                        {
                            dataPoint.intValue = (int) Enum.Parse(type, s);
                        }
                    }
                }
            }

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
        
        [MenuItem("Assets/Create/VomitLib/View/ViewScript")]
        public static void GenerateViewScript()
        {
            var selectCount = Selection.count;
            var selectName = Selection.activeObject.name;
            if (selectCount != 1) return;
            if (!selectName.StartsWith("View")) return;
            
            var folderPath = Path.Combine(Application.dataPath[..^7], VomitEditor.Config.ViewConfig.ScriptGeneratePath);
            if (VomitEditor.Config.ViewConfig.IsGenerateFolder)
            {
                folderPath = Path.Combine(folderPath, selectName);
            }
            var filePath = Path.Combine(folderPath, selectName + ".cs");
            var designerFilePath = Path.Combine(folderPath, selectName + ".Designer.cs");
            if (File.Exists(filePath) ||File.Exists(designerFilePath))
            {
                Debug.LogError("请手动删除对应的脚本后重试.");
                return;
            }

            if (VomitEditor.Config.ViewConfig.IsGenerateFolder)
            {
                Directory.CreateDirectory(folderPath);
            }


            string content = @$"using QFramework;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Twenty2.VomitLib.View;

public partial class {selectName} : ViewLogic
{{
    public override void OnOpened(ViewParameterBase param)
    {{
        
    }}

    public override void OnClose(ViewParameterBase param = null)
    {{
        
    }}

}}";

            File.WriteAllText(filePath, content, Encoding.UTF8);
            content = @$"using UnityEngine;
using UnityEngine.UI;
using Twenty2.VomitLib.View;

public partial class {selectName}
{{

}}
";
            File.WriteAllText(designerFilePath, content, Encoding.UTF8);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorPrefs.SetInt("__AUTO_BIND_VIEW_SCRIPTS__", 1);
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        public static void BindScript()
        {
            if (EditorPrefs.GetInt("__AUTO_BIND_VIEW_SCRIPTS__", 0) != 1)
            {
                return;
            }
            
            EditorPrefs.SetInt("__AUTO_BIND_VIEW_SCRIPTS__", 0); 
            
            if (Selection.activeObject == null || !Selection.activeObject.name.StartsWith("View"))
            {
                return;
            }
            
            var selectName = Selection.activeObject.name;
            
            // 用Linq实现
            Type bindType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).FirstOrDefault(type => type.Name == selectName);
            
            if (bindType != null)
            {
                var prefabPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
                prefabContents.AddComponent(bindType);
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);
                AssetDatabase.SaveAssets();
            }
        }
        
        
        
        [MenuItem("GameObject/UI/VomitCanvas")]
        public static void GenerateViewTemplate()
        {
            var canvas = new GameObject("VomitCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().pixelPerfect = true;
            canvas.GetComponent<Canvas>().additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                                                     AdditionalCanvasShaderChannels.Normal |
                                                                     AdditionalCanvasShaderChannels.Tangent;

            canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0;
            canvas.GetComponent<CanvasScaler>().referenceResolution =
                VomitEditor.Config.ViewConfig.ViewResolution;

            canvas.gameObject.layer = 5;
        }
        
        [MenuItem("GameObject/UI/VomitCanvas(No Raycast)")]
        public static void GenerateViewNoRaycastTemplate()
        {
            var canvas = new GameObject("VomitCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().pixelPerfect = true;
            canvas.GetComponent<Canvas>().additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                                                     AdditionalCanvasShaderChannels.Normal |
                                                                     AdditionalCanvasShaderChannels.Tangent;

            canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0;
            canvas.GetComponent<CanvasScaler>().referenceResolution =
                VomitEditor.Config.ViewConfig.ViewResolution;

            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            
            canvas.gameObject.layer = 5;
        }
    }
}