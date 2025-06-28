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
    /// Model监控器 - 用于运行时监控和修改QFramework的Model层数据
    /// Runtime组件，监控功能仅在Editor下生效
    /// </summary>
    [System.Serializable]
    public class ModelMonitor : MonoBehaviour
    {
        #region Fields

        [Header("Model监控设置")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private float _refreshInterval = 0.5f;
        [SerializeField] private bool _autoRefresh = true;
        
        [Header("性能设置")]
        [SerializeField] private bool _monitorAllModels = true;
        [SerializeField] private string _specificModelTypeName = "";

        // Runtime数据（仅Editor可见）
        [System.NonSerialized] private List<ModelInfo> _modelInfos = new List<ModelInfo>();
        [System.NonSerialized] private Dictionary<Type, List<FieldPropertyInfo>> _modelFieldsCache = new Dictionary<Type, List<FieldPropertyInfo>>();
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

        public bool MonitorAllModels
        {
            get => _monitorAllModels;
            set => _monitorAllModels = value;
        }

        public string SpecificModelTypeName
        {
            get => _specificModelTypeName;
            set => _specificModelTypeName = value;
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
                RefreshModelData();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ModelMonitor] 初始化监控失败: {e.Message}");
            }
        }

        /// <summary>
        /// 刷新Model数据
        /// </summary>
        public void RefreshModelData()
        {
            if (!_enableMonitoring)
            {
                Debug.LogWarning("[ModelMonitor] 监控未启用");
                return;
            }
            
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ModelMonitor] 不在运行模式");
                return;
            }

            var architecture = GetArchitecture();
            if (architecture == null)
            {
                Debug.LogWarning("[ModelMonitor] RefreshModelData - 架构为null");
                return;
            }

            // Debug.Log("[ModelMonitor] 开始刷新Model数据...");

            try
            {
                _modelInfos.Clear();
                CollectModelInfos();
                // Debug.Log($"[ModelMonitor] 刷新完成 - 找到 {_modelInfos.Count} 个Model");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ModelMonitor] 刷新Model数据失败: {e.Message}");
                Debug.LogError($"[ModelMonitor] 堆栈: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 设置特定Model监控
        /// </summary>
        public void SetSpecificModel(string modelTypeName)
        {
            _specificModelTypeName = modelTypeName;
            _monitorAllModels = string.IsNullOrEmpty(modelTypeName);
            RefreshModelData();
        }

        /// <summary>
        /// 获取Model值
        /// </summary>
        public object GetModelFieldValue(Type modelType, string fieldName)
        {
            var architecture = GetArchitecture();
            if (architecture == null) return null;

            try
            {
                var model = GetModelInstance(modelType);
                if (model == null) return null;

                var fieldInfo = modelType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                    return fieldInfo.GetValue(model);

                var propertyInfo = modelType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null && propertyInfo.CanRead)
                    return propertyInfo.GetValue(model);

                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ModelMonitor] 获取Model字段值失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 设置Model值
        /// </summary>
        public bool SetModelFieldValue(Type modelType, string fieldName, object value)
        {
            var architecture = GetArchitecture();
            if (architecture == null) return false;

            try
            {
                var model = GetModelInstance(modelType);
                if (model == null) return false;

                var fieldInfo = modelType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null && !fieldInfo.IsInitOnly)
                {
                    fieldInfo.SetValue(model, value);
                    return true;
                }

                var propertyInfo = modelType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(model, value);
                    return true;
                }

                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ModelMonitor] 设置Model字段值失败: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Data Access Interface (for Editor)

#if UNITY_EDITOR

        /// <summary>
        /// 获取Model信息列表（仅Editor访问）
        /// </summary>
        public List<ModelInfo> GetModelInfos() => _modelInfos ?? new List<ModelInfo>();

        /// <summary>
        /// 获取Model字段信息（仅Editor访问）
        /// </summary>
        public List<FieldPropertyInfo> GetModelFields(Type modelType)
        {
            if (_modelFieldsCache.TryGetValue(modelType, out var cached))
                return cached;

            var fields = new List<FieldPropertyInfo>();
            
            // 获取字段
            var fieldInfos = modelType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
            var propertyInfos = modelType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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

            _modelFieldsCache[modelType] = fields;
            return fields;
        }

        /// <summary>
        /// 清空缓存（仅Editor访问）
        /// </summary>
        public void ClearCache()
        {
            _modelFieldsCache.Clear();
        }

#endif

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
                Debug.LogWarning("[ModelMonitor] 获取架构失败 - Vomit.Interface返回null");
                return;
            }

            try
            {
                var architectureType = architecture.GetType();
                // Debug.Log($"[ModelMonitor] 架构类型: {architectureType.Name}");
                // Debug.Log($"[ModelMonitor] 架构完整类型: {architectureType.FullName}");
                // Debug.Log($"[ModelMonitor] 架构基类: {architectureType.BaseType?.FullName}");
                
                // 打印所有私有字段来找到正确的容器字段
                var allFields = architectureType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                // Debug.Log($"[ModelMonitor] 所有字段数量: {allFields.Length}");
                foreach (var field in allFields)
                {
                    // Debug.Log($"[ModelMonitor] 字段: {field.Name} - 类型: {field.FieldType.Name}");
                }
                
                // 包括基类字段
                var baseFields = architectureType.BaseType?.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (baseFields != null)
                {
                    // Debug.Log($"[ModelMonitor] 基类字段数量: {baseFields.Length}");
                    foreach (var field in baseFields)
                    {
                        // Debug.Log($"[ModelMonitor] 基类字段: {field.Name} - 类型: {field.FieldType.Name}");
                    }
                }
                
                // 尝试多种可能的容器字段名
                string[] possibleContainerNames = { "mContainer", "_container", "container", "Container" };
                foreach (var name in possibleContainerNames)
                {
                    _containerFieldCache = architectureType.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance) 
                                        ?? architectureType.BaseType?.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (_containerFieldCache != null)
                    {
                        // Debug.Log($"[ModelMonitor] 找到容器字段: {name}");
                        break;
                    }
                }
                
                // Debug.Log($"[ModelMonitor] 容器字段找到: {_containerFieldCache?.Name ?? "未找到"}");
                
                // 缓存实例字典字段
                _instancesFieldCache = typeof(QFramework.IOCContainer).GetField("mInstances", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                // Debug.Log($"[ModelMonitor] 实例字段找到: {_instancesFieldCache?.Name ?? "未找到"}");
                
                _reflectionCacheInitialized = true;
                // Debug.Log("[ModelMonitor] 反射缓存初始化完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ModelMonitor] 反射缓存初始化失败: {e.Message}");
                Debug.LogError($"[ModelMonitor] 堆栈: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 更新监控数据
        /// </summary>
        private void UpdateMonitoringData()
        {
#if UNITY_EDITOR
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                RefreshModelData();
            }
#endif
        }

        /// <summary>
        /// 收集Model信息
        /// </summary>
        private void CollectModelInfos()
        {
            if (!_reflectionCacheInitialized) 
            {
                Debug.LogWarning("[ModelMonitor] 反射缓存未初始化");
                return;
            }

            var architecture = GetArchitecture();
            if (architecture == null) 
            {
                Debug.LogWarning("[ModelMonitor] CollectModelInfos - 架构为null");
                return;
            }

            // Debug.Log("[ModelMonitor] 开始收集Model信息...");

            var container = _containerFieldCache?.GetValue(architecture) as QFramework.IOCContainer;
            if (container == null)
            {
                Debug.LogWarning($"[ModelMonitor] 无法获取IOC容器 - 容器字段: {_containerFieldCache?.Name}");
                return;
            }

            // Debug.Log("[ModelMonitor] IOC容器获取成功");

            var instances = _instancesFieldCache?.GetValue(container) as System.Collections.IDictionary;
            if (instances == null)
            {
                Debug.LogWarning($"[ModelMonitor] 无法获取实例字典 - 实例字段: {_instancesFieldCache?.Name}");
                return;
            }

            // Debug.Log($"[ModelMonitor] IOC容器中有 {instances.Count} 个实例");

            int modelCount = 0;
            foreach (System.Collections.DictionaryEntry entry in instances)
            {
                var instanceType = entry.Key as Type;
                var instance = entry.Value;
                
                // Debug.Log($"[ModelMonitor] 检查实例: {instanceType?.Name} - 是否为IModel: {(instanceType != null && typeof(IModel).IsAssignableFrom(instanceType))}");
                
                if (instanceType != null && instance != null && 
                    typeof(IModel).IsAssignableFrom(instanceType))
                {
                    modelCount++;
                    // Debug.Log($"[ModelMonitor] 找到Model: {instanceType.Name}");

                    // 性能优化：只监控指定的Model
                    if (!_monitorAllModels && !string.IsNullOrEmpty(_specificModelTypeName))
                    {
                        if (!instanceType.Name.Contains(_specificModelTypeName))
                        {
                            // Debug.Log($"[ModelMonitor] 跳过Model: {instanceType.Name} (不匹配过滤条件: {_specificModelTypeName})");
                            continue;
                        }
                    }

                    var model = instance as IModel;
                    var fields = GetModelFields(instanceType);
                    
                    var modelInfo = new ModelInfo
                    {
                        Name = instanceType.Name,
                        Type = instanceType,
                        FullTypeName = instanceType.FullName,
                        IsInitialized = model?.Initialized ?? false,
                        FieldCount = fields.Count,
                        Instance = instance
                    };
                    
                    _modelInfos.Add(modelInfo);
                    // Debug.Log($"[ModelMonitor] 添加Model: {modelInfo.Name}, 字段数: {modelInfo.FieldCount}, 已初始化: {modelInfo.IsInitialized}");
                }
            }

            // Debug.Log($"[ModelMonitor] 收集完成 - 总实例数: {instances.Count}, Model数: {modelCount}, 最终添加: {_modelInfos.Count}");
        }

        /// <summary>
        /// 获取Model实例
        /// </summary>
        private object GetModelInstance(Type modelType)
        {
            if (!_reflectionCacheInitialized) return null;

            var architecture = GetArchitecture();
            if (architecture == null) return null;

            if (_containerFieldCache?.GetValue(architecture) is QFramework.IOCContainer container)
            {
                if (_instancesFieldCache?.GetValue(container) is System.Collections.IDictionary instances)
                {
                    if (instances.Contains(modelType))
                        return instances[modelType];
                }
            }

            return null;
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Model信息
    /// </summary>
    [System.Serializable]
    public class ModelInfo
    {
        public string Name;
        public Type Type;
        public string FullTypeName;
        public bool IsInitialized;
        public int FieldCount;
        public object Instance;
    }

    /// <summary>
    /// 字段/属性信息
    /// </summary>
    [System.Serializable]
    public class FieldPropertyInfo
    {
        public string Name;
        public Type Type;
        public bool IsProperty;
        public bool CanRead;
        public bool CanWrite;
        public bool IsPublic;
    }

    #endregion
}