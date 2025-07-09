using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using QFramework;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Twenty2.VomitLib.Monitor
{
    /// <summary>
    /// System监控器 - 用于运行时监控和调试QFramework的System层
    /// Runtime组件，监控功能仅在Editor下生效
    /// </summary>
    [System.Serializable]
    public class SystemMonitor : MonoBehaviour
    {
#if UNITY_EDITOR
        #region Fields

        [Header("System监控设置")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private float _refreshInterval = 0.5f;
        [SerializeField] private bool _autoRefresh = true;
        
        [Header("性能设置")]
        [SerializeField] private bool _monitorAllSystems = true;
        [SerializeField] private string _specificSystemTypeName = "";

        // Runtime数据（仅Editor可见）
        [System.NonSerialized] private List<SystemInfo> _systemInfos = new List<SystemInfo>();
        [System.NonSerialized] private Dictionary<Type, List<FieldPropertyInfo>> _systemFieldsCache = new Dictionary<Type, List<FieldPropertyInfo>>();
        [System.NonSerialized] private Dictionary<Type, List<MethodInfo>> _systemMethodsCache = new Dictionary<Type, List<MethodInfo>>();
        [System.NonSerialized] private double _lastRefreshTime;

        // 反射缓存
        [System.NonSerialized] private FieldInfo _containerFieldCache;
        [System.NonSerialized] private FieldInfo _instancesFieldCache;
        [System.NonSerialized] private bool _reflectionCacheInitialized = false;

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
            set => _refreshInterval = Mathf.Clamp(value, 0.1f, 5f);
        }

        public bool AutoRefresh
        {
            get => _autoRefresh;
            set => _autoRefresh = value;
        }

        public bool MonitorAllSystems
        {
            get => _monitorAllSystems;
            set => _monitorAllSystems = value;
        }

        public string SpecificSystemTypeName
        {
            get => _specificSystemTypeName;
            set => _specificSystemTypeName = value;
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
                InitializeReflectionCache();
                RefreshSystemData();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SystemMonitor] 初始化监控失败: {e.Message}");
            }
        }

        /// <summary>
        /// 刷新System数据
        /// </summary>
        public void RefreshSystemData()
        {
            if (!_enableMonitoring)
            {
                Debug.LogWarning("[SystemMonitor] 监控未启用");
                return;
            }
            
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[SystemMonitor] 不在运行模式");
                return;
            }

            var architecture = GetArchitecture();
            if (architecture == null)
            {
                Debug.LogWarning("[SystemMonitor] RefreshSystemData - 架构为null");
                return;
            }

            // Debug.Log("[SystemMonitor] 开始刷新System数据...");

            try
            {
                _systemInfos.Clear();
                CollectSystemInfos();
                // Debug.Log($"[SystemMonitor] 刷新完成 - 找到 {_systemInfos.Count} 个System");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SystemMonitor] 刷新System数据失败: {e.Message}");
                Debug.LogError($"[SystemMonitor] 堆栈: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 设置特定System监控
        /// </summary>
        public void SetSpecificSystem(string systemTypeName)
        {
            _specificSystemTypeName = systemTypeName;
            _monitorAllSystems = string.IsNullOrEmpty(systemTypeName);
            RefreshSystemData();
        }

        /// <summary>
        /// 获取System值
        /// </summary>
        public object GetSystemFieldValue(Type systemType, string fieldName)
        {
            var architecture = GetArchitecture();
            if (architecture == null) return null;

            try
            {
                var system = GetSystemInstance(systemType);
                if (system == null) return null;

                var fieldInfo = systemType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                    return fieldInfo.GetValue(system);

                var propertyInfo = systemType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null && propertyInfo.CanRead)
                    return propertyInfo.GetValue(system);

                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SystemMonitor] 获取System字段值失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 设置System值
        /// </summary>
        public bool SetSystemFieldValue(Type systemType, string fieldName, object value)
        {
            var architecture = GetArchitecture();
            if (architecture == null) return false;

            try
            {
                var system = GetSystemInstance(systemType);
                if (system == null) return false;

                var fieldInfo = systemType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null && !fieldInfo.IsInitOnly)
                {
                    fieldInfo.SetValue(system, value);
                    return true;
                }

                var propertyInfo = systemType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(system, value);
                    return true;
                }

                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SystemMonitor] 设置System字段值失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 调用System方法
        /// </summary>
        public object InvokeSystemMethod(Type systemType, string methodName, object[] parameters = null)
        {
            var architecture = GetArchitecture();
            if (architecture == null) return null;

            try
            {
                var system = GetSystemInstance(systemType);
                if (system == null) return null;

                var methods = systemType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    if (method.Name == methodName)
                    {
                        var paramTypes = method.GetParameters();
                        if ((parameters?.Length ?? 0) == paramTypes.Length)
                        {
                            return method.Invoke(system, parameters);
                        }
                    }
                }

                Debug.LogWarning($"[SystemMonitor] 未找到匹配的方法: {systemType.Name}.{methodName}");
                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SystemMonitor] 调用System方法失败: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Data Access Interface (for Editor)

        /// <summary>
        /// 获取System信息列表（仅Editor访问）
        /// </summary>
        public List<SystemInfo> GetSystemInfos() => _systemInfos ?? new List<SystemInfo>();

        /// <summary>
        /// 获取System字段信息（仅Editor访问）
        /// </summary>
        public List<FieldPropertyInfo> GetSystemFields(Type systemType)
        {
            if (_systemFieldsCache.TryGetValue(systemType, out var cached))
                return cached;

            var fields = new List<FieldPropertyInfo>();
            
            // 获取字段
            var fieldInfos = systemType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fieldInfos)
            {
                if (field.IsStatic || field.Name.StartsWith("<")) continue;
                
                fields.Add(new FieldPropertyInfo
                {
                    Name = field.Name,
                    Type = field.FieldType,
                    IsProperty = false,
                    CanRead = true,
                    CanWrite = !field.IsInitOnly,
                    IsPublic = field.IsPublic
                });
            }

            // 获取属性
            var propertyInfos = systemType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var property in propertyInfos)
            {
                if (property.GetIndexParameters().Length > 0) continue; // 跳过索引器
                
                fields.Add(new FieldPropertyInfo
                {
                    Name = property.Name,
                    Type = property.PropertyType,
                    IsProperty = true,
                    CanRead = property.CanRead,
                    CanWrite = property.CanWrite,
                    IsPublic = property.GetGetMethod()?.IsPublic == true || property.GetSetMethod()?.IsPublic == true
                });
            }

            _systemFieldsCache[systemType] = fields;
            return fields;
        }

        /// <summary>
        /// 获取System方法信息（仅Editor访问）
        /// </summary>
        public List<MethodInfo> GetSystemMethods(Type systemType)
        {
            if (_systemMethodsCache.TryGetValue(systemType, out var cached))
                return cached;

            var methods = new List<MethodInfo>();
            var methodInfos = systemType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var method in methodInfos)
            {
                // 跳过属性访问器、构造函数、继承的Object方法等
                if (method.IsSpecialName || method.DeclaringType == typeof(object) || 
                    method.DeclaringType == typeof(AbstractSystem) || method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                    continue;

                methods.Add(method);
            }

            _systemMethodsCache[systemType] = methods;
            return methods;
        }

        /// <summary>
        /// 清空缓存（仅Editor访问）
        /// </summary>
        public void ClearCache()
        {
            _systemFieldsCache.Clear();
            _systemMethodsCache.Clear();
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// 初始化反射缓存
        /// </summary>
        private void InitializeReflectionCache()
        {
            if (_reflectionCacheInitialized) return;

            var architecture = GetArchitecture();
            if (architecture == null)
            {
                Debug.LogWarning("[SystemMonitor] 获取架构失败 - Vomit.Interface返回null");
                return;
            }

            try
            {
                var architectureType = architecture.GetType();
                // Debug.Log($"[SystemMonitor] 架构类型: {architectureType.Name}");
                
                // 尝试多种可能的容器字段名
                string[] possibleContainerNames = { "mContainer", "_container", "container", "Container" };
                foreach (var name in possibleContainerNames)
                {
                    _containerFieldCache = architectureType.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance) 
                                        ?? architectureType.BaseType?.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (_containerFieldCache != null)
                    {
                        // Debug.Log($"[SystemMonitor] 找到容器字段: {name}");
                        break;
                    }
                }
                
                // 缓存实例字典字段
                _instancesFieldCache = typeof(QFramework.IOCContainer).GetField("mInstances", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                _reflectionCacheInitialized = true;
                // Debug.Log("[SystemMonitor] 反射缓存初始化完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SystemMonitor] 反射缓存初始化失败: {e.Message}");
                Debug.LogError($"[SystemMonitor] 堆栈: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 更新监控数据
        /// </summary>
        private void UpdateMonitoringData()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                RefreshSystemData();
            }
        }

        /// <summary>
        /// 收集System信息
        /// </summary>
        private void CollectSystemInfos()
        {
            if (!_reflectionCacheInitialized) 
            {
                Debug.LogWarning("[SystemMonitor] 反射缓存未初始化");
                return;
            }

            var architecture = GetArchitecture();
            if (architecture == null) 
            {
                Debug.LogWarning("[SystemMonitor] CollectSystemInfos - 架构为null");
                return;
            }

            // Debug.Log("[SystemMonitor] 开始收集System信息...");

            var container = _containerFieldCache?.GetValue(architecture) as QFramework.IOCContainer;
            if (container == null)
            {
                Debug.LogWarning($"[SystemMonitor] 无法获取IOC容器 - 容器字段: {_containerFieldCache?.Name}");
                return;
            }

            var instances = _instancesFieldCache?.GetValue(container) as System.Collections.IDictionary;
            if (instances == null)
            {
                Debug.LogWarning($"[SystemMonitor] 无法获取实例字典 - 实例字段: {_instancesFieldCache?.Name}");
                return;
            }

            // Debug.Log($"[SystemMonitor] IOC容器中有 {instances.Count} 个实例");

            int systemCount = 0;
            foreach (System.Collections.DictionaryEntry entry in instances)
            {
                var instanceType = entry.Key as Type;
                var instance = entry.Value;
                
                // Debug.Log($"[SystemMonitor] 检查实例: {instanceType?.Name} - 是否为ISystem: {(instanceType != null && typeof(ISystem).IsAssignableFrom(instanceType))}");
                
                if (instanceType != null && instance != null && 
                    typeof(ISystem).IsAssignableFrom(instanceType))
                {
                    systemCount++;
                    // Debug.Log($"[SystemMonitor] 找到System: {instanceType.Name}");

                    // 性能优化：只监控指定的System
                    if (!_monitorAllSystems && !string.IsNullOrEmpty(_specificSystemTypeName))
                    {
                        if (!instanceType.Name.Contains(_specificSystemTypeName))
                        {
                            // Debug.Log($"[SystemMonitor] 跳过System: {instanceType.Name} (不匹配过滤条件: {_specificSystemTypeName})");
                            continue;
                        }
                    }

                    var system = instance as ISystem;
                    var fields = GetSystemFields(instanceType);
                    var methods = GetSystemMethods(instanceType);
                    
                    var systemInfo = new SystemInfo
                    {
                        Name = instanceType.Name,
                        Type = instanceType,
                        FullTypeName = instanceType.FullName,
                        IsInitialized = system?.Initialized ?? false,
                        FieldCount = fields.Count,
                        MethodCount = methods.Count,
                        Instance = instance
                    };
                    
                    _systemInfos.Add(systemInfo);
                    // Debug.Log($"[SystemMonitor] 添加System: {systemInfo.Name}, 字段数: {systemInfo.FieldCount}, 方法数: {systemInfo.MethodCount}, 已初始化: {systemInfo.IsInitialized}");
                }
            }

            // Debug.Log($"[SystemMonitor] 收集完成 - 总实例数: {instances.Count}, System数: {systemCount}, 最终添加: {_systemInfos.Count}");
        }

        /// <summary>
        /// 获取System实例
        /// </summary>
        private object GetSystemInstance(Type systemType)
        {
            if (!_reflectionCacheInitialized) return null;

            var architecture = GetArchitecture();
            if (architecture == null) return null;

            if (_containerFieldCache?.GetValue(architecture) is QFramework.IOCContainer container)
            {
                if (_instancesFieldCache?.GetValue(container) is System.Collections.IDictionary instances)
                {
                    if (instances.Contains(systemType))
                        return instances[systemType];
                }
            }

            return null;
        }

        #endregion
#endif
    }

    #region Data Classes

    /// <summary>
    /// System信息
    /// </summary>
    [System.Serializable]
    public class SystemInfo
    {
        public string Name;
        public Type Type;
        public string FullTypeName;
        public bool IsInitialized;
        public int FieldCount;
        public int MethodCount;
        public object Instance;
    }

    #endregion
}