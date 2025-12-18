using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAPI;
using QFramework;
using UnityEngine;

namespace Twenty2.VomitLib
{
    public static partial class Vomit
    {
        public static IArchitecture Interface { get; private set; }

        public static bool IsInit => Interface != null;

        /// <summary>
        /// 关联框架的接口
        /// </summary>
        /// <param name="architecture">关联框架的接口</param>
        /// <param name="logger">日志管理器</param>
        public static void Init(IArchitecture architecture, ILogHelper logger = null)
        {
            Log.SetLogHelper(logger ?? new DefaultLogHelper());

            Interface = architecture;
        }

        /// <summary>
        /// 依照合理的顺序关联框架的系统和数据, 性能略低, 适合在开发阶段调用
        /// </summary>
        [Obsolete("使用生成的AutoLoad")]
        public static void AutoLoad(Assembly assembly)
        {
            if (IsInit == false)
            {
                throw new Exception("请先调用 Vomit.Init(IArchitecture architecture)");
            }

            HashSet<Type> modelTypes = new();
            HashSet<Type> systemTypes = new();
            
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(AbstractModel)))
                {
                    modelTypes.Add(type);
                }
                
                if (type.IsSubclassOf(typeof(AbstractSystem)))
                {
                    systemTypes.Add(type);
                }
            }


            var registerModel = Interface.GetType().GetMethod("RegisterModel", BindingFlags.Public | BindingFlags.Instance);
            foreach (var type in modelTypes)
            {
                // 反射泛型方法 Interface.RegisterModel<T>()
                Log.Debug($"Auto Load {type.Name}");
                registerModel!.MakeGenericMethod(type).Invoke(Interface, new[] {Activator.CreateInstance(type)});
            }
            
            var registerSystem = Interface.GetType().GetMethod("RegisterSystem", BindingFlags.Public | BindingFlags.Instance);
            foreach (var type in systemTypes)
            {
                // 反射泛型方法 Interface.RegisterSystem<T>()
                Log.Debug($"Auto Load {type.Name}");
                registerSystem!.MakeGenericMethod(type).Invoke(Interface, new[] {Activator.CreateInstance(type)});
            }
        }

        /// <summary>
        /// 启动框架监视器, 仅在编辑器下有效
        /// </summary>
        public static void EnableMonitor()
        {
#if UNITY_EDITOR
            new GameObject("VomitMonitor", typeof(Twenty2.VomitLib.Monitor.ViewMonitor));
            new GameObject("ModelMonitor", typeof(Twenty2.VomitLib.Monitor.ModelMonitor));
            new GameObject("SystemMonitor", typeof(Twenty2.VomitLib.Monitor.SystemMonitor));
            new GameObject("EventMonitor", typeof(Twenty2.VomitLib.Monitor.EventMonitor));
            new GameObject("CommandMonitor", typeof(Twenty2.VomitLib.Monitor.CommandMonitor));
            new GameObject("LubanMonitor", typeof(Twenty2.VomitLib.Monitor.LubanSupportMonitor));
#endif
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

    public interface IGameInformation : ICanGetModel
    {
        IArchitecture IBelongToArchitecture.GetArchitecture()
        {
            return Vomit.Interface;
        }
    }
}