using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Twenty2.VomitLib.Config
{
    [Serializable]
    public class ClientDBConfig
    {
        public enum JsonFormat
        {
            [Obsolete("请使用 NewtonsoftJson")]
            SimpleJson,
            
            NewtonsoftJson,
            
            Bin,
        }
        
        public string ClientServerDllPath;

        public string ConfigPath;

        public string LocalizationPath;

        public string GenDataPath;

        public string GenCodePath;

        public JsonFormat Format;


    }
}