using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Twenty2.VomitLib.Tools
{
    public class FSM<T>
    {
        /// <summary>
        /// 当前的状态机
        /// </summary>
        public IState CurrentState { get; private set; }
        
        /// <summary>
        /// 当前状态机索引
        /// </summary>
        public T CurrentStateId { get; private set; }
        
        /// <summary>
        /// 上一个状态机的索引
        /// </summary>    
        public T PreviousStateId { get; private set; }

        /// <summary>
        /// 当前状态持续帧数
        /// </summary>
        public long FrameCountOfCurrentState { get; private set; }

        /// <summary>
        /// 当前状态持续秒数
        /// </summary>
        public float SecondsOfCurrentState { get; private set; }
        
        /// <summary>
        /// 所有的状态机
        /// </summary>
        protected Dictionary<T, IState> _states = new Dictionary<T, IState>();

        protected bool IsRunning { get; private set; } = false;
        
        
        /// <summary>
        /// 启动状态机
        /// </summary>
        public void Launch(T t)
        {
            if (CurrentState != null)
            {
                LogKit.E($"The state machine has already been started. It can not be started again!");
                return;
            }

            if (_states.Count <= 0)
            {
                LogKit.I("The state machine has no states. It can not be started!");
                return;
            }
            
            if (!_states.TryGetValue(t, out var state))
            {
                throw new NotImplementedException($"Not Found State: {t}");
            }
            
            PreviousStateId = t;
            CurrentStateId = t;
            CurrentState = state;
            
            FrameCountOfCurrentState = 0;
            SecondsOfCurrentState = 0.0f;
            
            CurrentState.Enter(null);
            
            IsRunning = true;
            
            UniTask.WaitWhile(Update, PlayerLoopTiming.Update, cancellationToken: Application.exitCancellationToken);

            UniTask.WaitWhile(FixedUpdate, PlayerLoopTiming.FixedUpdate, cancellationToken: Application.exitCancellationToken);
        }
        
        /// <summary>
        /// 添加一个状态机
        /// </summary>
        public void AddState(T id, IState state)
        {
            _states.Add(id, state);
        }
        
        public void ChangeState(T t, IState context)
        {
            if (!IsRunning)
            {
                LogKit.E("状态机不在运行, 请检查初始化状态或切换时机.");
                return;
            }
            
            if (CurrentState == null)
            {
                return;
            }
            
            if (t.Equals(CurrentStateId))
            {
                return;
            }

            if (!_states.TryGetValue(t, out var state))
            {
                LogKit.E($"无效的状态转换! {t}");
                return;
            }

            if (!state.Condition())
            {
                LogKit.E($"无效的状态转换! {state}=>{t}, 请检查对应的条件.");
                return;
            }
            
            LogKit.I($"state changing : {CurrentState} => {t}");
            
            IsRunning = false;
            CurrentState.Exit();
            PreviousStateId = CurrentStateId;
            CurrentState = state;
            CurrentStateId = t;
            FrameCountOfCurrentState = 1;
            SecondsOfCurrentState = 0.0f;
            CurrentState.Enter(context);
            IsRunning = true;
            
            LogKit.I($"state changed : {PreviousStateId} => {CurrentStateId}");
        }
        
        private bool FixedUpdate()
        {
            if (IsRunning)
            {
                CurrentState?.FixedUpdate();    
            }
            
            return true;
        }

        private bool Update()
        {
            if (IsRunning)
            {
                CurrentState?.Update();
            }
            FrameCountOfCurrentState++;
            SecondsOfCurrentState += Time.deltaTime;
            return true;
        }
    }
}