using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QFramework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// 监控数据提供者 - 负责收集和缓存所有监控数据
    /// </summary>
    public class MonitorDataProvider : IDisposable
    {
        #region Fields

        // 缓存的数据
        private List<ViewInfo> _viewInfos = new List<ViewInfo>();
        private List<ModelInfo> _modelInfos = new List<ModelInfo>();
        private List<SystemInfo> _systemInfos = new List<SystemInfo>();
        private List<EventInfo> _eventInfos = new List<EventInfo>();
        private List<CommandInfo> _commandInfos = new List<CommandInfo>();
        private List<ProcedureInfo> _procedureInfos = new List<ProcedureInfo>();
        private List<LubanTableInfo> _lubanTableInfos = new List<LubanTableInfo>();

        // 执行结果历史
        private Dictionary<Type, CommandExecutionResult> _commandExecutionResults = new Dictionary<Type, CommandExecutionResult>();
        private List<ProcedureTransition> _procedureHistory = new List<ProcedureTransition>();

        public Dictionary<Type, CommandExecutionResult> CommandExecutionResults => _commandExecutionResults;

        // 反射缓存
        private FieldInfo _containerFieldCache;
        private FieldInfo _instancesFieldCache;
        private FieldInfo _typeEventSystemFieldCache;
        private FieldInfo _eventsFieldCache;
        private FieldInfo _typeEventsFieldCache;
        private bool _reflectionCacheInitialized = false;

        // 配置
        private bool _includeSystemAssemblies = false;
        private bool _includeUnityAssemblies = false;

        #endregion

        #region Properties

        public bool IsInitialized => _reflectionCacheInitialized;
        public List<ViewInfo> ViewInfos => _viewInfos;
        public List<ModelInfo> ModelInfos => _modelInfos;
        public List<SystemInfo> SystemInfos => _systemInfos;
        public List<EventInfo> EventInfos => _eventInfos;
        public List<CommandInfo> CommandInfos => _commandInfos;
        public List<ProcedureInfo> ProcedureInfos => _procedureInfos;
        public List<LubanTableInfo> LubanTableInfos => _lubanTableInfos;

        #endregion

        #region Public Methods

        /// <summary>
        /// 刷新所有数据
        /// </summary>
        public void RefreshAll()
        {
            if (!Vomit.IsInit)
            {
                Debug.LogWarning("[MonitorDataProvider] 框架未初始化，无法刷新数据");
                return;
            }

            InitializeReflectionCache();

            RefreshViewData();
            RefreshModelData();
            RefreshSystemData();
            RefreshEventData();
            RefreshCommandData();
            RefreshProcedureData();
            RefreshLubanData();
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void ClearAllCache()
        {
            _viewInfos.Clear();
            _modelInfos.Clear();
            _systemInfos.Clear();
            _eventInfos.Clear();
            _commandInfos.Clear();
            _procedureInfos.Clear();
            _lubanTableInfos.Clear();
            _commandExecutionResults.Clear();
            _procedureHistory.Clear();
        }

        /// <summary>
        /// 获取概览统计信息
        /// </summary>
        public OverviewStatistics GetOverviewStatistics()
        {
            return new OverviewStatistics
            {
                ViewCount = _viewInfos.Count,
                VisibleViewCount = _viewInfos.Count(v => v.IsVisible),
                ModelCount = _modelInfos.Count,
                SystemCount = _systemInfos.Count,
                EventCount = _eventInfos.Count,
                CommandCount = _commandInfos.Count,
                ProcedureCount = _procedureInfos.Count,
                LubanTableCount = _lubanTableInfos.Count
            };
        }

        /// <summary>
        /// 执行Command
        /// </summary>
        public CommandExecutionResult ExecuteCommand(Type commandType)
        {
            if (!Vomit.IsInit) return null;

            try
            {
                var architecture = Vomit.Interface;
                var commandInstance = Activator.CreateInstance(commandType);

                var startTime = DateTime.Now;
                object result = null;
                Exception exception = null;

                try
                {
                    // 检查是否有返回值
                    var hasReturnValue = commandType.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

                    if (hasReturnValue)
                    {
                        // 获取返回值类型
                        var returnType = commandType.GetInterfaces()
                            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>))
                            .GetGenericArguments()[0];

                        // 使用泛型SendCommand方法
                        var sendMethod = typeof(IArchitecture).GetMethods()
                            .FirstOrDefault(m => m.Name == "SendCommand" &&
                                          m.IsGenericMethod &&
                                          m.GetParameters().Length == 1 &&
                                          m.GetParameters()[0].ParameterType.IsGenericType &&
                                          m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(ICommand<>));

                        if (sendMethod != null)
                        {
                            var constructedMethod = sendMethod.MakeGenericMethod(returnType);
                            result = constructedMethod.Invoke(architecture, new object[] { commandInstance });
                        }
                    }
                    else
                    {
                        // 无返回值的Command
                        var sendMethod = typeof(IArchitecture).GetMethod("SendCommand", new Type[] { commandType });
                        sendMethod?.Invoke(architecture, new object[] { commandInstance });
                    }
                }
                catch (TargetInvocationException tie)
                {
                    exception = tie.InnerException;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                var executionResult = new CommandExecutionResult
                {
                    CommandType = commandType,
                    ExecutionTime = DateTime.Now - startTime,
                    Success = exception == null,
                    Result = result,
                    Exception = exception,
                    Timestamp = startTime
                };

                _commandExecutionResults[commandType] = executionResult;
                return executionResult;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonitorDataProvider] 执行Command失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取Model字段值
        /// </summary>
        public object GetModelFieldValue(Type modelType, string fieldName)
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

        /// <summary>
        /// 设置Model字段值
        /// </summary>
        public bool SetModelFieldValue(Type modelType, string fieldName, object value)
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

        /// <summary>
        /// 触发Event
        /// </summary>
        public void TriggerEvent(Type eventType)
        {
            if (!Vomit.IsInit) return;

            try
            {
                var architecture = Vomit.Interface;

                if (eventType.IsValueType)
                {
                    var eventInstance = Activator.CreateInstance(eventType);
                    var sendMethod = typeof(IArchitecture).GetMethod("SendEvent", new Type[] { eventType });
                    sendMethod?.Invoke(architecture, new object[] { eventInstance });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonitorDataProvider] 触发Event失败: {e.Message}");
            }
        }

        public void Dispose()
        {
            ClearAllCache();
        }

        #endregion

        #region Private Methods - Data Refresh

        private void RefreshViewData()
        {
            _viewInfos.Clear();

            // 获取View静态类的类型
            var viewType = Type.GetType("Twenty2.VomitLib.View.View, Twenty2.VomitLib");
            if (viewType == null)
            {
                Debug.LogWarning("[MonitorDataProvider] 无法找到View类型");
                return;
            }

            // 获取_visibleViewMap字段
            var visibleViewMapField = viewType.GetField("_visibleViewMap", BindingFlags.NonPublic | BindingFlags.Static);
            var visibleViews = visibleViewMapField?.GetValue(null) as System.Collections.IDictionary;

            // 获取_hiddenViewMap字段
            var hiddenViewMapField = viewType.GetField("_hiddenViewMap", BindingFlags.NonPublic | BindingFlags.Static);
            var hiddenViews = hiddenViewMapField?.GetValue(null) as System.Collections.IDictionary;

            // 获取_preLoadMap字段
            var preloadMapField = viewType.GetField("_preLoadMap", BindingFlags.NonPublic | BindingFlags.Static);
            var preloadViews = preloadMapField?.GetValue(null) as System.Collections.IDictionary;

            // 收集可见的Views
            if (visibleViews != null)
            {
                foreach (System.Collections.DictionaryEntry entry in visibleViews)
                {
                    var viewKey = entry.Key.ToString();
                    var viewLogic = entry.Value;

                    if (viewLogic != null)
                    {
                        // 通过反射获取ViewLogic的属性
                        var viewLogicType = viewLogic.GetType();
                        var gameObjectProperty = viewLogicType.GetProperty("gameObject");
                        var sortOrderProperty = viewLogicType.GetProperty("SortOrder");

                        var gameObject = gameObjectProperty?.GetValue(viewLogic) as GameObject;
                        var sortOrder = (int)(sortOrderProperty?.GetValue(viewLogic) ?? 0);

                        if (gameObject != null)
                        {
                            _viewInfos.Add(new ViewInfo
                            {
                                Name = viewKey,
                                GameObject = gameObject,
                                IsVisible = true,
                                IsHidden = false,
                                IsCache = false,
                                IsPreloaded = false,
                                SortOrder = sortOrder
                            });
                        }
                    }
                }
            }

            // 收集隐藏的Views
            if (hiddenViews != null)
            {
                foreach (System.Collections.DictionaryEntry entry in hiddenViews)
                {
                    var viewKey = entry.Key.ToString();
                    var viewLogic = entry.Value;

                    if (viewLogic != null)
                    {
                        var viewLogicType = viewLogic.GetType();
                        var gameObjectProperty = viewLogicType.GetProperty("gameObject");
                        var sortOrderProperty = viewLogicType.GetProperty("SortOrder");

                        var gameObject = gameObjectProperty?.GetValue(viewLogic) as GameObject;
                        var sortOrder = (int)(sortOrderProperty?.GetValue(viewLogic) ?? 0);

                        if (gameObject != null && !_viewInfos.Any(v => v.Name == viewKey))
                        {
                            _viewInfos.Add(new ViewInfo
                            {
                                Name = viewKey,
                                GameObject = gameObject,
                                IsVisible = false,
                                IsHidden = true,
                                IsCache = true,
                                IsPreloaded = false,
                                SortOrder = sortOrder
                            });
                        }
                    }
                }
            }

            // 收集预加载的Views
            if (preloadViews != null)
            {
                foreach (System.Collections.DictionaryEntry entry in preloadViews)
                {
                    var viewKey = entry.Key.ToString();
                    var gameObject = entry.Value as GameObject;

                    if (gameObject != null && !_viewInfos.Any(v => v.Name == viewKey))
                    {
                        _viewInfos.Add(new ViewInfo
                        {
                            Name = viewKey,
                            GameObject = gameObject,
                            IsVisible = gameObject.activeInHierarchy,
                            IsHidden = !gameObject.activeInHierarchy,
                            IsCache = false,
                            IsPreloaded = true,
                            SortOrder = 0
                        });
                    }
                }
            }

            Debug.Log($"[MonitorDataProvider] View数据收集完成，共找到 {_viewInfos.Count} 个View");
        }

        private void RefreshModelData()
        {
            _modelInfos.Clear();
            if (!_reflectionCacheInitialized)
            {
                Debug.LogWarning("[MonitorDataProvider] Model数据收集失败：反射缓存未初始化");
                return;
            }

            var container = _containerFieldCache?.GetValue(Vomit.Interface) as QFramework.IOCContainer;
            if (container == null)
            {
                Debug.LogWarning("[MonitorDataProvider] Model数据收集失败：无法获取IOCContainer");
                return;
            }

            var instances = _instancesFieldCache?.GetValue(container) as System.Collections.IDictionary;
            if (instances == null)
            {
                Debug.LogWarning("[MonitorDataProvider] Model数据收集失败：无法获取实例字典");
                return;
            }

            foreach (System.Collections.DictionaryEntry entry in instances)
            {
                var instanceType = entry.Key as Type;
                if (instanceType != null && typeof(IModel).IsAssignableFrom(instanceType))
                {
                    var model = entry.Value as IModel;
                    _modelInfos.Add(new ModelInfo
                    {
                        Name = instanceType.Name,
                        Type = instanceType,
                        FullTypeName = instanceType.FullName,
                        IsInitialized = model?.Initialized ?? false,
                        Instance = entry.Value
                    });
                }
            }

            Debug.Log($"[MonitorDataProvider] Model数据收集完成，共找到 {_modelInfos.Count} 个Model");
        }

        private void RefreshSystemData()
        {
            _systemInfos.Clear();
            if (!_reflectionCacheInitialized)
            {
                Debug.LogWarning("[MonitorDataProvider] System数据收集失败：反射缓存未初始化");
                return;
            }

            var container = _containerFieldCache?.GetValue(Vomit.Interface) as QFramework.IOCContainer;
            if (container == null)
            {
                Debug.LogWarning("[MonitorDataProvider] System数据收集失败：无法获取IOCContainer");
                return;
            }

            var instances = _instancesFieldCache?.GetValue(container) as System.Collections.IDictionary;
            if (instances == null)
            {
                Debug.LogWarning("[MonitorDataProvider] System数据收集失败：无法获取实例字典");
                return;
            }

            foreach (System.Collections.DictionaryEntry entry in instances)
            {
                var instanceType = entry.Key as Type;
                if (instanceType != null && typeof(ISystem).IsAssignableFrom(instanceType))
                {
                    var system = entry.Value as ISystem;
                    _systemInfos.Add(new SystemInfo
                    {
                        Name = instanceType.Name,
                        Type = instanceType,
                        FullTypeName = instanceType.FullName,
                        IsInitialized = system?.Initialized ?? false,
                        Instance = entry.Value
                    });
                }
            }

            Debug.Log($"[MonitorDataProvider] System数据收集完成，共找到 {_systemInfos.Count} 个System");
        }

        private void RefreshEventData()
        {
            _eventInfos.Clear();
            if (!_reflectionCacheInitialized)
            {
                Debug.LogWarning("[MonitorDataProvider] Event数据收集失败：反射缓存未初始化");
                return;
            }

            var typeEventSystem = _typeEventSystemFieldCache?.GetValue(Vomit.Interface) as TypeEventSystem;
            if (typeEventSystem == null)
            {
                Debug.LogWarning("[MonitorDataProvider] Event数据收集失败：无法获取TypeEventSystem");
                return;
            }

            var easyEvents = _eventsFieldCache?.GetValue(typeEventSystem) as EasyEvents;
            if (easyEvents == null)
            {
                Debug.LogWarning("[MonitorDataProvider] Event数据收集失败：无法获取EasyEvents");
                return;
            }

            var typeEvents = _typeEventsFieldCache?.GetValue(easyEvents) as System.Collections.IDictionary;
            if (typeEvents == null)
            {
                Debug.LogWarning("[MonitorDataProvider] Event数据收集失败：无法获取事件类型字典");
                return;
            }

            foreach (System.Collections.DictionaryEntry entry in typeEvents)
            {
                var easyEventType = entry.Key as Type;
                if (easyEventType != null && easyEventType.IsGenericType)
                {
                    var genericTypeDef = easyEventType.GetGenericTypeDefinition();
                    if (genericTypeDef.Name == "EasyEvent`1")
                    {
                        var actualEventType = easyEventType.GetGenericArguments()[0];
                        var listenerCount = GetListenerCount(entry.Value);

                        _eventInfos.Add(new EventInfo
                        {
                            EventType = actualEventType,
                            EventTypeName = actualEventType.FullName,
                            FullTypeName = actualEventType.AssemblyQualifiedName,
                            ListenerCount = listenerCount,
                            IsStruct = actualEventType.IsValueType,
                            AssemblyName = actualEventType.Assembly.GetName().Name
                        });
                    }
                }
            }

            Debug.Log($"[MonitorDataProvider] Event数据收集完成，共找到 {_eventInfos.Count} 个Event");
        }

        private void RefreshCommandData()
        {
            _commandInfos.Clear();

            var assemblies = GetTargetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    var commandTypes = types.Where(IsCommandType);

                    foreach (var commandType in commandTypes)
                    {
                        var commandInfo = CreateCommandInfo(commandType);
                        if (commandInfo != null)
                        {
                            _commandInfos.Add(commandInfo);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[MonitorDataProvider] 扫描程序集 {assembly.GetName().Name} 失败: {e.Message}");
                }
            }

            Debug.Log($"[MonitorDataProvider] Command数据收集完成，共找到 {_commandInfos.Count} 个Command");
        }

        private void RefreshProcedureData()
        {
            _procedureInfos.Clear();

            // 获取ProcedureMgr
            var gameType = Type.GetType("Game, Assembly-CSharp");
            if (gameType == null)
            {
                Debug.LogWarning("[MonitorDataProvider] Procedure数据收集失败：无法找到Game类型");
                return;
            }

            var procedureProperty = gameType.GetProperty("Procedure", BindingFlags.Public | BindingFlags.Static);
            var procedureMgr = procedureProperty?.GetValue(null);

            if (procedureMgr != null)
            {
                // 获取当前状态
                var currentStateProperty = procedureMgr.GetType().GetProperty("CurrentState");
                var currentState = currentStateProperty?.GetValue(procedureMgr)?.ToString();

                _procedureInfos.Add(new ProcedureInfo
                {
                    Name = "Current Procedure",
                    StateName = currentState ?? "Unknown",
                    IsActive = true
                });

                Debug.Log($"[MonitorDataProvider] Procedure数据收集完成，当前状态: {currentState}");
            }
            else
            {
                Debug.LogWarning("[MonitorDataProvider] Procedure数据收集失败：无法获取ProcedureMgr实例");
            }
        }

        private void RefreshLubanData()
        {
            _lubanTableInfos.Clear();

            // 获取Game.DB
            var gameType = Type.GetType("Game, Assembly-CSharp");
            if (gameType == null)
            {
                Debug.LogWarning("[MonitorDataProvider] Luban数据收集失败：无法找到Game类型");
                return;
            }

            var dbProperty = gameType.GetProperty("DB", BindingFlags.Public | BindingFlags.Static);
            var dbInstance = dbProperty?.GetValue(null);

            if (dbInstance != null)
            {
                var dbType = dbInstance.GetType();
                var properties = dbType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    if (!property.Name.StartsWith("Tb")) continue;

                    try
                    {
                        var tableInstance = property.GetValue(dbInstance);
                        if (tableInstance != null)
                        {
                            var tableType = property.PropertyType;

                            int dataCount = 0;
                            var dataListProperty = tableType.GetProperty("DataList");
                            if (dataListProperty != null)
                            {
                                var dataList = dataListProperty.GetValue(tableInstance) as System.Collections.IList;
                                dataCount = dataList?.Count ?? 0;
                            }

                            _lubanTableInfos.Add(new LubanTableInfo
                            {
                                TableName = property.Name,
                                TableType = tableType,
                                TableTypeName = tableType.Name,
                                DataCount = dataCount
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[MonitorDataProvider] 获取数据表失败: {property.Name}, 错误: {e.Message}");
                    }
                }

                Debug.Log($"[MonitorDataProvider] Luban数据收集完成，共找到 {_lubanTableInfos.Count} 个数据表");
            }
            else
            {
                Debug.LogWarning("[MonitorDataProvider] Luban数据收集失败：无法获取Game.DB实例");
            }
        }

        #endregion

        #region Private Methods - Reflection

        private void InitializeReflectionCache()
        {
            if (_reflectionCacheInitialized) return;

            if (!Vomit.IsInit) return;

            try
            {
                var architecture = Vomit.Interface;
                var architectureType = architecture.GetType();

                // 缓存容器字段
                string[] possibleContainerNames = { "mContainer", "_container", "container", "Container" };
                foreach (var name in possibleContainerNames)
                {
                    _containerFieldCache = architectureType.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                                        ?? architectureType.BaseType?.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (_containerFieldCache != null) break;
                }

                // 缓存实例字典字段
                _instancesFieldCache = typeof(QFramework.IOCContainer).GetField("mInstances",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                // 缓存TypeEventSystem字段
                _typeEventSystemFieldCache = architectureType.GetField("mTypeEventSystem", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? architectureType.BaseType?.GetField("mTypeEventSystem", BindingFlags.NonPublic | BindingFlags.Instance);

                // 缓存EasyEvents的字段
                _eventsFieldCache = typeof(TypeEventSystem).GetField("mEvents", BindingFlags.NonPublic | BindingFlags.Instance);

                // 缓存Dictionary<Type, IEasyEvent>字段
                _typeEventsFieldCache = typeof(EasyEvents).GetField("mTypeEvents", BindingFlags.NonPublic | BindingFlags.Instance);

                _reflectionCacheInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonitorDataProvider] 初始化反射缓存失败: {e.Message}");
            }
        }

        private object GetModelInstance(Type modelType)
        {
            if (!_reflectionCacheInitialized) return null;

            if (_containerFieldCache?.GetValue(Vomit.Interface) is QFramework.IOCContainer container)
            {
                if (_instancesFieldCache?.GetValue(container) is System.Collections.IDictionary instances)
                {
                    if (instances.Contains(modelType))
                        return instances[modelType];
                }
            }

            return null;
        }

        private int GetListenerCount(object easyEvent)
        {
            try
            {
                var easyEventType = easyEvent.GetType();
                var onEventField = easyEventType.GetField("mOnEvent", BindingFlags.NonPublic | BindingFlags.Instance);

                if (onEventField != null)
                {
                    var action = onEventField.GetValue(easyEvent) as Delegate;
                    return action?.GetInvocationList()?.Length ?? 0;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MonitorDataProvider] 获取监听器数量失败: {e.Message}");
            }

            return 0;
        }

        private bool IsCommandType(Type type)
        {
            if (type.IsAbstract || type.IsInterface) return false;

            return typeof(ICommand).IsAssignableFrom(type) ||
                   type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
        }

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
                    CanInstantiate = CanInstantiateType(commandType),
                    IsNestedInInterface = commandType.IsNested && commandType.DeclaringType?.IsInterface == true
                };

                // 检查是否有返回值
                var genericCommandInterface = commandType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

                if (genericCommandInterface != null)
                {
                    commandInfo.HasReturnValue = true;
                    commandInfo.ReturnType = genericCommandInterface.GetGenericArguments()[0];
                }

                return commandInfo;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MonitorDataProvider] 创建CommandInfo失败: {commandType.Name}, 错误: {e.Message}");
                return null;
            }
        }

        private bool CanInstantiateType(Type type)
        {
            var constructors = type.GetConstructors();
            return constructors.Any(c => c.GetParameters().Length == 0);
        }

        private Assembly[] GetTargetAssemblies()
        {
            var assemblies = new List<Assembly>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyName = assembly.GetName().Name;

                if (!_includeUnityAssemblies && IsUnityAssembly(assemblyName))
                    continue;

                if (!_includeSystemAssemblies && IsSystemAssembly(assemblyName))
                    continue;

                assemblies.Add(assembly);
            }

            return assemblies.ToArray();
        }

        private bool IsUnityAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("Unity") ||
                   assemblyName.StartsWith("UnityEngine") ||
                   assemblyName.StartsWith("UnityEditor");
        }

        private bool IsSystemAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("System") ||
                   assemblyName.StartsWith("mscorlib") ||
                   assemblyName.StartsWith("netstandard") ||
                   assemblyName.StartsWith("Microsoft");
        }

        #endregion
    }
}
