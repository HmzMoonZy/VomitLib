﻿using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    public abstract class ProcedureState<T> :
        ICanGetModel, ICanGetUtility, ICanGetSystem, ICanRegisterEvent, ICanSendEvent, ICanSendCommand, IState 
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
        /// 进入状态回调
        /// </summary>
        protected abstract void OnEnter(IState context);

        /// <summary>
        /// 退出状态回调
        /// </summary>
        protected  abstract void OnExit();

        /// <summary>
        /// 状态机切换条件
        /// </summary>
        public abstract bool Condition();
        
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
        protected void ChangeState(T id)
        {
            Procedure<T>.Change(id, this);
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