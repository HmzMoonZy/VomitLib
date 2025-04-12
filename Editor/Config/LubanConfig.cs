using System;
using UnityEngine;
using Twenty2.VomitLib.LubanSupport;

namespace Twenty2.VomitLib.Config
{
    [Serializable]
    public class LubanConfig
    {
        public string ClientServerDllPath;

        public string ConfigPath;

        public string LocalizationPath;

        public string GenDataPath;

        public string GenCodePath;

        public LubanFormat Format;

        public bool NoneStyle = false;
    }
}