using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Twenty2.VomitLib.Tools;

using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    public abstract class ProcedureState<T> : ICanGetModel, ICanGetUtility, ICanGetSystem, ICanRegisterEvent, ICanSendEvent, ICanSendCommand, IState, ICanSendQuery where T : struct 
    {
        /// <summary>
        /// 事件注册列表
        /// </summary>
        private List<IUnRegister> _registers = new();
        
        
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
        protected abstract void OnEnter(IState context);

        /// <summary>
        /// 退出状态回调
        /// </summary>
        protected abstract void OnExit();
        
        public void Enter(IState context)
        {
            OnEnter(context);
        }

        public void Exit()
        {
            foreach (var unRegister in _registers)
            {
                unRegister.UnRegister();
            }
            _registers.Clear();
            OnExit();
        }

        /// <summary>
        /// 每帧调用的方法
        /// </summary>
        public virtual void Update(float elapseSeconds, float realElapseSeconds)
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
        protected void ChangeState(T id)
        {
            Procedure<T>.Instance.Change(id, this);
        }
        
        /// <summary>
        /// 监听一个事件, 当退出状态时, 自动注销
        /// </summary>
        protected void RegisterEvent<TEvent>(Action<TEvent> action) where TEvent : struct
        {
            _registers.Add(((ICanRegisterEvent) this).RegisterEvent(action));
        }
        
        public IArchitecture GetArchitecture()
        {
            return Vomit.Interface;
        }
    }
}