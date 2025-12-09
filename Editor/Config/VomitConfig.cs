using LubanSupport.Editor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Twenty2.VomitLib.Config
{
    [CreateAssetMenu(fileName = "VomitLibConfig", menuName = "VomitLib/CreateConfig", order = 0)]
    public class VomitConfig : ScriptableObject
    {
        [Header("View 配置")] 
        public ViewConfig ViewConfig;
        
        [Header("Luban 配置")] 
        public LubanConfig LubanConfig;
        
        [Header("SpriteAtlas 配置")] 
        public SpriteAtlasConfig SpriteAtlasConfig;
        
        [Header("Net 配置")] 
        public NetConfig NetConfig;


        [Header("工具栏")] 
        public bool ShowDataDirButton;
        public bool ShowGenerateDataButton;
        public bool ShowGenerateMsgButton;
        public bool ShowSlecteConfigButton = true;
        public bool ShowServerDirButton;
    }
}