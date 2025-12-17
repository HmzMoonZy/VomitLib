using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    /// <summary>
    /// Procedure状态基类
    /// </summary>
    public abstract class AbstractProcedure<T> where T : struct, Enum
    {
        #region Fields

        private readonly List<IUnRegister> _eventRegisters = new();

        private CancellationTokenSource _procedureCts = null;
        
        #endregion
        
        #region Abstract Methods
        
        public abstract T ProcedureKey { get; }
        
        /// <summary>
        /// 进入状态时调用
        /// </summary>
        protected abstract UniTask OnEnter(ProcedureArgsBase args);
        
        /// <summary>
        /// 退出状态时调用
        /// </summary>
        protected abstract void OnExit();
        
        #endregion

        #region Public Methods

        public async UniTask Enter(ProcedureArgsBase args)
        {
            try
            {
                await OnEnter(args);
                
                _procedureCts ??= new CancellationTokenSource();
                var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken, _procedureCts.Token);

                UniTask.WaitWhile(Tick, PlayerLoopTiming.Update, cancellationToken: tokenSource.Token).Forget();
                UniTask.WaitWhile(FixedTick, PlayerLoopTiming.FixedUpdate, cancellationToken: tokenSource.Token).Forget();
            }
            catch (Exception e)
            {
                Log.Error($"进入状态 {ProcedureKey} 时发生异常: {e.Message} \n{e.StackTrace}");
                throw;
            }
        }
        
        public void Exit()
        {
            try
            {
                // 先清理事件注册
                ClearEventRegisters();
                
                _procedureCts?.Cancel();
                _procedureCts?.Dispose();
                _procedureCts = null;
                
                // 再执行退出逻辑
                OnExit();
            }
            catch (Exception e)
            {
                Log.Error($"退出状态 {ProcedureKey} 时发生异常: {e.Message}");
                ClearEventRegisters();
                throw;
            }
        }

        #endregion
        
        #region Virtual Methods
        
        /// <summary>
        /// 每帧更新，子类可以重写
        /// </summary>
        protected virtual void OnTick(float deltaTime, float unscaledDeltaTime)
        {
            // 默认不做任何事
        }
        
        /// <summary>
        /// 固定更新，子类可以重写
        /// </summary>
        protected virtual void OnFixedTick()
        {
            // 默认不做任何事
        }
        
        #endregion
        
        #region Protected Utility Methods
        
        /// <summary>
        /// 切换状态
        /// </summary>
        protected UniTask ChangeState(T targetState, ProcedureArgsBase args)
        {
            return ProcedureMgr<T>.Instance.ChangeState(targetState, args);
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
        
        private void ClearEventRegisters()
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
        
        private bool Tick()
        {
            var deltaTime = Time.deltaTime;
            var unscaledDeltaTime = Time.unscaledDeltaTime;
            
            try
            {
                OnTick(deltaTime, unscaledDeltaTime);
            }
            catch (Exception e)
            {
                Log.Error($"状态 {ProcedureKey} Update异常: {e.Message} \n{e.StackTrace}");
            }
            
            return true;
        }
        
        private bool FixedTick()
        {
            try
            {
                OnFixedTick();
            }
            catch (Exception e)
            {
                Log.Error($"状态 {ProcedureKey} FixedUpdate异常: {e.Message}");
            }
            return true;
        }
        
        #endregion
    }
}