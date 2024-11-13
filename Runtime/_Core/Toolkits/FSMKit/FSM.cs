using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;
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
        public async UniTask Launch(T t)
        {
            if (CurrentState != null)
            {
                LogKit.E($"The state machine has already been started. It can not be started again!");
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
            
            await CurrentState.Enter(null);
            
            UniTask.WaitWhile(Update, PlayerLoopTiming.Update).Forget();

            UniTask.WaitWhile(FixedUpdate, PlayerLoopTiming.FixedUpdate).Forget();
            
            IsRunning = true;
        }
        
        /// <summary>
        /// 添加一个状态机
        /// </summary>
        public void AddState(T id, IState state)
        {
            _states.Add(id, state);
        }
        
        public async UniTask ChangeState(T t, IState context)
        {
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
                return;
            }

            if (!state.Condition())
            {
                LogKit.E($"无效的状态转换! {state}=>{t}, 请检查对应的条件.");
                return;
            }
            
            LogKit.I($"Start : {state}=>{t}");

            IsRunning = false;
            await CurrentState.Exit();
            PreviousStateId = CurrentStateId;
            CurrentState = state;
            CurrentStateId = t;
            FrameCountOfCurrentState = 1;
            SecondsOfCurrentState = 0.0f;
            await CurrentState.Enter(context);
            IsRunning = true;
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