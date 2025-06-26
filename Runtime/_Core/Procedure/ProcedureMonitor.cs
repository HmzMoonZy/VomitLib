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
        [Tooltip("要监控的状态机泛型类型的完整类名\n例如：Game+State 或 GameState 或 ProjectName.GameState")]
        [SerializeField] private string _genericTypeFullName = "Game+State";
        
        [Space(5)]
        [Header("常用示例")]
        [Tooltip("常见的类型格式示例")]
        [SerializeField, TextArea(2, 3)] private string _examples = "嵌套枚举: Game+State\n独立枚举: GameState\n完整类名: MyProject.States.GameState";
        
        [Space(10)]
        [Header("运行时信息")]
        [SerializeField, TextArea(3, 12)] private string _runtimeInfo = "请在Play模式下查看实时信息";
        
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
            _genericTypeFullName = "Game+State";
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
                if (string.IsNullOrEmpty(_genericTypeFullName))
                {
                    _runtimeInfo = "错误：请指定泛型类型的完整类名";
                    return;
                }
                
                // 查找类型
                Type targetType = FindType(_genericTypeFullName);
                
                if (targetType == null)
                {
                    _runtimeInfo = $"错误：未找到类型 '{_genericTypeFullName}'\n\n支持的格式：\n1. 嵌套枚举: Game+State\n2. 独立枚举: GameState\n3. 完整类名: MyProject.GameState";
                    return;
                }
                
                if (!targetType.IsEnum)
                {
                    _runtimeInfo = $"错误：类型 '{targetType.FullName}' 不是枚举类型";
                    return;
                }
                
                // 构造ProcedureStateMachine<T>类型
                var genericStateMachineType = typeof(ProcedureStateMachine<>).MakeGenericType(targetType);
                _stateMachineType = genericStateMachineType;
                
                // 获取Instance属性
                var instanceProperty = genericStateMachineType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty != null)
                {
                    _stateMachineInstance = instanceProperty.GetValue(null);
                    
                    if (_stateMachineInstance != null)
                    {
                        _runtimeInfo = $"✅ 成功连接到状态机\nProcedureStateMachine<{targetType.Name}>";
                        RefreshInfo();
                    }
                    else
                    {
                        _runtimeInfo = "⚠️ 状态机实例为空，可能尚未初始化\n请确保游戏已启动并初始化了Procedure系统";
                    }
                }
                else
                {
                    _runtimeInfo = "错误：未找到状态机的Instance属性";
                }
            }
            catch (Exception e)
            {
                _runtimeInfo = $"初始化失败：{e.Message}\n\n请检查类型名称是否正确";
            }
        }
        
        /// <summary>
        /// 查找类型，支持多种格式
        /// </summary>
        private Type FindType(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    // 1. 直接按完整类名查找
                    var directType = assembly.GetType(typeName);
                    if (directType != null && directType.IsEnum)
                    {
                        return directType;
                    }
                    
                    // 2. 如果包含+号，按嵌套类型查找
                    if (typeName.Contains("+"))
                    {
                        var nestedType = assembly.GetType(typeName);
                        if (nestedType != null && nestedType.IsEnum)
                        {
                            return nestedType;
                        }
                    }
                    
                    // 3. 按简单名称在所有类型中搜索
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.IsEnum)
                        {
                            // 检查简单名称
                            if (type.Name == typeName)
                            {
                                return type;
                            }
                            
                            // 检查完整名称
                            if (type.FullName == typeName)
                            {
                                return type;
                            }
                            
                            // 检查嵌套类型的特殊格式
                            if (type.FullName != null && type.FullName.Replace('+', '.').EndsWith($".{typeName}"))
                            {
                                return type;
                            }
                        }
                    }
                }
                catch
                {
                    // 忽略加载错误，继续查找其他程序集
                    continue;
                }
            }
            
            return null;
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
        
        /// <summary>
        /// 获取当前程序集中所有的枚举类型
        /// </summary>
        [ContextMenu("显示所有枚举类型")]
        public void ShowAllEnumTypes()
        {
            var enumTypes = new System.Collections.Generic.List<string>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    // 只显示项目程序集的枚举，过滤Unity和系统程序集
                    if (assembly.FullName.StartsWith("Unity") || 
                        assembly.FullName.StartsWith("System") || 
                        assembly.FullName.StartsWith("mscorlib") ||
                        assembly.FullName.StartsWith("netstandard"))
                        continue;
                        
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.IsEnum)
                        {
                            enumTypes.Add(type.FullName);
                        }
                    }
                }
                catch { /* 忽略加载错误 */ }
            }
            
            enumTypes.Sort();
            var enumList = string.Join("\n", enumTypes);
            
            Debug.Log($"项目中所有可用的枚举类型（{enumTypes.Count}个）：\n{enumList}");
            
            _runtimeInfo = $"📄 所有可用枚举类型（{enumTypes.Count}个）：\n\n{enumList}\n\n请复制其中一个到上面的字段中";
        }
        
        #endregion
    }
}