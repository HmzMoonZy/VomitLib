using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine.Serialization;

namespace Twenty2.VomitLib.ClientDB
{
    [Serializable]
    public struct LocalizationKeyMap
    {
        public Language Language;

        public string Key;
    }
    
    [Serializable]
    public class ClientDBConfig
    {
        public string ClientServerDllPath;

        public string ConfigPath;

        public string ExcelPath;

        public string LocalizationPath;

        public string JsonOutputPath;

        public string GenCodePath;
        
        // TODO 多语言
        public List<LocalizationKeyMap> LocalizationKes;
    }
}