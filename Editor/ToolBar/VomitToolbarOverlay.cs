using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using LubanSupport.Editor;
using Twenty2.VomitLib.Config;

namespace Twenty2.VomitLib.Editor
{
    // ---- 按钮元素 ----

    [EditorToolbarElement(id, typeof(SceneView))]
    class ToolbarBtnDataDir : EditorToolbarButton
    {
        public const string id = "VomitLib/DataDir";

        public ToolbarBtnDataDir()
        {
            text = "数据文件夹";
            tooltip = "打开数据文件夹";
            clicked += () => OpenFolder.OpenFolderDesign();
            if (!VomitEditor.Config.ShowDataDirButton)
                style.display = UnityEngine.UIElements.DisplayStyle.None;
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class ToolbarBtnGenData : EditorToolbarButton
    {
        public const string id = "VomitLib/GenData";

        public ToolbarBtnGenData()
        {
            text = "生成数据";
            tooltip = "生成客户端/服务端/备份数据";
            clicked += () =>
            {
                LubanTool.GenerateClientData();
                LubanTool.GenerateServerData();
                LubanTool.GenerateBackupData();
            };
            if (!VomitEditor.Config.ShowGenerateDataButton)
                style.display = UnityEngine.UIElements.DisplayStyle.None;
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class ToolbarBtnGenMsg : EditorToolbarButton
    {
        public const string id = "VomitLib/GenMsg";

        public ToolbarBtnGenMsg()
        {
            text = "生成协议";
            tooltip = "生成 MessagePack 协议";
            clicked += () => MessagePack.MessagePackTool.GenerateMessagePackProtocol();
            if (!VomitEditor.Config.ShowGenerateMsgButton)
                style.display = UnityEngine.UIElements.DisplayStyle.None;
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class ToolbarBtnConfig : EditorToolbarButton
    {
        public const string id = "VomitLib/Config";

        public ToolbarBtnConfig()
        {
            text = "配置文件";
            tooltip = "选中 VomitLib 配置文件";
            clicked += () =>
            {
                var guids = AssetDatabase.FindAssets($"t:{nameof(VomitConfig)}");
                if (guids.Length > 0)
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
            };
            if (!VomitEditor.Config.ShowSlecteConfigButton)
                style.display = UnityEngine.UIElements.DisplayStyle.None;
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class ToolbarBtnServerDir : EditorToolbarButton
    {
        public const string id = "VomitLib/ServerDir";

        public ToolbarBtnServerDir()
        {
            text = "服务器文件夹";
            tooltip = "打开服务器文件夹";
            clicked += () => OpenFolder.OpenFolderServer();
            if (!VomitEditor.Config.ShowServerDirButton)
                style.display = UnityEngine.UIElements.DisplayStyle.None;
        }
    }

    // ---- 组合成 Overlay ----

    [Overlay(typeof(SceneView), "VomitLib Tools")]
    public class VomitToolbarOverlay : ToolbarOverlay
    {
        VomitToolbarOverlay() : base(
            ToolbarBtnDataDir.id,
            ToolbarBtnGenData.id,
            ToolbarBtnGenMsg.id,
            ToolbarBtnConfig.id,
            ToolbarBtnServerDir.id
        )
        { }
    }
}
