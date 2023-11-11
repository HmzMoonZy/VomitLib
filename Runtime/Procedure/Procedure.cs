using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    public static class Procedure<T> where T : struct
    {
        public static FSM<T> s_fsm;

        private static T s_entryID;
        
        public static void Init()
        {
            s_fsm = new();
            
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.HasAttribute<ProcedureAttribute>()) continue;

                    var attr = type.GetAttribute<ProcedureAttribute>();
                    var id = (T) attr.ProcedureID;
                    var obj = (IState)Activator.CreateInstance(type);

                    LogKit.I($"创建 Procedure {id}");
                    
                    s_fsm.AddState(id, obj);
                    
                    if (attr.IsEntry) s_entryID = id;
                }
            }

            UniTask.WaitWhile(() =>
            {
                s_fsm.Update();
                return true;
            }, PlayerLoopTiming.Update).Forget();
            
            UniTask.WaitWhile(() =>
            {
                s_fsm.FixedUpdate();
                return true;
            }, PlayerLoopTiming.FixedUpdate).Forget();
        }

        public static void Start()
        {
            s_fsm.StartState(s_entryID);
        }

        public static void Change(T id)
        {
            LogKit.I($"切换状态! {s_fsm.PreviousStateId} => {id}");
            
            s_fsm.ChangeState(id);
        }
    }

}
