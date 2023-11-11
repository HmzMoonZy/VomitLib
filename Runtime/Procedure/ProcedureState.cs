using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    public abstract class ProcedureState<T> : ICanGetModel, ICanGetUtility, ICanGetSystem, ICanRegisterEvent, ICanSendEvent, ICanSendCommand, IState where T : struct
    {
        /// <summary>
        /// 状态机对象
        /// </summary>
        protected FSM<T> _procedure => Procedure<T>.Fsm;
        
        /// <summary>
        /// 事件注册列表
        /// </summary>
        private List<IUnRegister> _registers = new();

        /// <summary>
        /// 当前状态ID
        /// </summary>
        protected T CurrentStateID => _procedure.CurrentStateId;
        
        /// <summary>
        /// 上一个状态ID
        /// </summary>
        protected T PreviousStateID => _procedure.PreviousStateId;

        /// <summary>
        /// 进入状态回调
        /// </summary>
        protected abstract void OnEnter();

        /// <summary>
        /// 退出状态回调
        /// </summary>
        protected  abstract void OnExit();

        /// <summary>
        /// 状态机切换条件
        /// </summary>
        public abstract bool Condition();
        
        public void Enter()
        {
            OnEnter();
        }
        
        public void Exit()
        {
            foreach (var unRegister in _registers)
            {
                unRegister.UnRegister();
            }
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
        
        [Obsolete("暂时不支持 OnGUI")]
        public virtual void OnGUI()
        {
        }



        /// <summary>
        /// 切换状态
        /// </summary>
        protected void ChangeState(T id)
        {
            _procedure.ChangeState(id);
        }


        protected void RegisterProcedureEvent<T>(Action<T> action) where T : struct
        {
            _registers.Add(this.RegisterEvent<T>(action));
            
        }
        

        public IArchitecture GetArchitecture()
        {
            return Vomit.Interface;
        }
    }
}