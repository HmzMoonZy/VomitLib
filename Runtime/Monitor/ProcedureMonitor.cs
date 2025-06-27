#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;

namespace Twenty2.VomitLib.Monitor
{
    /// <summary>
    /// Procedure系统监控器 - 监控状态机状态
    /// </summary>
    public class ProcedureMonitor : MonoBehaviour
    {
        [Header("Procedure监控设置")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private float _refreshInterval = 1.0f;
        [SerializeField] private string _stateMachineType = "Game+State";
        
        [Space(10)]
        [Header("监控信息")]
        [SerializeField, TextArea(6, 10)] 
        private string _monitorInfo = "Procedure监控信息将在这里显示...";
        
        // 内部状态
        private float _lastRefreshTime;
        private object _stateMachineInstance;
        private Type _stateMachineTypeInfo;
        private ProcedureDebugInfo _debugInfo;
        
        // 状态历史追踪
        private object _lastStateId;
        private System.Collections.Generic.List<string> _stateHistory = new System.Collections.Generic.List<string>();
        private const int MAX_HISTORY_COUNT = 20;
        
        #region Unity Callbacks
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            if (_enableMonitoring && Time.time - _lastRefreshTime > _refreshInterval)
            {
                RefreshMonitorInfo();
                _lastRefreshTime = Time.time;
            }
        }
        
        private void OnEnable()
        {
            // 当对象重新激活时（如场景切换后），重新初始化
            if (_stateMachineInstance == null)
            {
                Initialize();
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 初始化Procedure监控
        /// </summary>
        private void Initialize()
        {
            try
            {
                if (string.IsNullOrEmpty(_stateMachineType))
                {
                    _monitorInfo = "[ERROR] 请指定状态机类型";
                    return;
                }
                
                // 查找目标枚举类型
                Type targetType = FindEnumType(_stateMachineType);
                if (targetType?.IsEnum == true)
                {
                    // 查找ProcedureStateMachine<T>类型
                    var procedureStateMachineType = FindTypeByName("ProcedureStateMachine`1");
                    if (procedureStateMachineType != null)
                    {
                        // 构造泛型类型
                        _stateMachineTypeInfo = procedureStateMachineType.MakeGenericType(targetType);
                        
                        // 获取Instance属性
                        var instanceProperty = _stateMachineTypeInfo.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                        _stateMachineInstance = instanceProperty?.GetValue(null);
                    }
                }
                
                RefreshMonitorInfo();
            }
            catch (Exception e)
            {
                _monitorInfo = $"[ERROR] Procedure监控初始化失败: {e.Message}";
            }
        }
        
        #endregion
        
        #region Monitor Logic
        
        /// <summary>
        /// 刷新监控信息
        /// </summary>
        private void RefreshMonitorInfo()
        {
            try
            {
                _debugInfo = ExtractProcedureDebugInfo();
                _monitorInfo = BuildMonitorInfoText();
            }
            catch (Exception e)
            {
                _monitorInfo = $"[ERROR] 刷新Procedure监控信息失败: {e.Message}";
            }
        }
        
        /// <summary>
        /// 提取Procedure调试信息
        /// </summary>
        private ProcedureDebugInfo ExtractProcedureDebugInfo()
        {
            var info = new ProcedureDebugInfo();
            
            try
            {
                if (_stateMachineInstance == null || _stateMachineTypeInfo == null)
                {
                    info.ErrorMessage = $"状态机未连接 (类型: {_stateMachineType})";
                    return info;
                }
                
                // 获取状态机基本信息
                info.IsRunning = GetPropertyValue<bool>("IsRunning");
                info.IsInitialized = GetPropertyValue<bool>("IsInitialized");
                info.CurrentStateId = GetPropertyValue<object>("CurrentStateId");
                info.PreviousStateId = GetPropertyValue<object>("PreviousStateId");
                info.FrameCount = GetPropertyValue<long>("FrameCount");
                info.StateTime = GetPropertyValue<float>("StateTime");
                info.StateMachineType = _stateMachineTypeInfo.Name;
                
                // 获取注册状态数量
                var registeredStatesField = _stateMachineTypeInfo.GetField("_registeredStates", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (registeredStatesField != null)
                {
                    var registeredStates = registeredStatesField.GetValue(_stateMachineInstance);
                    if (registeredStates != null)
                    {
                        var countProperty = registeredStates.GetType().GetProperty("Count");
                        if (countProperty != null)
                        {
                            info.RegisteredStatesCount = (int)countProperty.GetValue(registeredStates);
                        }
                    }
                }
                
                // 获取当前状态类型名称
                if (info.CurrentStateId != null)
                {
                    info.CurrentStateTypeName = info.CurrentStateId.GetType().Name;
                }
                
                // 状态历史追踪
                TrackStateHistory(info.CurrentStateId);
                info.StateHistory = _stateHistory.ToArray();
                
                info.IsValid = true;
            }
            catch (Exception e)
            {
                info.ErrorMessage = $"提取Procedure信息失败: {e.Message}";
            }
            
            return info;
        }
        
        /// <summary>
        /// 构建监控信息文本
        /// </summary>
        private string BuildMonitorInfoText()
        {
            var text = "PROCEDURE 系统监控\n";
            text += "────────────────────\n";
            text += $"更新时间: {DateTime.Now:HH:mm:ss}\n\n";
            
            if (_debugInfo?.IsValid == true)
            {
                text += "状态机信息:\n";
                text += $"  运行状态: {(_debugInfo.IsRunning ? "[RUNNING]" : "[STOPPED]")}\n";
                text += $"  初始化: {(_debugInfo.IsInitialized ? "[OK]" : "[NO]")}\n";
                text += $"  注册状态数: {_debugInfo.RegisteredStatesCount}\n";
                text += $"  类型: {_debugInfo.StateMachineType}\n\n";
                
                text += "当前状态:\n";
                text += $"  状态ID: {_debugInfo.CurrentStateId ?? "无"}\n";
                text += $"  状态类型: {_debugInfo.CurrentStateTypeName ?? "无"}\n";
                text += $"  运行时间: {_debugInfo.StateTime:F1}s\n";
                text += $"  运行帧数: {_debugInfo.FrameCount}\n";
                
                if (_debugInfo.PreviousStateId != null)
                {
                    text += $"  上一状态: {_debugInfo.PreviousStateId}\n";
                }
            }
            else
            {
                text += $"[ERROR] {_debugInfo?.ErrorMessage ?? "未知错误"}\n";
            }
            
            return text;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// 根据名称查找类型
        /// </summary>
        private Type FindTypeByName(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.Name == typeName || type.FullName?.EndsWith($".{typeName}") == true)
                        {
                            return type;
                        }
                    }
                }
                catch { continue; }
            }
            return null;
        }
        
        /// <summary>
        /// 查找枚举类型
        /// </summary>
        private Type FindEnumType(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    // 直接按完整类名查找
                    var directType = assembly.GetType(typeName);
                    if (directType?.IsEnum == true) return directType;
                    
                    // 如果包含+号，按嵌套类型查找
                    if (typeName.Contains("+"))
                    {
                        var nestedType = assembly.GetType(typeName);
                        if (nestedType?.IsEnum == true) return nestedType;
                    }
                    
                    // 按简单名称在所有类型中搜索
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.IsEnum)
                        {
                            if (type.Name == typeName || type.FullName == typeName)
                            {
                                return type;
                            }
                        }
                    }
                }
                catch { continue; }
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取属性值
        /// </summary>
        private T GetPropertyValue<T>(string propertyName)
        {
            try
            {
                var property = _stateMachineTypeInfo.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                var value = property?.GetValue(_stateMachineInstance);
                return value is T ? (T)value : default(T);
            }
            catch
            {
                return default(T);
            }
        }
        
        /// <summary>
        /// 追踪状态历史
        /// </summary>
        private void TrackStateHistory(object currentStateId)
        {
            if (currentStateId == null) return;
            
            // 检查状态是否发生变化
            bool stateChanged = false;
            if (_lastStateId == null)
            {
                stateChanged = true;
            }
            else if (!_lastStateId.Equals(currentStateId))
            {
                stateChanged = true;
            }
            
            if (stateChanged)
            {
                var timestamp = System.DateTime.Now.ToString("HH:mm:ss");
                var historyEntry = $"[{timestamp}] {currentStateId}";
                
                // 添加到历史记录
                _stateHistory.Insert(0, historyEntry);
                
                // 限制历史记录数量
                if (_stateHistory.Count > MAX_HISTORY_COUNT)
                {
                    _stateHistory.RemoveAt(_stateHistory.Count - 1);
                }
                
                _lastStateId = currentStateId;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 手动刷新监控信息
        /// </summary>
        [ContextMenu("刷新Procedure监控")]
        public void ManualRefresh()
        {
            RefreshMonitorInfo();
        }
        
        /// <summary>
        /// 重新初始化
        /// </summary>
        [ContextMenu("重新初始化")]
        public void Reinitialize()
        {
            Initialize();
        }
        
        /// <summary>
        /// 获取当前调试信息
        /// </summary>
        public ProcedureDebugInfo GetDebugInfo() => _debugInfo;
        
        #endregion
        
        #region Data Classes
        
        /// <summary>
        /// Procedure调试信息
        /// </summary>
        public class ProcedureDebugInfo
        {
            public bool IsValid;
            public string ErrorMessage;
            
            // 状态机信息
            public bool IsRunning;
            public bool IsInitialized;
            public long FrameCount;
            public float StateTime;
            public string StateMachineType;
            
            // 当前状态信息
            public object CurrentStateId;
            public object PreviousStateId;
            public string CurrentStateTypeName;
            
            // 注册状态信息
            public int RegisteredStatesCount;
            
            // 状态历史（新增）
            public string[] StateHistory;
        }
        
        #endregion
    }
}
#endif