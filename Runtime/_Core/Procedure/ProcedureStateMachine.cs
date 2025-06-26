using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    /// <summary>
    /// Procedure专用状态机 - 单例模式，一个游戏只有一个流程状态机
    /// </summary>
    public class ProcedureStateMachine<T> where T : struct
    {
        #region Singleton
        private ProcedureStateMachine()
        {
            // 单例，私有构造函数
        }

        private static ProcedureStateMachine<T> _instance;
        public static ProcedureStateMachine<T> Instance => _instance ??= new ProcedureStateMachine<T>();
        
        #endregion
        
        #region Fields
        
        private readonly Dictionary<T, IProcedureState> _states = new();
        private readonly Dictionary<T, IProcedureState> _registeredStates = new();
        private IProcedureState _currentState;
        private IProcedureState _previousState;
        private T _currentStateId;
        private T _previousStateId;
        private bool _isRunning;
        private bool _isChanging;
        private bool _isInitialized;
        
        // 状态统计信息
        private long _frameCount;
        private float _stateTime;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 当前状态
        /// </summary>
        public IProcedureState CurrentState => _currentState;
        
        /// <summary>
        /// 当前状态ID
        /// </summary>
        public T CurrentStateId => _currentStateId;
        
        /// <summary>
        /// 上一个状态ID
        /// </summary>
        public T PreviousStateId => _previousStateId;
        
        /// <summary>
        /// 当前状态运行帧数
        /// </summary>
        public long FrameCount => _frameCount;
        
        /// <summary>
        /// 当前状态运行时间
        /// </summary>
        public float StateTime => _stateTime;
        
        /// <summary>
        /// 状态机是否在运行
        /// </summary>
        public bool IsRunning => _isRunning;
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 注册状态 - 手动注册，不再使用反射
        /// </summary>
        public void RegisterState<TState>(T stateId, TState state) where TState : class, IProcedureState
        {
            if (_isInitialized)
            {
                Log.Error("状态机已初始化，无法注册新状态");
                return;
            }
            
            if (_registeredStates.ContainsKey(stateId))
            {
                Log.Warning($"状态 {stateId} 已存在，将被覆盖");
            }
            
            _registeredStates[stateId] = state;
            Log.Info($"注册流程状态: {stateId} -> {typeof(TState).Name}");
        }
        
        /// <summary>
        /// 注册状态 - 泛型版本，自动创建实例
        /// </summary>
        public void RegisterState<TState>(T stateId) where TState : class, IProcedureState, new()
        {
            RegisterState(stateId, new TState());
        }
        
        /// <summary>
        /// 初始化并启动状态机
        /// </summary>
        public void Initialize(T initialStateId)
        {
            if (_isInitialized)
            {
                Log.Warning("状态机已经初始化过了");
                return;
            }
            
            if (_registeredStates.Count == 0)
            {
                Log.Error("没有注册任何状态，无法初始化状态机");
                return;
            }
            
            if (!_registeredStates.ContainsKey(initialStateId))
            {
                Log.Error($"初始状态 {initialStateId} 未注册");
                return;
            }
            
            // 将所有注册的状态添加到状态机
            foreach (var kvp in _registeredStates)
            {
                _states[kvp.Key] = kvp.Value;
            }
            
            _isInitialized = true;
            
            // 启动状态机
            Start(initialStateId);
            
            Log.Info($"状态机初始化完成，初始状态: {initialStateId}");
        }
        
        /// <summary>
        /// 异步初始化 - 延迟一帧后启动
        /// </summary>
        public async UniTask InitializeAsync(T initialStateId)
        {
            await UniTask.NextFrame();
            Initialize(initialStateId);
        }
        
        /// <summary>
        /// 内部启动方法
        /// </summary>
        private void Start(T initialStateId)
        {
            if (_isRunning)
            {
                Log.Error("状态机已经在运行中，不能重复启动");
                return;
            }
            
            if (!_states.TryGetValue(initialStateId, out var initialState))
            {
                Log.Error($"找不到初始状态: {initialStateId}");
                return;
            }
            
            _currentStateId = initialStateId;
            _currentState = initialState;
            _previousStateId = initialStateId;
            _previousState = null;
            
            ResetStateTime();
            
            try
            {
                _currentState.Enter();
                _isRunning = true;
                
                Log.Info($"状态机启动成功，初始状态: {initialStateId}");
                
                // 启动Update循环
                StartUpdateLoop().Forget();
            }
            catch (Exception e)
            {
                Log.Error($"状态机启动失败: {e.Message}");
                _isRunning = false;
            }
        }
        
        /// <summary>
        /// 停止状态机
        /// </summary>
        public void Shutdown()
        {
            if (!_isInitialized) return;
            
            try
            {
                _currentState?.Exit();
            }
            catch (Exception e)
            {
                Log.Error($"退出当前状态时发生错误: {e.Message}");
            }
            finally
            {
                _isRunning = false;
                _isInitialized = false;
                _currentState = null;
                Log.Info("状态机已停止");
            }
        }
 
        /// <summary>
        /// 切换状态
        /// </summary>
        public bool ChangeState(T newStateId)
        {
            if (!_isInitialized)
            {
                Log.Error("状态机未初始化，无法切换状态");
                return false;
            }
            
            if (_isChanging)
            {
                Log.Warning("状态正在切换中，忽略此次切换请求");
                return false;
            }
            
            if (newStateId.Equals(_currentStateId))
            {
                Log.Warning($"尝试切换到相同状态: {newStateId}");
                return true;
            }
            
            if (!_states.TryGetValue(newStateId, out var newState))
            {
                Log.Error($"找不到目标状态: {newStateId}");
                return false;
            }
            
            // 检查切换条件
            if (!newState.CanChangeFrom(_currentState))
            {
                Log.Warning($"状态切换条件不满足: {_currentStateId} -> {newStateId}");
                return false;
            }
            
            return InternalChangeState(newStateId, newState);
        }
        
        /// <summary>
        /// 强制切换状态（跳过条件检查）
        /// </summary>
        public bool ForceChangeState(T newStateId)
        {
            if (!_isInitialized)
            {
                Log.Error("状态机未初始化，无法切换状态");
                return false;
            }
            
            if (!_states.TryGetValue(newStateId, out var newState))
            {
                Log.Error($"找不到目标状态: {newStateId}");
                return false;
            }
            
            return InternalChangeState(newStateId, newState);
        }

        /// <summary>
        /// 获取状态信息
        /// </summary>
        public string GetStateInfo()
        {
            if (!_isInitialized) return "未初始化";

            return $"当前状态: {_currentStateId}, 运行时间: {_stateTime:F2}s, 帧数: {_frameCount}";
        }

        /// <summary>
        /// 获取所有注册的状态列表
        /// </summary>
        public IEnumerable<T> GetRegisteredStates()
        {
            return _registeredStates.Keys;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 内部状态切换逻辑
        /// </summary>
        private bool InternalChangeState(T newStateId, IProcedureState newState)
        {
            _isChanging = true;
            
            try
            {
                Log.Info($"状态切换: {_currentStateId} -> {newStateId}");
                
                // 保存旧状态信息
                var oldState = _currentState;
                var oldStateId = _currentStateId;
                
                // 退出旧状态
                oldState?.Exit();
                
                // 更新状态引用
                _previousState = oldState;
                _previousStateId = oldStateId;
                _currentState = newState;
                _currentStateId = newStateId;
                
                ResetStateTime();
                
                // 进入新状态
                newState.Enter();
                
                // 发送状态改变事件
                Vomit.Interface?.SendEvent(new EvtProcedure.Changed
                {
                    FromStateId = oldStateId.ToString(),
                    ToStateId = newStateId.ToString(),
                    FromState = oldState,
                    ToState = newState
                });
                
                Log.Info($"状态切换完成: {oldStateId} -> {newStateId}");
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"状态切换失败: {e.Message}");
                return false;
            }
            finally
            {
                _isChanging = false;
            }
        }
        
        /// <summary>
        /// 重置状态时间
        /// </summary>
        private void ResetStateTime()
        {
            _frameCount = 0;
            _stateTime = 0f;
        }
        
        /// <summary>
        /// 启动Update循环
        /// </summary>
        private async UniTaskVoid StartUpdateLoop()
        {
            // Update循环
            var updateTask = UniTask.Create(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        if (_currentState != null && !_isChanging)
                        {
                            _currentState.Update(Time.deltaTime, Time.unscaledDeltaTime);
                            _frameCount++;
                            _stateTime += Time.deltaTime;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"状态Update异常: {e.Message}");
                    }
                    
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
            });
            
            // FixedUpdate循环
            var fixedUpdateTask = UniTask.Create(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        if (_currentState != null && !_isChanging)
                        {
                            _currentState.FixedUpdate();
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"状态FixedUpdate异常: {e.Message}");
                    }
                    
                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
                }
            });
            
            // 等待两个循环都结束
            await UniTask.WhenAll(updateTask, fixedUpdateTask);
        }
        
        #endregion
    }
}