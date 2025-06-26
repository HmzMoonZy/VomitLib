using System;
using System.Reflection;
using UnityEngine;

namespace Twenty2.VomitLib.Procedure
{
    /// <summary>
    /// Procedure监控组件
    /// 可以挂载到任何GameObject上，在Inspector中显示Procedure系统状态
    /// 用户需要指定要监控的状态机泛型类型
    /// </summary>
    public class ProcedureMonitor : MonoBehaviour
    {
        [Header("Procedure系统监控")]
        [Tooltip("要监控的状态机泛型类型\n格式：类名+枚举名，例如：Game+State 表示 ProcedureStateMachine<Game.State>")]
        [SerializeField] private string _stateMachineGenericType = "Game+State";
        
        [Space(10)]
        [Header("运行时信息")]
        [SerializeField, TextArea(3, 10)] private string _runtimeInfo = "请在Play模式下查看实时信息";
        
        [Space(5)]
        [SerializeField] private bool _autoRefresh = true;
        [SerializeField] private float _refreshInterval = 1f;
        
        private float _lastRefreshTime;
        private object _stateMachineInstance;
        private Type _stateMachineType;
        
        #region Unity Callbacks
        
        private void Start()
        {
            InitializeStateMachine();
        }
        
        private void Update()
        {
            if (_autoRefresh && Application.isPlaying && Time.time - _lastRefreshTime > _refreshInterval)
            {
                RefreshInfo();
                _lastRefreshTime = Time.time;
            }
        }
        
        #if UNITY_EDITOR
        private void Reset()
        {
            _stateMachineGenericType = "Game+State";
            _runtimeInfo = "在Play模式下会显示当前状态、运行时间等信息";
        }
        #endif
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 初始化状态机
        /// </summary>
        private void InitializeStateMachine()
        {
            try
            {
                if (string.IsNullOrEmpty(_stateMachineGenericType))
                {
                    _runtimeInfo = "错误：请指定状态机泛型类型";
                    return;
                }
                
                // 解析泛型类型，例如 "Game+State" 表示 ProcedureStateMachine<Game.State>
                var parts = _stateMachineGenericType.Split('+');
                if (parts.Length != 2)
                {
                    _runtimeInfo = "错误：泛型类型格式应为 '类名+枚举名'，例如：Game+State";
                    return;
                }
                
                var className = parts[0];
                var enumName = parts[1];
                
                // 查找枚举类型
                Type enumType = null;
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        // 先尝试作为嵌套类型
                        var nestedType = assembly.GetType($"{className}+{enumName}");
                        if (nestedType != null && nestedType.IsEnum)
                        {
                            enumType = nestedType;
                            break;
                        }
                        
                        // 再尝试作为独立类型
                        var standaloneType = assembly.GetType(enumName);
                        if (standaloneType != null && standaloneType.IsEnum)
                        {
                            enumType = standaloneType;
                            break;
                        }
                    }
                    catch { /* 忽略加载错误 */ }
                }
                
                if (enumType == null)
                {
                    _runtimeInfo = $"错误：未找到枚举类型 {className}.{enumName}";
                    return;
                }
                
                // 构造ProcedureStateMachine<T>类型
                var genericStateMachineType = typeof(ProcedureStateMachine<>).MakeGenericType(enumType);
                _stateMachineType = genericStateMachineType;
                
                // 获取Instance属性
                var instanceProperty = genericStateMachineType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty != null)
                {
                    _stateMachineInstance = instanceProperty.GetValue(null);
                    
                    if (_stateMachineInstance != null)
                    {
                        _runtimeInfo = $"成功连接到状态机: ProcedureStateMachine<{enumType.Name}>";
                        RefreshInfo();
                    }
                    else
                    {
                        _runtimeInfo = "错误：状态机实例为空，可能尚未初始化";
                    }
                }
                else
                {
                    _runtimeInfo = "错误：未找到状态机的Instance属性";
                }
            }
            catch (Exception e)
            {
                _runtimeInfo = $"初始化失败：{e.Message}";
            }
        }
        
        /// <summary>
        /// 刷新信息
        /// </summary>
        private void RefreshInfo()
        {
            if (_stateMachineInstance == null || _stateMachineType == null)
            {
                return;
            }
            
            try
            {
                var info = "🔄 Procedure状态机监控\n\n";
                
                // 获取基本状态
                var isRunning = GetPropertyValue<bool>("IsRunning");
                var isInitialized = GetPropertyValue<bool>("IsInitialized");
                var currentStateId = GetPropertyValue<object>("CurrentStateId");
                var previousStateId = GetPropertyValue<object>("PreviousStateId");
                var frameCount = GetPropertyValue<long>("FrameCount");
                var stateTime = GetPropertyValue<float>("StateTime");
                
                info += $"运行状态: {(isRunning ? "✅ 运行中" : "❌ 已停止")}\n";
                info += $"初始化状态: {(isInitialized ? "✅ 已初始化" : "❌ 未初始化")}\n";
                info += $"当前状态: {currentStateId ?? "无"}\n";
                info += $"上一状态: {previousStateId ?? "无"}\n";
                info += $"运行时间: {stateTime:F1}s\n";
                info += $"运行帧数: {frameCount}\n";
                
                // 获取注册状态数量
                var registeredStatesField = _stateMachineType.GetField("_registeredStates", BindingFlags.NonPublic | BindingFlags.Instance);
                if (registeredStatesField != null)
                {
                    var registeredStates = registeredStatesField.GetValue(_stateMachineInstance);
                    if (registeredStates != null)
                    {
                        var countProperty = registeredStates.GetType().GetProperty("Count");
                        if (countProperty != null)
                        {
                            var count = countProperty.GetValue(registeredStates);
                            info += $"注册状态数: {count}\n";
                        }
                    }
                }
                
                info += $"\n⏰ 最后更新: {DateTime.Now:HH:mm:ss}";
                
                _runtimeInfo = info;
            }
            catch (Exception e)
            {
                _runtimeInfo = $"刷新信息失败：{e.Message}";
            }
        }
        
        /// <summary>
        /// 获取属性值
        /// </summary>
        private T GetPropertyValue<T>(string propertyName)
        {
            try
            {
                var property = _stateMachineType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    var value = property.GetValue(_stateMachineInstance);
                    if (value is T) return (T)value;
                    if (value != null) return (T)Convert.ChangeType(value, typeof(T));
                }
                return default(T);
            }
            catch
            {
                return default(T);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 手动刷新信息
        /// </summary>
        [ContextMenu("刷新状态信息")]
        public void ManualRefresh()
        {
            RefreshInfo();
        }
        
        /// <summary>
        /// 重新初始化状态机
        /// </summary>
        [ContextMenu("重新初始化")]
        public void Reinitialize()
        {
            InitializeStateMachine();
        }
        
        #endregion
    }
}