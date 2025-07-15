using System;
using UnityEngine;

namespace Twenty2.VomitLib.Config
{
    [Serializable]
    public class NetConfig
    {
        [Header("服务器路径配置")]
        [Tooltip("相对于Unity项目根目录的服务器路径")]
        public string ServerPath = "Server/";
    }
}