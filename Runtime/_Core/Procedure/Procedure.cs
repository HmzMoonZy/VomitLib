using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    public static class Procedure<T> where T : struct
    {
        public static FSM<T> Fsm;
        
        public static void Init(IDictionary<T, ProcedureState<T>> stateDict, T startState)
        {
            if (Fsm != null)
            {
                return;
            }
            
            Fsm = new();
            
            foreach (var state in stateDict)
            {
                LogKit.I($"创建 Procedure {state.Key}");

                Fsm.AddState(state.Key, state.Value);
            }
            
            Init();
            
            Start(startState);
        }
        
        /// <summary>
        /// 自动扫描所有可能的流程, 初始化流程系统
        /// </summary>
        /// <param name="isStart">启动入口流程</param>
        public static void Init(bool isStart)
        {
            Fsm = new();

            T start = default;

            AutoRegisterProcedureState(ref start);

            Init();
            
            if (isStart)
            {
                Start(start);
            }
            return;

            static void AutoRegisterProcedureState(ref T s)
            {
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (!type.HasAttribute<ProcedureAttribute>()) continue;

                        var attr = type.GetAttribute<ProcedureAttribute>();
                        var id = (T) attr.ProcedureID;
                        var obj = (IState) Activator.CreateInstance(type);

                        LogKit.I($"创建 Procedure {id}");

                        Fsm.AddState(id, obj);

                        if (attr.IsEntry) s = id;
                    }
                }
            }
        }

        private static void Init()
        {
            UniTask.WaitWhile(() =>
            {
                Fsm.Update();
                return true;
            }, PlayerLoopTiming.Update).Forget();

            UniTask.WaitWhile(() =>
            {
                Fsm.FixedUpdate();
                return true;
            }, PlayerLoopTiming.FixedUpdate).Forget();
        }
        

        /// <summary>
        /// 启动入口流程
        /// </summary>
        public static void Start(T startState)
        {
            Fsm.StartState(startState);
        }

        /// <summary>
        /// 当前流程ID
        /// </summary>
        /// <returns></returns>
        public static T GetCurrState()
        {
            return Fsm.CurrentStateId;
        }
        
        /// <summary>
        /// 上一个流程ID
        /// </summary>
        /// <returns></returns>
        public static T GetPrevState()
        {
            return Fsm.PreviousStateId;
        }
        
        /// <summary>
        /// 切换流程
        /// </summary>
        public static async UniTask Change(T id, IState context)
        {
            await Fsm.ChangeState(id, context);
            
            Vomit.Interface.SendEvent(new EProcedure.Changed<T>()
            {
                Prev = GetPrevState(),
                Curr = GetCurrState(),
            });
            
            LogKit.I($"切换状态! {Fsm.PreviousStateId} => {id}");
        }
    }


}