using QFramework;
using UnityEngine;

namespace Twenty2.VomitLib
{
    public static class Vomit
    {
        private static VomitRuntimeConfig _vomitRuntimeConfig;

        public static IArchitecture Interface;
        
        public static void Init(IArchitecture architecture)
        {
            Interface = architecture;
        }

        public static VomitRuntimeConfig RuntimeConfig
        {
            get
            {
                if (_vomitRuntimeConfig == null)
                {
                    _vomitRuntimeConfig = Resources.Load<VomitRuntimeConfig>("VomitLibConfig");
                }

                return _vomitRuntimeConfig;
            }
        }
    }
}