using System;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    public abstract class ProcedureState<T> : ICanGetModel, ICanGetUtility, ICanGetSystem, ICanRegisterEvent, ICanSendEvent, IState where T : struct
    {
        protected FSM<T> _procedure => Procedure<T>.s_fsm;

        public abstract void Enter();

        public abstract void Exit();

        public abstract bool Condition();

        public virtual void Update()
        {
        }

        public virtual void FixedUpdate()
        {
        }

        /// <summary>
        /// 永远不要实现这个方法,因为没有用.
        /// </summary>
        public virtual void OnGUI()
        {
        }

        // TODO 
        [Obsolete]
        protected void RegisterProcedureEvent()
        {
            // TODO 
        }
        

        public IArchitecture GetArchitecture()
        {
            return Vomit.Interface;
        }
    }
}