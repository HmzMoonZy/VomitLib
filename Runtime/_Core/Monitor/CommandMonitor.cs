using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using QFramework;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Twenty2.VomitLib.Monitor
{
    /// <summary>
    /// Command监控器 - 用于运行时监控和调试QFramework的Command系统
    /// Runtime组件，监控功能仅在Editor下生效
    /// </summary>
    [System.Serializable]
    public class CommandMonitor : MonoBehaviour
    {
        #region Fields

        [Header("Command监控设置")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private float _refreshInterval = 2.0f;
        [SerializeField] private bool _autoRefresh = false; // Command通常不需要频繁刷新

        [Header("扫描设置")]
        [SerializeField] private bool _includeSystemAssemblies = false;
        [SerializeField] private bool _includeUnityAssemblies = false;

        // Runtime数据（仅Editor可见）
        [System.NonSerialized] private List<CommandInfo> _commandInfos = new List<CommandInfo>();
        [System.NonSerialized] private Dictionary<Type, CommandExecutionResult> _executionResults = new Dictionary<Type, CommandExecutionResult>();
        [System.NonSerialized] private double _lastRefreshTime;

        // 反射缓存
        [System.NonSerialized] private bool _commandsCacheInitialized = false;

        #endregion

        #region Properties

        public bool EnableMonitoring
        {
            get => _enableMonitoring;
            set => _enableMonitoring = value;
        }

        public float RefreshInterval
        {
            get => _refreshInterval;
            set => _refreshInterval = Mathf.Clamp(value, 0.5f, 10f);
        }

        public bool AutoRefresh
        {
            get => _autoRefresh;
            set => _autoRefresh = value;
        }

        public bool IncludeSystemAssemblies
        {
            get => _includeSystemAssemblies;
            set => _includeSystemAssemblies = value;
        }

        public bool IncludeUnityAssemblies
        {
            get => _includeUnityAssemblies;
            set => _includeUnityAssemblies = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_enableMonitoring)
            {
                InitializeMonitoring();
            }
        }

        private void Update()
        {
            if (_enableMonitoring && Application.isPlaying)
            {
                UpdateMonitoringData();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 获取当前架构实例
        /// </summary>
        private IArchitecture GetArchitecture()
        {
            return Vomit.Interface;
        }

        /// <summary>
        /// 初始化监控
        /// </summary>
        public void InitializeMonitoring()
        {
            if (!_enableMonitoring) return;

            try
            {
                RefreshCommandData();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CommandMonitor] 初始化监控失败: {e.Message}");
            }
        }

        /// <summary>
        /// 刷新Command数据
        /// </summary>
        public void RefreshCommandData()
        {
            if (!_enableMonitoring)
            {
                Debug.LogWarning("[CommandMonitor] 监控未启用");
                return;
            }

            Debug.Log("[CommandMonitor] 开始刷新Command数据...");

            try
            {
                _commandInfos.Clear();
                ScanForCommands();
                _commandsCacheInitialized = true;
                Debug.Log($"[CommandMonitor] 刷新完成 - 找到 {_commandInfos.Count} 个Command类型");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CommandMonitor] 刷新Command数据失败: {e.Message}");
                Debug.LogError($"[CommandMonitor] 堆栈: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 手动执行Command（用于调试）
        /// </summary>
        public void ExecuteCommand(Type commandType)
        {
            var architecture = GetArchitecture();
            if (architecture == null) 
            {
                Debug.LogWarning("[CommandMonitor] 无法获取架构实例");
                return;
            }

            try
            {
                // 检查Command类型
                var commandInfo = _commandInfos.FirstOrDefault(c => c.CommandType == commandType);
                if (commandInfo == null)
                {
                    Debug.LogError($"[CommandMonitor] 未找到Command信息: {commandType.Name}");
                    return;
                }

                // 创建Command实例
                var commandInstance = CreateCommandInstance(commandType);
                if (commandInstance == null)
                {
                    Debug.LogError($"[CommandMonitor] 无法创建Command实例: {commandType.Name}");
                    return;
                }

                var startTime = DateTime.Now;
                object result = null;
                Exception exception = null;

                try
                {
                    // 执行Command
                    if (commandInfo.HasReturnValue)
                    {
                        // 有返回值的Command
                        var sendMethod = typeof(IArchitecture).GetMethod("SendCommand", new Type[] { commandInfo.CommandInterfaceType });
                        if (sendMethod != null)
                        {
                            result = sendMethod.Invoke(architecture, new object[] { commandInstance });
                        }
                    }
                    else
                    {
                        // 无返回值的Command
                        var sendMethod = typeof(IArchitecture).GetMethod("SendCommand", new Type[] { commandType });
                        if (sendMethod == null)
                        {
                            // 尝试泛型版本
                            sendMethod = typeof(IArchitecture).GetMethod("SendCommand").MakeGenericMethod(commandType);
                        }
                        if (sendMethod != null)
                        {
                            sendMethod.Invoke(architecture, new object[] { commandInstance });
                        }
                    }

                    Debug.Log($"[CommandMonitor] 成功执行Command: {commandType.Name}");
                }
                catch (Exception e)
                {
                    exception = e.InnerException ?? e;
                    Debug.LogError($"[CommandMonitor] 执行Command失败: {commandType.Name}, 错误: {exception.Message}");
                }

                // 记录执行结果
                var executionTime = DateTime.Now - startTime;
                var executionResult = new CommandExecutionResult
                {
                    CommandType = commandType,
                    ExecutionTime = executionTime,
                    Success = exception == null,
                    Result = result,
                    Exception = exception,
                    Timestamp = startTime
                };

                _executionResults[commandType] = executionResult;

#if UNITY_EDITOR
                EditorApplication.delayCall += () => Repaint();
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CommandMonitor] 执行Command过程失败: {e.Message}");
            }
        }

        /// <summary>
        /// 清空执行结果
        /// </summary>
        public void ClearExecutionResults()
        {
            _executionResults.Clear();
        }

        #endregion

        #region Data Access Interface (for Editor)

#if UNITY_EDITOR

        /// <summary>
        /// 获取Command信息列表（仅Editor访问）
        /// </summary>
        public List<CommandInfo> GetCommandInfos() => _commandInfos ?? new List<CommandInfo>();

        /// <summary>
        /// 获取Command执行结果（仅Editor访问）
        /// </summary>
        public Dictionary<Type, CommandExecutionResult> GetExecutionResults() => _executionResults ?? new Dictionary<Type, CommandExecutionResult>();

        /// <summary>
        /// 清空缓存（仅Editor访问）
        /// </summary>
        public void ClearCache()
        {
            _commandInfos.Clear();
            _executionResults.Clear();
            _commandsCacheInitialized = false;
        }

        /// <summary>
        /// 强制重绘Inspector
        /// </summary>
        private void Repaint()
        {
            var inspector = Editor.CreateEditor(this);
            inspector.Repaint();
        }

#endif

        #endregion

        #region Private Methods

        /// <summary>
        /// 更新监控数据
        /// </summary>
        private void UpdateMonitoringData()
        {
#if UNITY_EDITOR
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                RefreshCommandData();
            }
#endif
        }

        /// <summary>
        /// 扫描程序集中的Command类
        /// </summary>
        private void ScanForCommands()
        {
            Debug.Log("[CommandMonitor] 开始扫描Command类型...");

            var assemblies = GetTargetAssemblies();
            int totalCommandCount = 0;

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    var commandTypes = types.Where(IsCommandType).ToList();

                    Debug.Log($"[CommandMonitor] 在程序集 {assembly.GetName().Name} 中找到 {commandTypes.Count} 个Command");

                    foreach (var commandType in commandTypes)
                    {
                        var commandInfo = CreateCommandInfo(commandType);
                        if (commandInfo != null)
                        {
                            _commandInfos.Add(commandInfo);
                            totalCommandCount++;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CommandMonitor] 扫描程序集 {assembly.GetName().Name} 失败: {e.Message}");
                }
            }

            Debug.Log($"[CommandMonitor] 扫描完成 - 总共找到 {totalCommandCount} 个Command类型");
        }

        /// <summary>
        /// 获取目标程序集列表
        /// </summary>
        private Assembly[] GetTargetAssemblies()
        {
            var assemblies = new List<Assembly>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyName = assembly.GetName().Name;
                
                // 跳过Unity内部程序集（除非明确要求包含）
                if (!_includeUnityAssemblies && IsUnityAssembly(assemblyName))
                    continue;
                    
                // 跳过系统程序集（除非明确要求包含）
                if (!_includeSystemAssemblies && IsSystemAssembly(assemblyName))
                    continue;
                
                assemblies.Add(assembly);
            }
            
            return assemblies.ToArray();
        }

        /// <summary>
        /// 判断是否为Unity程序集
        /// </summary>
        private bool IsUnityAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("Unity") || 
                   assemblyName.StartsWith("UnityEngine") || 
                   assemblyName.StartsWith("UnityEditor");
        }

        /// <summary>
        /// 判断是否为系统程序集
        /// </summary>
        private bool IsSystemAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("System") || 
                   assemblyName.StartsWith("mscorlib") ||
                   assemblyName.StartsWith("netstandard") ||
                   assemblyName.StartsWith("Microsoft");
        }

        /// <summary>
        /// 判断类型是否为Command
        /// </summary>
        private bool IsCommandType(Type type)
        {
            if (type.IsAbstract || type.IsInterface) return false;
            
            return typeof(ICommand).IsAssignableFrom(type) || 
                   type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
        }

        /// <summary>
        /// 创建Command信息
        /// </summary>
        private CommandInfo CreateCommandInfo(Type commandType)
        {
            try
            {
                var commandInfo = new CommandInfo
                {
                    CommandType = commandType,
                    CommandTypeName = commandType.FullName,
                    FullTypeName = commandType.AssemblyQualifiedName,
                    AssemblyName = commandType.Assembly.GetName().Name,
                    HasReturnValue = false,
                    ReturnType = null,
                    CommandInterfaceType = null,
                    CanInstantiate = CanInstantiateType(commandType)
                };

                // 检查是否有返回值
                var genericCommandInterface = commandType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
                
                if (genericCommandInterface != null)
                {
                    commandInfo.HasReturnValue = true;
                    commandInfo.ReturnType = genericCommandInterface.GetGenericArguments()[0];
                    commandInfo.CommandInterfaceType = genericCommandInterface;
                }
                else
                {
                    commandInfo.CommandInterfaceType = typeof(ICommand);
                }

                return commandInfo;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CommandMonitor] 创建CommandInfo失败: {commandType.Name}, 错误: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 判断类型是否可以实例化
        /// </summary>
        private bool CanInstantiateType(Type type)
        {
            // 检查是否有无参构造函数
            var constructors = type.GetConstructors();
            return constructors.Any(c => c.GetParameters().Length == 0);
        }

        /// <summary>
        /// 创建Command实例
        /// </summary>
        private object CreateCommandInstance(Type commandType)
        {
            try
            {
                return Activator.CreateInstance(commandType);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommandMonitor] 创建Command实例失败: {commandType.Name}, 错误: {e.Message}");
                return null;
            }
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Command信息
    /// </summary>
    [System.Serializable]
    public class CommandInfo
    {
        public Type CommandType;
        public string CommandTypeName;
        public string FullTypeName;
        public string AssemblyName;
        public bool HasReturnValue;
        public Type ReturnType;
        public Type CommandInterfaceType;
        public bool CanInstantiate;
    }

    /// <summary>
    /// Command执行结果
    /// </summary>
    [System.Serializable]
    public class CommandExecutionResult
    {
        public Type CommandType;
        public TimeSpan ExecutionTime;
        public bool Success;
        public object Result;
        public Exception Exception;
        public DateTime Timestamp;
    }

    #endregion
}