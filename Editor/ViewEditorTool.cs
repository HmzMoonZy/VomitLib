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
            
            var folderPath = Path.Combine(Application.dataPath[..^7], Vomit.GetConfigInEditor().ViewFrameworkConfig.ScriptGeneratePath);
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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }
        
        
        [UnityEditor.Callbacks.DidReloadScripts]
        static void Bind()
        {
            if (Selection.activeObject is not GameObject)
            {
                return;
            }
            
            // 绑定脚本
            var instance = PrefabUtility.InstantiatePrefab(Selection.activeObject) as GameObject;
            if (instance == null)
            {
                return;
            }

            if (instance.GetComponent<ViewConfig>() == null)
            {
                Object.DestroyImmediate(instance);
                return;
            }

            var logic = instance.GetComponent<ViewLogic>();
            if (logic != null)
            {
                Object.DestroyImmediate(instance);
                return;
            }
        
            var type = Assembly.Load("Assembly-CSharp").GetType(Selection.activeObject.name);
            var addComponent = instance.AddComponent(type);
            if (addComponent == null)
            {
                LogKit.E("没有!");
            }

            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
        
            Object.DestroyImmediate(instance);
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
                Vomit.GetConfigInEditor().ViewFrameworkConfig.ViewResolution;

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
                Vomit.GetConfigInEditor().ViewFrameworkConfig.ViewResolution;

            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            
            canvas.gameObject.layer = 5;
        }
    }
}