using System;

namespace Twenty2.VomitLib.ClientDB
{
    [Serializable]
    public class ClientDBConfig
    {
        public string ClientServerDllPath;

        public string ConfigPath;

        public string LocalizationPath;

        public string JsonOutputPath;

        public string GenCodePath;
        
        // TODO 多语言
    }
}