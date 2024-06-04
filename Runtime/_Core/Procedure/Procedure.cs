using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    public static class Procedure<T> where T : struct
    {
        public static FSM<T> Fsm;

        private static T s_entryID;
        
        /// <summary>
        /// 初始化流程系统
        /// </summary>
        /// <param name="isStart">启动入口流程</param>
        public static void Init(bool isStart = true)
        {
            Fsm = new();

            AutoRegisterProcedureState();

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

            if (isStart)
            {
                Start();
            }
            return;

            static void AutoRegisterProcedureState()
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

                        if (attr.IsEntry) s_entryID = id;
                    }
                }
            }
        }

        /// <summary>
        /// 启动入口流程
        /// </summary>
        public static void Start()
        {
            Fsm.StartState(s_entryID);
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