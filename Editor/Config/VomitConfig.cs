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
    }
}