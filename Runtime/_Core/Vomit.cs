using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAPI;
using QFramework;
using UnityEngine;

namespace Twenty2.VomitLib
{
    #region Vomit Class

    public static class Vomit
    {
        public static IArchitecture Interface { get; private set; }

        public static bool IsInit => Interface != null;

        /// <summary>
        /// 关联框架的接口
        /// </summary>
        /// <param name="architecture">关联框架的接口</param>
        public static void Init(IArchitecture architecture)
        {
            Interface = architecture;
        }

        /// <summary>
        /// 依照合理的顺序关联框架的系统和数据
        /// </summary>
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
    }

    #endregion
    
    #region Qframework Extension

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
    
    #region  Poolable Command   // TODO
    
    /// <summary>
    /// 可池化的接口 - 所有需要池化的 Command 都应实现此接口
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 从池中取出时调用
        /// </summary>
        void OnAcquire();
        
        /// <summary>
        /// 回收到池中时调用
        /// </summary>
        void OnRecycle();
    }
    
    public interface IAutoRecycleCommand : ICommand, IPoolable
    {
    }
    
    public interface IAutoRecycleCommand<TResult> : ICommand<TResult>, IPoolable
    {
    }
    
    public abstract class AbstractSmartCommand : AbstractCommand, IAutoRecycleCommand
    {
        public abstract void Execute();

        public abstract void OnAcquire();

        public abstract void OnRecycle();
    }
    
    public abstract class AbstractSmartCommand<TResult> : AbstractCommand<TResult>, IAutoRecycleCommand<TResult>
    {
        public abstract TResult Execute();

        public abstract void OnAcquire();

        public abstract void OnRecycle();
    }
    
    #endregion
    
    #endregion
}