using System;
using System.Collections.Generic;

namespace Twenty2.VomitLib.Config
{
    [Serializable]
    public class ClientDBConfig
    {
        public enum JsonFormat
        {
            SimpleJson,
            
            NewtonsoftJson,
            
            Bin,
        }
        
        public string ClientServerDllPath;

        public string ConfigPath;

        public string LocalizationPath;

        public string JsonOutputPath;

        public string GenCodePath;

        public JsonFormat Format;


    }
}