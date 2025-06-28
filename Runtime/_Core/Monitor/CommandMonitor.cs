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
        [System.NonSerialized] private Dictionary<Type, List<CommandInfo>> _interfaceCommandMap = new Dictionary<Type, List<CommandInfo>>();
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

            // Debug.Log("[CommandMonitor] 开始刷新Command数据...");

            try
            {
                _commandInfos.Clear();
                _interfaceCommandMap.Clear();
                ScanForCommands();
                BuildInterfaceCommandMap();
                _commandsCacheInitialized = true;
                // Debug.Log($"[CommandMonitor] 刷新完成 - 找到 {_commandInfos.Count} 个Command类型");
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
                    Log.Debug($"[CommandMonitor] 开始执行Command: {commandType.Name}, HasReturnValue: {commandInfo.HasReturnValue}");
                    
                    // 执行Command
                    if (commandInfo.HasReturnValue)
                    {
                        // 有返回值的Command - 使用泛型方法 TResult SendCommand<TResult>(ICommand<TResult> command)
                        Log.Debug($"[CommandMonitor] 执行Command[HasReturnValue]: {commandType.Name}, ReturnType: {commandInfo.ReturnType.Name}");
                        Log.Debug($"[CommandMonitor] CommandInterfaceType: {commandInfo.CommandInterfaceType.Name}");
                        
                        // 查找泛型SendCommand方法
                        var allMethods = typeof(IArchitecture).GetMethods();
                        Log.Debug($"[CommandMonitor] IArchitecture的所有方法数量: {allMethods.Length}");
                        
                        foreach (var method in allMethods.Where(m => m.Name == "SendCommand"))
                        {
                            Log.Debug($"[CommandMonitor] SendCommand方法: {method}, IsGeneric: {method.IsGenericMethod}, ParamCount: {method.GetParameters().Length}");
                            if (method.GetParameters().Length > 0)
                            {
                                var paramType = method.GetParameters()[0].ParameterType;
                                Log.Debug($"[CommandMonitor] 参数类型: {paramType}, IsGeneric: {paramType.IsGenericType}");
                                if (paramType.IsGenericType)
                                {
                                    Log.Debug($"[CommandMonitor] 泛型定义: {paramType.GetGenericTypeDefinition()}");
                                }
                            }
                        }
                        
                        var genericMethod = typeof(IArchitecture).GetMethods()
                            .FirstOrDefault(m => m.Name == "SendCommand" && 
                                          m.IsGenericMethod && 
                                          m.GetParameters().Length == 1 &&
                                          m.GetParameters()[0].ParameterType.IsGenericType &&
                                          m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(ICommand<>));
                        
                        if (genericMethod != null)
                        {
                            Log.Debug($"[CommandMonitor] 找到泛型方法: {genericMethod}");
                            // 构造具体的泛型方法
                            var constructedMethod = genericMethod.MakeGenericMethod(commandInfo.ReturnType);
                            Log.Debug($"[CommandMonitor] 构造的方法: {constructedMethod}");
                            result = constructedMethod.Invoke(architecture, new object[] { commandInstance });
                            Log.Debug($"[CommandMonitor] 成功执行有返回值Command: {commandType.Name}, 结果: {result}");
                        }
                        else
                        {
                            Log.Error($"[CommandMonitor] 未找到有返回值的SendCommand泛型方法");
                        }
                    }
                    else
                    {
                        // 无返回值的Command
                        Log.Debug($"[CommandMonitor] 执行Command[NoReturnValue]: {commandType.Name}");
                        var sendMethod = typeof(IArchitecture).GetMethod("SendCommand", new Type[] { commandType });
                        if (sendMethod != null)
                        {
                            sendMethod.Invoke(architecture, new object[] { commandInstance });
                        }
                        else
                        {
                            // 尝试使用泛型方法 void SendCommand<T>(T command) where T : ICommand
                            var genericMethod = typeof(IArchitecture).GetMethods()
                                .FirstOrDefault(m => m.Name == "SendCommand" && 
                                              m.IsGenericMethod && 
                                              m.GetParameters().Length == 1 && 
                                              m.ReturnType == typeof(void) &&
                                              m.GetGenericArguments().Length == 1);
                            if (genericMethod != null)
                            {
                                var constructedMethod = genericMethod.MakeGenericMethod(commandType);
                                constructedMethod.Invoke(architecture, new object[] { commandInstance });
                                Log.Debug($"[CommandMonitor] 成功执行无返回值Command: {commandType.Name}");
                            }
                            else
                            {
                                Log.Error($"[CommandMonitor] 未找到无返回值的SendCommand泛型方法: {commandType.Name}");
                            }
                        }
                    }

                    Log.Debug($"[CommandMonitor] 成功执行Command: {commandType.Name}");
                }
                catch (Exception e)
                {
                    exception = e.InnerException ?? e;
                    Log.Error($"[CommandMonitor] 执行Command失败: {commandType.Name}, 错误: {exception.Message}");
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
                Log.Error($"[CommandMonitor] 执行Command过程失败: {e.Message}");
            }
        }

        /// <summary>
        /// 清空执行结果
        /// </summary>
        public void ClearExecutionResults()
        {
            _executionResults.Clear();
        }

        /// <summary>
        /// 获取接口分组的Command映射
        /// </summary>
        public Dictionary<Type, List<CommandInfo>> GetInterfaceCommandMap() => _interfaceCommandMap ?? new Dictionary<Type, List<CommandInfo>>();

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
            _interfaceCommandMap.Clear();
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
            // Debug.Log("[CommandMonitor] 开始扫描Command类型...");

            var assemblies = GetTargetAssemblies();
            int totalCommandCount = 0;

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    var commandTypes = types.Where(IsCommandType).ToList();

                    // Debug.Log($"[CommandMonitor] 在程序集 {assembly.GetName().Name} 中找到 {commandTypes.Count} 个Command");

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
                    Log.Warning($"[CommandMonitor] 扫描程序集 {assembly.GetName().Name} 失败: {e.Message}");
                }
            }

            // Log.Debug($"[CommandMonitor] 扫描完成 - 总共找到 {totalCommandCount} 个Command类型");
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
                    CanInstantiate = CanInstantiateType(commandType),
                    ParentInterfaceType = GetParentInterfaceType(commandType),
                    IsNestedInInterface = commandType.IsNested && commandType.DeclaringType?.IsInterface == true
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
                Log.Warning($"[CommandMonitor] 创建CommandInfo失败: {commandType.Name}, 错误: {e.Message}");
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
                Log.Error($"[CommandMonitor] 创建Command实例失败: {commandType.Name}, 错误: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 构建接口-Command映射表
        /// </summary>
        private void BuildInterfaceCommandMap()
        {
            _interfaceCommandMap.Clear();
            
            foreach (var commandInfo in _commandInfos)
            {
                if (commandInfo.IsNestedInInterface && commandInfo.ParentInterfaceType != null)
                {
                    if (!_interfaceCommandMap.ContainsKey(commandInfo.ParentInterfaceType))
                    {
                        _interfaceCommandMap[commandInfo.ParentInterfaceType] = new List<CommandInfo>();
                    }
                    _interfaceCommandMap[commandInfo.ParentInterfaceType].Add(commandInfo);
                }
            }
        }

        /// <summary>
        /// 获取Command的父接口类型
        /// </summary>
        private Type GetParentInterfaceType(Type commandType)
        {
            if (commandType.IsNested && commandType.DeclaringType?.IsInterface == true)
            {
                return commandType.DeclaringType;
            }
            return null;
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
        public Type ParentInterfaceType;
        public bool IsNestedInInterface;
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