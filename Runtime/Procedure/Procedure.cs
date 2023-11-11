using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    public static class Procedure<T> where T : struct
    {
        public static FSM<T> Fsm;

        private static T s_entryID;
        
        public static void Init()
        {
            Fsm = new();
            
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.HasAttribute<ProcedureAttribute>()) continue;

                    var attr = type.GetAttribute<ProcedureAttribute>();
                    var id = (T) attr.ProcedureID;
                    var obj = (IState)Activator.CreateInstance(type);

                    LogKit.I($"创建 Procedure {id}");
                    
                    Fsm.AddState(id, obj);
                    
                    if (attr.IsEntry) s_entryID = id;
                }
            }

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

        public static void Start()
        {
            Fsm.StartState(s_entryID);
        }

        public static void Change(T id)
        {
            LogKit.I($"切换状态! {Fsm.PreviousStateId} => {id}");
            
            Fsm.ChangeState(id);
        }
    }

}
