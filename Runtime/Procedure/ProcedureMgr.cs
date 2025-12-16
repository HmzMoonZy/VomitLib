using System;
using System.Collections.Generic;

namespace Twenty2.VomitLib.Procedure
{
    /// <summary>
    /// Procedure专用状态机 - 单例模式，一个游戏只有一个流程状态机
    /// </summary>
    public class ProcedureMgr<T> where T : struct, Enum
    {
        public delegate void ProcedureChangedDelegate(T? form, T? to, AbstractProcedure<T> state);
        
        #region Singleton
        private ProcedureMgr()
        {
            // 单例，私有构造函数
        }

        private static ProcedureMgr<T> _instance;
        public static ProcedureMgr<T> Instance => _instance ??= new ProcedureMgr<T>();
        
        #endregion
        
        #region Fields
        
        private readonly Dictionary<T, AbstractProcedure<T>> _registered = new();
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 当前状态机器
        /// </summary>
        public AbstractProcedure<T> CurrentState{get; private set;}
        
        /// <summary>
        /// 上一个状态机
        /// </summary>
        public AbstractProcedure<T> PreviousState{get; private set;}
        
        /// <summary>
        /// 当前状态ID
        /// </summary>
        public T? CurrentStateId => CurrentState?.ProcedureKey;

        /// <summary>
        /// 上一个状态ID
        /// </summary>
        public T? PreviousStateId => PreviousState?.ProcedureKey;
        
        /// <summary>
        /// 当前状态运行帧数
        /// </summary>
        public long FrameCount { get; private set; } = 0;

        /// <summary>
        /// 当前状态运行时间
        /// </summary>
        public float StateTime { get; private set; } = 0f;
        
        /// <summary>
        /// 状态机是否在运行
        /// </summary>
        public bool IsRunning{get; private set;}
        
        /// <summary>
        /// 状态机是否正在切换
        /// </summary>
        public bool IsChanging{get; private set;}
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized{get; private set;}
        
        #endregion

        #region Event

        public event ProcedureChangedDelegate OnProcedureChanged = null;

        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 注册流程
        /// </summary>
        public void RegisterState(AbstractProcedure<T> state)
        {
            if (IsInitialized)
            {
                Log.Error("状态机已初始化，无法注册新状态");
                return;
            }
            
            if (_registered.ContainsKey(state.ProcedureKey))
            {
                Log.Error($"状态 {state.ProcedureKey} 已注册，请勿重复注册");
                return;
            }
            
            _registered[state.ProcedureKey] = state;
            Log.Debug($"注册流程状态: {state.ProcedureKey}");
        }
        
        /// <summary>
        /// 初始化并启动状态机
        /// </summary>
        public void Initialize(T initialStateId, ProcedureArgsBase args)
        {
            if (IsInitialized)
            {
                Log.Warning("状态机已经初始化过了");
                return;
            }
            
            if (_registered.Count == 0)
            {
                Log.Error("没有注册任何状态，无法初始化状态机");
                return;
            }
            
            if (!_registered.ContainsKey(initialStateId))
            {
                Log.Error($"初始状态 {initialStateId} 未注册");
                return;
            }
            
            IsInitialized = true;
            
            // 启动状态机
            Run(initialStateId, args);
        }
        
        /// <summary>
        /// 停止状态机
        /// </summary>
        public void Shutdown()
        {
            if (!IsInitialized)
            {
                Log.Warning("状态机未初始化，无法停止状态机");
                return;
            }
            
            try
            {
                CurrentState?.Exit();
            }
            catch (Exception e)
            {
                Log.Error($"退出当前状态时发生错误: {e.Message}");
            }
            finally
            {
                IsRunning = false;
                IsInitialized = false;
                CurrentState = null;
                PreviousState = null;
                ResetMonitor();
                Log.Debug("状态机已停止");
            }
        }
 
        /// <summary>
        /// 切换状态
        /// </summary>
        public bool ChangeState(T newStateId, ProcedureArgsBase args)
        {
            if (!IsInitialized)
            {
                Log.Error("状态机未初始化，无法切换状态");
                return false;
            }
            
            if (IsChanging)
            {
                Log.Warning("状态正在切换中，忽略此次切换请求");
                return false;
            }
            
            if (newStateId.Equals(CurrentStateId))
            {
                Log.Warning($"尝试切换到相同状态: {newStateId}");
                return false;
            }
            
            if (!_registered.TryGetValue(newStateId, out var newState))
            {
                Log.Error($"找不到目标状态: {newStateId}");
                return false;
            }
            
            // 开始切换
            
            IsChanging = true;
            
            try
            {
                Log.Debug($"准备状态切换: {CurrentStateId} -> {newStateId}");
                
                // 保存旧状态信息
                var oldState = CurrentState;
                var oldStateId = CurrentStateId;
                
                // 退出旧状态
                oldState?.Exit();
                
                // 更新状态引用
                PreviousState = oldState;
                CurrentState = newState;
                ResetMonitor();
                
                // 进入新状态
                newState.Enter(args);
                
                // 发送状态改变事件
                OnProcedureChanged?.Invoke(oldStateId, newStateId, CurrentState);
                
                Log.Debug($"状态切换完成: {oldStateId} -> {newStateId}");
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"状态切换失败: {e.Message}");
                return false;
            }
            finally
            {
                IsChanging = false;
            }
        }
        
        /// <summary>
        /// 获取所有注册的状态列表
        /// </summary>
        public IEnumerable<T> GetRegisteredStates()
        {
            return _registered.Keys;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 内部启动方法
        /// </summary>
        private void Run(T runStateId, ProcedureArgsBase args)
        {
            if (IsRunning)
            {
                Log.Error("状态机已经在运行中，不能重复启动");
                return;
            }
            
            if (!_registered.TryGetValue(runStateId, out var runState))
            {
                Log.Error($"找不到初始状态: {runStateId}");
                return;
            }
            

            CurrentState = runState;
            PreviousState = null;
            
            ResetMonitor();
            
            try
            {
                runState.Enter(args);
                IsRunning = true;
                
                Log.Debug($"状态机启动成功，初始状态: {runStateId}");
            }
            catch (Exception e)
            {
                Log.Error($"状态机启动失败: {e.Message}");
                IsRunning = false;
            }
        }
        
        /// <summary>
        /// 重置状态机监控数据
        /// </summary>
        private void ResetMonitor()
        {
            FrameCount = 0;
            StateTime = 0f;
        }
        
        #endregion

        #region Static Methods

        /// <summary>
        /// 获取状态信息
        /// </summary>
        public static string GetStateInfo()
        {
            if (!Instance.IsInitialized) return "未初始化";

            return $"当前状态: {Instance.CurrentStateId}, 运行时间: {Instance.StateTime:F2}s, 帧数: {Instance.FrameCount}";
        }

        #endregion
    }
}