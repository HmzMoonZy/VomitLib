using UnityEngine;

namespace Twenty2.VomitLib.Config
{
    [CreateAssetMenu(fileName = "VomitLibConfig", menuName = "VomitLib/CreateConfig", order = 0)]
    public class VomitRuntimeConfig : ScriptableObject
    {
        [Header("View 配置")] 
        public ViewFrameworkConfig ViewFrameworkConfig;

        [Header("ClientDB 配置")] 
        public ClientDBConfig ClientDBConfig;

        [Header("Audio 配置")]
        public AudioConfig AudioConfig;
    }
}