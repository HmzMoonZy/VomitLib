using System;
using UnityEngine;
using Twenty2.VomitLib.LubanSupport;
using UnityEngine.Serialization;

namespace Twenty2.VomitLib.Config
{
    [Serializable]
    public class LubanConfig
    {
        [FormerlySerializedAs("ClientServerDllPath")]
        [Tooltip("Luban 动态库路径")]
        public string DllPath;

        [Tooltip("Luban 配置路径")]
        public string ConfigPath;

        [Tooltip("Luban 本地化表格路径")]
        public string LocalizationPath;

        [Tooltip("Luban 生成数据路径")]
        public string GenDataPath;

        [Tooltip("Luban 生成代码路径")]
        public string GenCodePath;

        [Tooltip("Luban 代码更格式化类型")]
        public LubanFormat Format;

        [Tooltip("Luban 是否风格化代码")]
        public bool NoneStyle = false;
    }
}