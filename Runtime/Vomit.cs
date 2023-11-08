using System;
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

            // 遍历所有异步事件
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsValueType || !type.HasAttribute<AsyncEventAttribute>()) continue;
                    
                    AsyncEventExtension.Register(type.Name);
                }
            }
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

    public class MonoController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture()
        {
            return Vomit.Interface;
        }
        
        protected void RegisterAliveEvent<T>(Action<T> onEvent) where T : struct
        {
            this.RegisterEvent(onEvent).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
    }

    public interface IAbstractController : IController
    {
        IArchitecture IBelongToArchitecture.GetArchitecture()
        {
            return Vomit.Interface;
        }
    }
}