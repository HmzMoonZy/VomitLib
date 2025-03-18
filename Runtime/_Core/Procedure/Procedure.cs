using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using FluentAPI;
using QFramework;
using Twenty2.VomitLib.Tools;

namespace Twenty2.VomitLib.Procedure
{
    public class Procedure<T> where T : struct
    {
        private readonly Fsm<T> _fsm;
        
        private static Procedure<T> _instance;

        public static Procedure<T> Instance => _instance ??= new Procedure<T>();

        /// <summary>
        /// 当前流程
        /// </summary>
        public T CurrentState => _fsm.CurrentStateId;

        /// <summary>
        /// 上一个流程
        /// </summary>
        public T PrevState => _fsm.PreviousStateId;

        /// <summary>
        /// 当前流程帧数
        /// </summary>
        public long FrameCountOfCurrentState => _fsm.FrameCountOfCurrentState;
        
        /// <summary>
        /// 当前流程时间
        /// </summary>
        public float CurrentStateTime => _fsm.SecondsOfCurrentState;
        
        private Procedure()
        {
            _fsm = new();
        }

        /// <summary>
        /// 自动扫描所有可能的流程, 初始化流程系统
        /// </summary>
        public void Launch()
        {
            T start = default;
            
            // 扫描所有流程
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.HasAttribute<ProcedureAttribute>())
                    {
                        continue;
                    }

                    var attr = type.GetAttribute<ProcedureAttribute>();
                    var id = (T) attr.ProcedureID;
                    var obj = (IState) Activator.CreateInstance(type);
                    
                    Log.Debug($"创建 Procedure {id}");

                    _fsm.AddState(id, obj);

                    if (attr.IsEntry)
                    {
                        start = id;
                    }
                }
            }

            // 启动
            UniTask.Create(async () =>
            {
                await UniTask.NextFrame();
                Log.Debug($"启动 Procedure {start}");
                _fsm.Launch(start);
            });
        }
        
        /// <summary>
        /// 切换流程
        /// </summary>
        public void Change(T id, IState context)
        {
            _fsm.ChangeState(id, context);
            
            Vomit.Interface.SendEvent<EProcedure.Changed>();
        }
    }


}