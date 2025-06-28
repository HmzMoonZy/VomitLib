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
    /// Event监控器 - 用于运行时监控和调试QFramework的Event系统
    /// Runtime组件，监控功能仅在Editor下生效
    /// </summary>
    [System.Serializable]
    public class EventMonitor : MonoBehaviour
    {
        #region Fields

        [Header("Event监控设置")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private float _refreshInterval = 0.5f;
        [SerializeField] private bool _autoRefresh = true;
        

        // Runtime数据（仅Editor可见）
        [System.NonSerialized] private List<EventInfo> _eventInfos = new List<EventInfo>();
        [System.NonSerialized] private Dictionary<Type, int> _eventListenerCounts = new Dictionary<Type, int>();
        [System.NonSerialized] private double _lastRefreshTime;

        // 反射缓存
        [System.NonSerialized] private FieldInfo _typeEventSystemFieldCache;
        [System.NonSerialized] private FieldInfo _eventsFieldCache;
        [System.NonSerialized] private FieldInfo _typeEventsFieldCache;
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
                RefreshEventData();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EventMonitor] 初始化监控失败: {e.Message}");
            }
        }

        /// <summary>
        /// 刷新Event数据
        /// </summary>
        public void RefreshEventData()
        {
            if (!_enableMonitoring)
            {
                Debug.LogWarning("[EventMonitor] 监控未启用");
                return;
            }
            
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[EventMonitor] 不在运行模式");
                return;
            }

            var architecture = GetArchitecture();
            if (architecture == null)
            {
                Debug.LogWarning("[EventMonitor] RefreshEventData - 架构为null");
                return;
            }

            Debug.Log("[EventMonitor] 开始刷新Event数据...");

            try
            {
                _eventInfos.Clear();
                _eventListenerCounts.Clear();
                CollectEventInfos();
                Debug.Log($"[EventMonitor] 刷新完成 - 找到 {_eventInfos.Count} 个事件类型");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EventMonitor] 刷新Event数据失败: {e.Message}");
                Debug.LogError($"[EventMonitor] 堆栈: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 手动触发事件（用于调试）
        /// </summary>
        public void TriggerEvent(Type eventType)
        {
            var architecture = GetArchitecture();
            if (architecture == null) return;

            try
            {
                // 创建事件实例（如果是struct，则使用默认构造函数）
                if (eventType.IsValueType)
                {
                    var eventInstance = Activator.CreateInstance(eventType);
                    var sendMethod = typeof(IArchitecture).GetMethod("SendEvent", new Type[] { eventType });
                    if (sendMethod != null)
                    {
                        sendMethod.Invoke(architecture, new object[] { eventInstance });
                        Debug.Log($"[EventMonitor] 手动触发事件: {eventType.Name}");
                    }
                    else
                    {
                        // 尝试无参版本
                        var sendMethodGeneric = typeof(IArchitecture).GetMethod("SendEvent", Type.EmptyTypes);
                        if (sendMethodGeneric != null)
                        {
                            var genericMethod = sendMethodGeneric.MakeGenericMethod(eventType);
                            genericMethod.Invoke(architecture, null);
                            Debug.Log($"[EventMonitor] 手动触发事件 (无参): {eventType.Name}");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EventMonitor] 触发事件失败: {e.Message}");
            }
        }


        #endregion

        #region Data Access Interface (for Editor)

#if UNITY_EDITOR

        /// <summary>
        /// 获取Event信息列表（仅Editor访问）
        /// </summary>
        public List<EventInfo> GetEventInfos() => _eventInfos ?? new List<EventInfo>();


        /// <summary>
        /// 获取Event监听器数量（仅Editor访问）
        /// </summary>
        public Dictionary<Type, int> GetEventListenerCounts() => _eventListenerCounts ?? new Dictionary<Type, int>();

        /// <summary>
        /// 清空缓存（仅Editor访问）
        /// </summary>
        public void ClearCache()
        {
            _eventListenerCounts.Clear();
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
        /// 初始化反射缓存
        /// </summary>
        private void InitializeReflectionCache()
        {
            if (_reflectionCacheInitialized) return;

            var architecture = GetArchitecture();
            if (architecture == null)
            {
                Debug.LogWarning("[EventMonitor] 获取架构失败 - Vomit.Interface返回null");
                return;
            }

            try
            {
                var architectureType = architecture.GetType();
                Debug.Log($"[EventMonitor] 架构类型: {architectureType.Name}");
                
                // 获取TypeEventSystem字段
                _typeEventSystemFieldCache = architectureType.GetField("mTypeEventSystem", BindingFlags.NonPublic | BindingFlags.Instance);
                if (_typeEventSystemFieldCache == null)
                {
                    // 尝试从基类查找
                    _typeEventSystemFieldCache = architectureType.BaseType?.GetField("mTypeEventSystem", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                
                // 获取EasyEvents的字段
                _eventsFieldCache = typeof(TypeEventSystem).GetField("mEvents", BindingFlags.NonPublic | BindingFlags.Instance);
                
                // 获取Dictionary<Type, IEasyEvent>字段
                _typeEventsFieldCache = typeof(EasyEvents).GetField("mTypeEvents", BindingFlags.NonPublic | BindingFlags.Instance);
                
                _reflectionCacheInitialized = true;
                Debug.Log($"[EventMonitor] 反射缓存初始化完成 - TypeEventSystem: {(_typeEventSystemFieldCache != null ? "✓" : "✗")}, EasyEvents: {(_eventsFieldCache != null ? "✓" : "✗")}, Dictionary: {(_typeEventsFieldCache != null ? "✓" : "✗")}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EventMonitor] 反射缓存初始化失败: {e.Message}");
                Debug.LogError($"[EventMonitor] 堆栈: {e.StackTrace}");
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
                RefreshEventData();
            }
#endif
        }

        /// <summary>
        /// 收集Event信息
        /// </summary>
        private void CollectEventInfos()
        {
            if (!_reflectionCacheInitialized) 
            {
                Debug.LogWarning("[EventMonitor] 反射缓存未初始化");
                return;
            }

            var architecture = GetArchitecture();
            if (architecture == null) 
            {
                Debug.LogWarning("[EventMonitor] CollectEventInfos - 架构为null");
                return;
            }

            Debug.Log("[EventMonitor] 开始收集Event信息...");

            var typeEventSystem = _typeEventSystemFieldCache?.GetValue(architecture) as TypeEventSystem;
            if (typeEventSystem == null)
            {
                Debug.LogWarning($"[EventMonitor] 无法获取TypeEventSystem - 字段缓存: {(_typeEventSystemFieldCache != null ? "有效" : "null")}");
                if (_typeEventSystemFieldCache != null)
                {
                    var fieldValue = _typeEventSystemFieldCache.GetValue(architecture);
                    Debug.LogWarning($"[EventMonitor] 字段值类型: {fieldValue?.GetType().Name ?? "null"}");
                }
                return;
            }

            var easyEvents = _eventsFieldCache?.GetValue(typeEventSystem) as EasyEvents;
            if (easyEvents == null)
            {
                Debug.LogWarning($"[EventMonitor] 无法获取EasyEvents");
                return;
            }

            var typeEvents = _typeEventsFieldCache?.GetValue(easyEvents) as System.Collections.IDictionary;
            if (typeEvents == null)
            {
                Debug.LogWarning($"[EventMonitor] 无法获取typeEvents字典");
                return;
            }

            Debug.Log($"[EventMonitor] 事件系统中有 {typeEvents.Count} 个事件类型");

            foreach (System.Collections.DictionaryEntry entry in typeEvents)
            {
                var easyEventType = entry.Key as Type; // 这是EasyEvent<T>类型
                var easyEvent = entry.Value;
                
                if (easyEventType != null && easyEvent != null)
                {
                    // 从EasyEvent<T>中提取实际的事件类型T
                    Type actualEventType = null;
                    if (easyEventType.IsGenericType)
                    {
                        var genericTypeDef = easyEventType.GetGenericTypeDefinition();
                        // 检查是否为EasyEvent<T>类型（通过名称匹配，避免类型引用问题）
                        if (genericTypeDef.Name == "EasyEvent`1")
                        {
                            var genericArgs = easyEventType.GetGenericArguments();
                            if (genericArgs.Length > 0)
                            {
                                actualEventType = genericArgs[0]; // 这是T，实际的事件类型
                            }
                        }
                    }
                    
                    if (actualEventType != null)
                    {
                        // 尝试获取监听器数量
                        int listenerCount = GetListenerCount(easyEvent);
                        _eventListenerCounts[actualEventType] = listenerCount;
                        
                        var eventInfo = new EventInfo
                        {
                            EventType = actualEventType,
                            EventTypeName = actualEventType.FullName,
                            FullTypeName = actualEventType.AssemblyQualifiedName,
                            ListenerCount = listenerCount,
                            IsStruct = actualEventType.IsValueType,
                            AssemblyName = actualEventType.Assembly.GetName().Name
                        };
                        
                        _eventInfos.Add(eventInfo);
                        Debug.Log($"[EventMonitor] 添加事件: {eventInfo.EventTypeName}, 监听器数: {eventInfo.ListenerCount}");
                    }
                    else
                    {
                        Debug.LogWarning($"[EventMonitor] 无法从类型 {easyEventType.Name} 中提取事件类型");
                    }
                }
            }

            Debug.Log($"[EventMonitor] 收集完成 - 事件类型数: {typeEvents.Count}, 最终添加: {_eventInfos.Count}");
        }

        /// <summary>
        /// 获取EasyEvent的监听器数量
        /// </summary>
        private int GetListenerCount(object easyEvent)
        {
            try
            {
                // 通过反射获取Action的调用列表长度
                var easyEventType = easyEvent.GetType();
                var onEventField = easyEventType.GetField("mOnEvent", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (onEventField != null)
                {
                    var action = onEventField.GetValue(easyEvent) as Delegate;
                    return action?.GetInvocationList()?.Length ?? 0;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EventMonitor] 获取监听器数量失败: {e.Message}");
            }
            
            return 0;
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Event信息
    /// </summary>
    [System.Serializable]
    public class EventInfo
    {
        public Type EventType;
        public string EventTypeName;
        public string FullTypeName;
        public int ListenerCount;
        public bool IsStruct;
        public string AssemblyName;
    }


    #endregion
}