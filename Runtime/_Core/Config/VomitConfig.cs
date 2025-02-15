using UnityEngine;
using UnityEngine.Serialization;

namespace Twenty2.VomitLib.Config
{
    [CreateAssetMenu(fileName = "VomitLibConfig", menuName = "VomitLib/CreateConfig", order = 0)]
    public class VomitConfig : ScriptableObject
    {
        [FormerlySerializedAs("_viewConfig")] [Header("View 配置")] 
        public ViewConfig ViewConfig;

        [FormerlySerializedAs("ClientDBConfig")] [Header("ClientDB 配置")] 
        public ClientDBConfig ClientDatabaseConfig;

        [Header("Audio 配置")]
        public AudioConfig AudioConfig;

        [Header("网络 配置")]
        public NetConfig NetConfig;
    }
}