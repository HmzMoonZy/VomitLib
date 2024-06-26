﻿using System;
using Cysharp.Threading.Tasks;
using QFramework;
using Twenty2.VomitLib.Config;
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
            
            // TODO 优化遍历范围
            // foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            // {
            //     foreach (var type in assembly.GetTypes())
            //     {
            //         // 遍历异步事件
            //         if (type.IsValueType && type.HasAttribute<AsyncEventAttribute>())
            //         {
            //             AsyncEventExtension.Register(type.Name);
            //         }
            //     }
            // }
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