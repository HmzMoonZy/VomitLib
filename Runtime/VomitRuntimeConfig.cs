using QFramework;
using Twenty2.VomitLib.ClientDB;
using Twenty2.VomitLib.View;
using UnityEngine;

namespace Twenty2.VomitLib
{
    [CreateAssetMenu(fileName = "VomitLibConfig", menuName = "VomitLib/CreateConfig", order = 0)]
    public class VomitRuntimeConfig : ScriptableObject
    {
        [Header("QFramework 架构名称")]
        public string ArchitectureName;
        
        [Header("View 配置")] 
        public ViewFrameworkConfig ViewFrameworkConfig;

        [Header("ClientDB 配置")] 
        public ClientDBConfig ClientDBConfig;
    }
}