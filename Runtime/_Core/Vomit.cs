using System;
using FluentAPI;
using QFramework;
using Twenty2.VomitLib.Config;
using UnityEngine;

namespace Twenty2.VomitLib
{
    public static class Vomit
    {
        public static VomitConfig Config { get; private set; }
        
        #if UNITY_EDITOR
        public static VomitConfig GetConfigInEditor()
        {
            var guid = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(VomitConfig)}")[0];
            return UnityEditor.AssetDatabase.LoadAssetAtPath<VomitConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
        }
        #endif

        public static IArchitecture Interface { get; private set; }
        
        /// <summary>
        /// 初始化 Vomit 框架
        /// </summary>
        /// <param name="architecture">IArchitecture 实例</param>
        /// <param name="onLoadConfig">框架配置有更新需求, 则可以动态加载配置文件, 否则会从默认路径(Resource/VomitLibConfig)读取</param>
        public static void Init(IArchitecture architecture, Func<VomitConfig> onLoadConfig = null)
        {
            Interface = architecture;
            
            Config = onLoadConfig == null ? Resources.Load<VomitConfig>("VomitLibConfig") : onLoadConfig.Invoke();
            
            if (Config == null)
            {
                LogKit.E("无法正确获得配置信息");
            }
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