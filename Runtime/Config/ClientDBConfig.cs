using System;
using System.Collections.Generic;

namespace Twenty2.VomitLib.Config
{
    [Serializable]
    public class ClientDBConfig
    {
        [Serializable]
        public struct Language2Key
        {
            public QFramework.Language Key;
            
            public string LocalizationKey;
        }
        
        public string ClientServerDllPath;

        public string ConfigPath;

        public string LocalizationPath;

        public string JsonOutputPath;

        public string GenCodePath;
        
        public List<Language2Key> LocationMap;
    }
}