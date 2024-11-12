using System;
using Cysharp.Threading.Tasks;
using QFramework;
using Twenty2.VomitLib.Config;
using UnityEngine;

namespace Twenty2.VomitLib
{
    public static class Vomit
    {
        public static VomitRuntimeConfig RuntimeConfig { get; private set; }
        
        #if UNITY_EDITOR
        public static VomitRuntimeConfig GetConfigInEditor()
        {
            var guid = UnityEditor.AssetDatabase.FindAssets("t:VomitRuntimeConfig")[0];
            return UnityEditor.AssetDatabase.LoadAssetAtPath<VomitRuntimeConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
        }
        #endif

        public static IArchitecture Interface { get; private set; }
        
        public static void Init(IArchitecture architecture, Func<VomitRuntimeConfig> onLoadConfig)
        {
            Interface = architecture;

            if (onLoadConfig == null)
            {
                LogKit.E("无法正确获得 VomitRuntimeConfig");
                return;
            }
            RuntimeConfig = onLoadConfig.Invoke();
        }
    }

    public class MonoController : MonoBehaviour, IAbstractController
    {
        protected void RegisterEvent<T>(Action<T> onEvent) where T : struct
        {
            this.As<ICanRegisterEvent>().RegisterEvent(onEvent).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
        
        protected IUnRegister RegisterEventWithoutUnRegister<T>(Action<T> onEvent) where T : struct
        {
            return this.As<ICanRegisterEvent>().RegisterEvent(onEvent);
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