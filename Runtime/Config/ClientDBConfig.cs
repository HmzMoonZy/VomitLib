using System;
using System.Collections.Generic;

namespace Twenty2.VomitLib.Config
{
    [Serializable]
    public class ClientDBConfig
    {
        public string ClientServerDllPath;

        public string ConfigPath;

        public string LocalizationPath;

        public string JsonOutputPath;

        public string GenCodePath;

        public List<(QFramework.Language key, string locationKey)> LocationMap;
    }
}