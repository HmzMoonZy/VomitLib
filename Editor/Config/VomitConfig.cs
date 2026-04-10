using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Twenty2.VomitLib.Config
{
    [FilePath("ProjectSettings/VomitLibConfig.asset", FilePathAttribute.Location.ProjectFolder)]
    public class VomitConfig : ScriptableSingleton<VomitConfig>
    {
        [Header("Code Gen 配置")]
        public CodeGenConfig CodeGenConfig;

        [Header("View 配置")]
        public ViewConfig ViewConfig;

        [Header("Luban 配置")]
        public LubanConfig LubanConfig;

        [Header("Net 配置")]
        public NetConfig NetConfig;

        [Header("工具栏")]
        public bool ShowDataDirButton;
        public bool ShowGenerateDataButton;
        public bool ShowGenerateMsgButton;
        public bool ShowSlecteConfigButton = true;
        public bool ShowServerDirButton;

        public void Save() => Save(true);
    }
}
