using System;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    /// <summary>
    /// Procedure状态基类 - 简化版本，职责更清晰
    /// 现在只负责状态管理，不再强制继承所有QFramework接口
    /// 需要时通过Vomit.Interface手动获取
    /// </summary>
    public abstract class ProcedureState<T> : IProcedureState where T : struct, Enum
    {
        #region Fields
        
        private readonly List<IUnRegister> _eventRegisters = new();
        private readonly string _stateName;
        
        #endregion
        
        #region Constructor
        
        protected ProcedureState()
        {
            _stateName = GetType().Name;
        }
        
        #endregion
        
        #region IProcedureState Implementation
        
        public string StateName => _stateName;
        
        public void Enter()
        {
            try
            {
                OnEnter();
            }
            catch (Exception e)
            {
                Log.Error($"进入状态 {_stateName} 时发生异常: {e.Message}");
                throw;
            }
        }
        
        public void Exit()
        {
            try
            {
                // 先清理事件注册
                CleanupEventRegisters();
                
                // 再执行退出逻辑
                OnExit();
            }
            catch (Exception e)
            {
                Log.Error($"退出状态 {_stateName} 时发生异常: {e.Message}");
                // 即使OnExit失败，也要确保事件被清理
                CleanupEventRegisters();
                throw;
            }
        }
        
        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            try
            {
                OnUpdate(deltaTime, unscaledDeltaTime);
            }
            catch (Exception e)
            {
                Log.Error($"状态 {_stateName} Update异常: {e.Message}");
            }
        }
        
        public void FixedUpdate()
        {
            try
            {
                OnFixedUpdate();
            }
            catch (Exception e)
            {
                Log.Error($"状态 {_stateName} FixedUpdate异常: {e.Message}");
            }
        }
        
        public virtual bool CanChangeFrom(IProcedureState fromState)
        {
            // 默认允许从任何状态切换，子类可以重写此方法添加条件
            return true;
        }
        
        #endregion
        
        #region Abstract Methods - 子类必须实现
        
        /// <summary>
        /// 进入状态时调用
        /// </summary>
        protected abstract void OnEnter();
        
        /// <summary>
        /// 退出状态时调用
        /// </summary>
        protected abstract void OnExit();
        
        #endregion
        
        #region Virtual Methods - 子类可以重写
        
        /// <summary>
        /// 每帧更新，子类可以重写
        /// </summary>
        protected virtual void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            // 默认不做任何事
        }
        
        /// <summary>
        /// 固定更新，子类可以重写
        /// </summary>
        protected virtual void OnFixedUpdate()
        {
            // 默认不做任何事
        }
        
        #endregion
        
        #region Protected Utility Methods
        
        /// <summary>
        /// 切换状态
        /// </summary>
        protected void ChangeState(T targetState)
        {
            ProcedureStateMachine<T>.Instance.ChangeState(targetState);
        }
        
        /// <summary>
        /// 强制切换状态（跳过条件检查）
        /// </summary>
        protected void ForceChangeState(T targetState)
        {
            ProcedureStateMachine<T>.Instance.ForceChangeState(targetState);
        }
        
        /// <summary>
        /// 注册事件监听，状态退出时自动注销
        /// </summary>
        protected void RegisterEvent<TEvent>(Action<TEvent> onEvent)
        {
            var unregister = Vomit.Interface.RegisterEvent(onEvent);
            _eventRegisters.Add(unregister);
        }
        
        /// <summary>
        /// 发送事件
        /// </summary>
        protected void SendEvent<TEvent>() where TEvent : new()
        {
            Vomit.Interface.SendEvent<TEvent>();
        }
        
        /// <summary>
        /// 发送事件
        /// </summary>
        protected void SendEvent<TEvent>(TEvent eventData)
        {
            Vomit.Interface.SendEvent(eventData);
        }
        
        /// <summary>
        /// 发送命令
        /// </summary>
        protected void SendCommand<TCommand>(TCommand command) where TCommand : ICommand
        {
            Vomit.Interface.SendCommand(command);
        }
        
        /// <summary>
        /// 发送查询
        /// </summary>
        protected TResult SendQuery<TResult>(IQuery<TResult> query)
        {
            return Vomit.Interface.SendQuery(query);
        }
        
        /// <summary>
        /// 获取Model
        /// </summary>
        protected TModel GetModel<TModel>() where TModel : class, IModel
        {
            return Vomit.Interface.GetModel<TModel>();
        }
        
        /// <summary>
        /// 获取System
        /// </summary>
        protected TSystem GetSystem<TSystem>() where TSystem : class, ISystem
        {
            return Vomit.Interface.GetSystem<TSystem>();
        }
        
        /// <summary>
        /// 获取Utility
        /// </summary>
        protected TUtility GetUtility<TUtility>() where TUtility : class, IUtility
        {
            return Vomit.Interface.GetUtility<TUtility>();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 清理事件注册
        /// </summary>
        private void CleanupEventRegisters()
        {
            foreach (var register in _eventRegisters)
            {
                try
                {
                    register?.UnRegister();
                }
                catch (Exception e)
                {
                    Log.Error($"注销事件时发生异常: {e.Message}");
                }
            }
            _eventRegisters.Clear();
        }
        
        #endregion
        
        #region Debug
        
        public override string ToString()
        {
            return _stateName;
        }
        
        #endregion
    }
}