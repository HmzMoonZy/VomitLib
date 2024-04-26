using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    public static class Procedure<T> where T : struct
    {
        public struct ProcedureChanged
        {
            public T Prev;

            public T Curr;
        }
        
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


            if (isStart) Start();
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

        public static IUnRegister RegisterEvent(Action<ProcedureChanged> onChanged)
        {
            return Vomit.Interface.RegisterEvent(onChanged);
        }

        /// <summary>
        /// 切换流程
        /// </summary>
        /// <param name="id"></param>
        public static void Change(T id)
        {
            Fsm.ChangeState(id);
            
            Vomit.Interface.SendEvent(new ProcedureChanged
            {
                Prev = GetPrevState(),
                Curr = GetCurrState(),
            });
            
            LogKit.I($"切换状态! {Fsm.PreviousStateId} => {id}");
        }
    }


}