/****************************************************************************
 * Copyright (c) 2016 - 2023 liangxiegame UNDER MIT License
 * 
 * https://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace QFramework
{
    public interface IState
    {
        bool Condition();
        UniTask Enter(IState context);
        void Update();
        void FixedUpdate();
        UniTask Exit();
    }
    
    public class CustomState : IState
    {
        private Func<bool> mOnCondition;
        private Func<IState, UniTask> mOnEnter;
        private Action mOnUpdate;
        private Action mOnFixedUpdate;
        private Action mOnGUI;
        private Func<UniTask> mOnExit;

        public CustomState OnCondition(Func<bool> onCondition)
        {
            mOnCondition = onCondition;
            return this;
        }
        
        public CustomState OnEnter(Func<IState, UniTask> onEnter)
        {
            mOnEnter = onEnter;
            return this;
        }

        
        public CustomState OnUpdate(Action onUpdate)
        {
            mOnUpdate = onUpdate;
            return this;
        }
        
        public CustomState OnFixedUpdate(Action onFixedUpdate)
        {
            mOnFixedUpdate = onFixedUpdate;
            return this;
        }
        
        public CustomState OnGUI(Action onGUI)
        {
            mOnGUI = onGUI;
            return this;
        }
        
        public CustomState OnExit(Func<UniTask> onExit)
        {
            mOnExit = onExit;
            return this;
        }


        public bool Condition()
        {
            var result = mOnCondition?.Invoke();
            return result == null || result.Value;
        }

        public UniTask Enter(IState context)
        {
            return mOnEnter?.Invoke(context) ?? UniTask.CompletedTask;
        }
        

        public void Update()
        {
            mOnUpdate?.Invoke();

        }

        public void FixedUpdate()
        {
            mOnFixedUpdate?.Invoke();
        }

        
        public void OnGUI()
        {
            mOnGUI?.Invoke();
        }

        public UniTask Exit()
        {
            return mOnExit?.Invoke() ?? UniTask.CompletedTask;
        }
    }

    public class FSM<T>
    {
        protected Dictionary<T, IState> mStates = new Dictionary<T, IState>();

        public void AddState(T id, IState state)
        {
            mStates.Add(id,state);
        }
        
        
        public CustomState State(T t)
        {
            if (mStates.ContainsKey(t))
            {
                return mStates[t] as CustomState;
            }

            var state = new CustomState();
            mStates.Add(t, state);
            return state;
        }

        private IState mCurrentState;
        private T mCurrentStateId;

        public IState CurrentState => mCurrentState;
        public T CurrentStateId => mCurrentStateId;
        public T PreviousStateId { get; private set; }

        public long FrameCountOfCurrentState = 1;
        public float SecondsOfCurrentState = 0.0f;
        
        public async UniTask ChangeState(T t, IState context)
        {
            if (t.Equals(CurrentStateId)) return;

            if (!mStates.TryGetValue(t, out var state)) return;

            if (mCurrentState == null) return;
            
            if (state.Condition())
            {
                await mCurrentState.Exit();
                PreviousStateId = mCurrentStateId;
                mCurrentState = state;
                mCurrentStateId = t;
                mOnStateChanged?.Invoke(PreviousStateId, CurrentStateId);
                FrameCountOfCurrentState = 1;
                SecondsOfCurrentState = 0.0f;
                await mCurrentState.Enter(context);
            }
            else
            {
                LogKit.E($"无效的状态转换! {state}=>{t}, 请检查对应的条件.");
            }
        }

        private Action<T, T> mOnStateChanged = (_, __) => { };
        
        public void OnStateChanged(Action<T, T> onStateChanged)
        {
            mOnStateChanged += onStateChanged;
        }

        public void StartState(T t)
        {
            if (mStates.TryGetValue(t, out var state))
            {
                PreviousStateId = t;
                mCurrentState = state;
                mCurrentStateId = t;
                FrameCountOfCurrentState = 0;
                SecondsOfCurrentState = 0.0f;
                state.Enter(null);
            }
        }

        public void FixedUpdate()
        {
            mCurrentState?.FixedUpdate();
        }

        public void Update()
        {
            mCurrentState?.Update();
            FrameCountOfCurrentState++;
            SecondsOfCurrentState += Time.deltaTime;
        }

        public void Clear()
        {
            mCurrentState = null;
            mCurrentStateId = default;
            mStates.Clear();
        }
    }
    
    public abstract class AbstractState<TStateId,TTarget> : IState
    {
        protected FSM<TStateId> mFSM;
        protected TTarget mTarget;

        public AbstractState(FSM<TStateId> fsm,TTarget target)
        {
            mFSM = fsm;
            mTarget = target;
        }


        bool IState.Condition()
        {
            return  OnCondition();;
        }

        UniTask IState.Enter(IState context)
        {
            return OnEnter(context);
        }

        void IState.Update()
        {
            OnUpdate();
        }

        void IState.FixedUpdate()
        {
            OnFixedUpdate();
        }
        
        UniTask IState.Exit()
        {
            return OnExit();
        }

        protected virtual bool OnCondition() => true;

        protected virtual UniTask OnEnter(IState context)
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnUpdate()
        {
            
        }

        protected virtual void OnFixedUpdate()
        {
            
        }

        protected virtual UniTask OnExit()
        {
            return UniTask.CompletedTask;
        }
    }
}