using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    public abstract class ProcedureState<T> : ICanGetModel, ICanGetUtility, ICanGetSystem, ICanRegisterEvent, ICanSendEvent, ICanSendCommand, IState 
        where T : struct 
    {
        /// <summary>
        /// 事件注册列表
        /// </summary>
        private List<IUnRegister> _registers = new();

        /// <summary>
        /// 当前状态ID
        /// </summary>
        protected T CurrentStateID => Procedure<T>.GetCurrState();
        
        /// <summary>
        /// 上一个状态ID
        /// </summary>
        protected T PreviousStateID =>  Procedure<T>.GetPrevState();

        /// <summary>
        /// 状态机切换条件, 满足条件才能够执行ChangeState
        /// </summary>
        public virtual bool Condition()
        {
            return true;
        }
        
        /// <summary>
        /// 进入状态回调
        /// </summary>
        protected abstract UniTask OnEnter(IState context);

        /// <summary>
        /// 退出状态回调
        /// </summary>
        protected virtual UniTask OnExit()
        {
            return UniTask.CompletedTask;
        }
        
        public UniTask Enter(IState context)
        {
            return OnEnter(context);
        }

        public UniTask Exit()
        {
            foreach (var unRegister in _registers)
            {
                unRegister.UnRegister();
            }
            _registers.Clear();
            return OnExit();
        }

        /// <summary>
        /// 每帧调用的方法
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// 固定帧率调用
        /// </summary>
        public virtual void FixedUpdate()
        {
        }
        
        /// <summary>
        /// 切换状态
        /// </summary>
        protected UniTask ChangeState(T id)
        {
            return Procedure<T>.Change(id, this);
        }


        protected void RegisterProcedureEvent<TEvent>(Action<TEvent> action) where TEvent : struct
        {
            _registers.Add(this.RegisterEvent(action));
            
        }
        

        public IArchitecture GetArchitecture()
        {
            return Vomit.Interface;
        }
    }
}