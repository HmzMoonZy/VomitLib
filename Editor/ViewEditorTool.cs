using System;
using System.Collections.Generic;
using System.IO;
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
        
        [MenuItem("VomitLib/View/删除初次打开Token")]
        public static void DeleteAllKeys()
        {
            var folderPath = Path.Combine(Application.dataPath[..^7], Vomit.GetConfigInEditor().ViewConfig.ScriptGeneratePath);
            DirectoryInfo dir = new(folderPath);

            foreach (var fileInfo in dir.GetFiles("*.cs"))
            {
                var viewName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                
                PlayerPrefs.DeleteKey($"__FIRST__{viewName}");
            }
        }

        [MenuItem("Assets/Create/VomitLib/View/ViewScript")]
        public static void GenerateViewScript()
        {
            var selectCount = Selection.count;
            var selectName = Selection.activeObject.name;
            if (selectCount != 1) return;
            if (!selectName.StartsWith("View")) return;
            
            var folderPath = Path.Combine(Application.dataPath[..^7], Vomit.GetConfigInEditor().ViewConfig.ScriptGeneratePath);
            var filePath = Path.Combine(folderPath, selectName + ".cs");
            var designerFilePath = Path.Combine(folderPath, selectName + ".Designer.cs");
            if (File.Exists(filePath) ||File.Exists(designerFilePath))
            {
                Debug.LogError("请手动删除对应的脚本后重试.");
                return;
            }


            string content = @$"using QFramework;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Twenty2.VomitLib.View;

public partial class {selectName} : ViewLogic
{{
    public override UniTask OnOpened(ViewParameterBase param)
    {{
        return UniTask.CompletedTask;
    }}

    public override UniTask OnClose()
    {{
        return UniTask.CompletedTask;
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

            PlayerPrefs.SetInt("__AUTO_BIND_VIEW_SCRIPTS__", 1);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        public static void BindScript()
        {
            if (PlayerPrefs.GetInt("__AUTO_BIND_VIEW_SCRIPTS__", 0) != 1)
            {
                return;
            }
            PlayerPrefs.SetInt("__AUTO_BIND_VIEW_SCRIPTS__", 0); 
            var selectCount = Selection.count;
            var selectName = Selection.activeObject.name;
            var instantiatePrefab = PrefabUtility.InstantiatePrefab(Selection.activeObject) as GameObject;
            Type bindType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == selectName)
                    {
                        bindType = type;
                        break;
                    }
                }

                if (bindType != null)
                {
                    break;
                }
            }

            if (bindType != null)
            {
                instantiatePrefab!.AddComponent(bindType);
                PrefabUtility.ApplyAddedComponent(instantiatePrefab.GetComponent(bindType),
                    AssetDatabase.GetAssetPath(Selection.activeInstanceID), InteractionMode.AutomatedAction);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            
            Object.DestroyImmediate(instantiatePrefab);
           
        }
        
        
        
        [MenuItem("GameObject/UI/VomitCanvas")]
        public static void GenerateViewTemplate()
        {
            var canvas = new GameObject("VomitCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ViewConfig));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().pixelPerfect = true;
            canvas.GetComponent<Canvas>().additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                                                     AdditionalCanvasShaderChannels.Normal |
                                                                     AdditionalCanvasShaderChannels.Tangent;

            canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0;
            canvas.GetComponent<CanvasScaler>().referenceResolution =
                Vomit.GetConfigInEditor().ViewConfig.ViewResolution;

            canvas.gameObject.layer = 5;
        }
        
        [MenuItem("GameObject/UI/VomitCanvas(No Raycast)")]
        public static void GenerateViewNoRaycastTemplate()
        {
            var canvas = new GameObject("VomitCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ViewConfig));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().pixelPerfect = true;
            canvas.GetComponent<Canvas>().additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                                                     AdditionalCanvasShaderChannels.Normal |
                                                                     AdditionalCanvasShaderChannels.Tangent;

            canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0;
            canvas.GetComponent<CanvasScaler>().referenceResolution =
                Vomit.GetConfigInEditor().ViewConfig.ViewResolution;

            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            
            canvas.gameObject.layer = 5;
        }
    }
}