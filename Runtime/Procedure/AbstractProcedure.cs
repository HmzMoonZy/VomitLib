using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    /// <summary>
    /// 带参数 Procedure状态基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TArgs"></typeparam>
    public abstract class AbstractGameProcedure<T, TArgs> : AbstractProcedure<T> where T : struct, Enum where TArgs : ProcedureArgsBase
    {
        protected TArgs Args;
        
        protected abstract UniTask OnEnter(TArgs args);
    
        protected override UniTask OnEnter(ProcedureArgsBase args)
        {
            Args = args as TArgs;
            return OnEnter(Args);
        }
    }
    
    /// <summary>
    /// Procedure状态基类
    /// </summary>
    public abstract class AbstractProcedure<T> where T : struct, Enum
    {
        #region Fields

        protected CancellationTokenSource ProcedureCts = null;

        private readonly List<IUnRegister> _eventRegisters = new();

        /// <summary>
        /// 进入就绪信号源。OnEnter 全部异步初始化完成后 resolve。
        /// 默认行为(见 DelayReadyUntilExplicitSet):OnEnter 返回即视为就绪,向后兼容;
        /// 需要异步加载(场景/角色等)的子类可重写 DelayReadyUntilExplicitSet 为 true,
        /// 并在加载完成时显式调用 SetReady()。
        /// 调用方(如遮罩切换)可 await WaitReady() 决定揭幕时机。
        /// </summary>
        private UniTaskCompletionSource _readyTcs;

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
        protected abstract void OnExit(T toState);
        
        #endregion

        #region Public Methods

        public async UniTask Enter(ProcedureArgsBase args)
        {
            // 每次进入重置就绪信号(支持状态复用)
            _readyTcs = new UniTaskCompletionSource();

            try
            {
                ProcedureCts ??= new CancellationTokenSource();
                await OnEnter(args);
                var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken, ProcedureCts.Token);

                UniTask.WaitWhile(Tick, PlayerLoopTiming.Update, cancellationToken: tokenSource.Token).Forget();
                UniTask.WaitWhile(FixedTick, PlayerLoopTiming.FixedUpdate, cancellationToken: tokenSource.Token).Forget();
            }
            catch (Exception e)
            {
                // OnEnter 抛异常:释放等待者,避免调用方死等,并如实向上抛
                _readyTcs.TrySetException(e);
                Log.Error($"进入状态 {ProcedureKey} 时发生异常: {e.Message} \n{e.StackTrace}");
                throw;
            }

            // 同步进入或无需延迟就绪:OnEnter 返回即视为就绪(向后兼容)
            if (!DelayReadyUntilExplicitSet)
            {
                _readyTcs.TrySetResult();
            }
        }
        
        public void Exit(T toState)
        {
            try
            {
                // 先清理事件注册
                ClearEventRegisters();
                
                ProcedureCts?.Cancel();
                ProcedureCts?.Dispose();
                ProcedureCts = null;

                // 退出时若仍未就绪(例如延迟就绪的 OnEnter 中途切换出去),释放等待者避免死等
                _readyTcs?.TrySetCanceled();
                
                // 再执行退出逻辑
                OnExit(toState);
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
        /// 为 true 时,OnEnter 返回后不会自动置为就绪,
        /// 子类必须在所有异步初始化(场景/角色/UI 等)完成后显式调用 SetReady()。
        /// 默认 false:OnEnter 返回即就绪,向后兼容。
        /// </summary>
        protected virtual bool DelayReadyUntilExplicitSet => false;

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
        /// 等待本状态进入就绪。
        /// 与 ProcedureCts 取消信号对称:取消表示"何时停",就绪表示"何时准备好可揭幕/可交互"。
        /// 由调用方(如遮罩切换 ChangeStateWithMask)在切换完成后 await,以决定揭幕时机。
        /// </summary>
        public UniTask WaitReady()
        {
            _readyTcs ??= new UniTaskCompletionSource();
            return _readyTcs.Task;
        }

        /// <summary>
        /// 声明本状态已就绪,可揭幕/可交互。
        /// 仅对 DelayReadyUntilExplicitSet 为 true 的状态有效;
        /// 默认状态下 OnEnter 返回时已由基类自动调用。
        /// </summary>
        protected void SetReady()
        {
            _readyTcs?.TrySetResult();
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
            
            // 累加状态机监控数据
            ProcedureMgr<T>.Instance.AccumulateMonitor(deltaTime);
            
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