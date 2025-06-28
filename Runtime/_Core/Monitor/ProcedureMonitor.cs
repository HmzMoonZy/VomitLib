using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Twenty2.VomitLib.Procedure;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Twenty2.VomitLib.Monitor
{
    /// <summary>
    /// Procedure监控器 - 用于运行时监控和调试VomitLib的Procedure系统
    /// Runtime组件，监控功能仅在Editor下生效
    /// </summary>
    [System.Serializable]
    public class ProcedureMonitor : MonoBehaviour
    {
        #region Fields

        [Header("Procedure监控设置")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private float _refreshInterval = 1.0f;
        [SerializeField] private bool _autoRefresh = true;

        [Header("状态机配置")]
        [SerializeField] private string _stateEnumTypeName = "Game+State";

        // Runtime数据（仅Editor可见）
        [System.NonSerialized] private ProcedureStateMachineInfo _stateMachineInfo;
        [System.NonSerialized] private List<ProcedureStateInfo> _stateInfos = new List<ProcedureStateInfo>();
        [System.NonSerialized] private ProcedureRuntimeInfo _runtimeInfo;
        [System.NonSerialized] private double _lastRefreshTime;

        // 反射缓存
        [System.NonSerialized] private bool _cacheInitialized = false;

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
            set => _refreshInterval = Mathf.Clamp(value, 0.1f, 10f);
        }

        public bool AutoRefresh
        {
            get => _autoRefresh;
            set => _autoRefresh = value;
        }

        public string StateEnumTypeName
        {
            get => _stateEnumTypeName;
            set => _stateEnumTypeName = value;
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
        /// 初始化监控
        /// </summary>
        public void InitializeMonitoring()
        {
            if (!_enableMonitoring) return;

            try
            {
                RefreshProcedureData();
            }
            catch (System.Exception e)
            {
                Log.Warning($"[ProcedureMonitor] 初始化监控失败: {e.Message}");
            }
        }

        /// <summary>
        /// 刷新Procedure数据
        /// </summary>
        public void RefreshProcedureData()
        {
            if (!_enableMonitoring)
            {
                Log.Warning("[ProcedureMonitor] 监控未启用");
                return;
            }

            Log.Debug("[ProcedureMonitor] 开始刷新Procedure数据...");

            try
            {
                _stateMachineInfo = null;
                _stateInfos.Clear();
                _runtimeInfo = null;

                InitializeStateMachine();

                _cacheInitialized = true;
                Log.Debug($"[ProcedureMonitor] 刷新完成 - 状态机: {_stateMachineInfo?.StateMachineTypeName ?? "未找到"}");
            }
            catch (System.Exception e)
            {
                Log.Error($"[ProcedureMonitor] 刷新Procedure数据失败: {e.Message}");
                Log.Error($"[ProcedureMonitor] 堆栈: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 手动切换状态（用于调试）
        /// </summary>
        public void ChangeState(object targetStateId)
        {
            if (_stateMachineInfo?.StateMachineInstance == null)
            {
                Log.Error("[ProcedureMonitor] 状态机实例未初始化");
                return;
            }

            try
            {
                var stateMachine = _stateMachineInfo.StateMachineInstance;
                var stateMachineType = stateMachine.GetType();
                var changeStateMethod = stateMachineType.GetMethod("ChangeState");
                
                if (changeStateMethod != null)
                {
                    changeStateMethod.Invoke(stateMachine, new object[] { targetStateId });
                    Log.Info($"[ProcedureMonitor] 手动切换状态: {targetStateId}");
                }
                else
                {
                    Log.Error($"[ProcedureMonitor] 找不到ChangeState方法: {stateMachineType.Name}");
                }

#if UNITY_EDITOR
                EditorApplication.delayCall += () => Repaint();
#endif
            }
            catch (System.Exception e)
            {
                Log.Error($"[ProcedureMonitor] 切换状态失败: {e.Message}");
            }
        }

        /// <summary>
        /// 强制切换状态（用于调试）
        /// </summary>
        public void ForceChangeState(object targetStateId)
        {
            if (_stateMachineInfo?.StateMachineInstance == null)
            {
                Log.Error("[ProcedureMonitor] 状态机实例未初始化");
                return;
            }

            try
            {
                var stateMachine = _stateMachineInfo.StateMachineInstance;
                var stateMachineType = stateMachine.GetType();
                var forceChangeStateMethod = stateMachineType.GetMethod("ForceChangeState");
                
                if (forceChangeStateMethod != null)
                {
                    forceChangeStateMethod.Invoke(stateMachine, new object[] { targetStateId });
                    Log.Info($"[ProcedureMonitor] 强制切换状态: {targetStateId}");
                }
                else
                {
                    Log.Error($"[ProcedureMonitor] 找不到ForceChangeState方法: {stateMachineType.Name}");
                }

#if UNITY_EDITOR
                EditorApplication.delayCall += () => Repaint();
#endif
            }
            catch (System.Exception e)
            {
                Log.Error($"[ProcedureMonitor] 强制切换状态失败: {e.Message}");
            }
        }

        #endregion

        #region Data Access Interface (for Editor)

#if UNITY_EDITOR

        /// <summary>
        /// 获取状态机信息（仅Editor访问）
        /// </summary>
        public ProcedureStateMachineInfo GetStateMachineInfo() => _stateMachineInfo;

        /// <summary>
        /// 获取状态信息列表（仅Editor访问）
        /// </summary>
        public List<ProcedureStateInfo> GetStateInfos() => _stateInfos ?? new List<ProcedureStateInfo>();

        /// <summary>
        /// 获取运行时信息（仅Editor访问）
        /// </summary>
        public ProcedureRuntimeInfo GetRuntimeInfo() => _runtimeInfo;

        /// <summary>
        /// 清空缓存（仅Editor访问）
        /// </summary>
        public void ClearCache()
        {
            _stateMachineInfo = null;
            _stateInfos.Clear();
            _runtimeInfo = null;
            _cacheInitialized = false;
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
                RefreshRuntimeData();
            }
#endif
        }

        /// <summary>
        /// 刷新运行时数据
        /// </summary>
        private void RefreshRuntimeData()
        {
            try
            {
                if (_stateMachineInfo != null)
                {
                    UpdateStateMachineRuntimeInfo();
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[ProcedureMonitor] 刷新运行时数据失败: {e.Message}");
            }
        }

        /// <summary>
        /// 更新状态机运行时信息
        /// </summary>
        private void UpdateStateMachineRuntimeInfo()
        {
            try
            {
                var stateMachine = _stateMachineInfo.StateMachineInstance;
                if (_runtimeInfo == null)
                {
                    _runtimeInfo = new ProcedureRuntimeInfo();
                }

                // 更新运行时信息
                _runtimeInfo.IsRunning = GetPropertyValue<bool>(stateMachine, "IsRunning");
                _runtimeInfo.IsInitialized = GetPropertyValue<bool>(stateMachine, "IsInitialized");
                _runtimeInfo.CurrentStateId = GetPropertyValue<object>(stateMachine, "CurrentStateId");
                _runtimeInfo.PreviousStateId = GetPropertyValue<object>(stateMachine, "PreviousStateId");
                _runtimeInfo.CurrentState = GetPropertyValue<IProcedureState>(stateMachine, "CurrentState");
                _runtimeInfo.FrameCount = GetPropertyValue<long>(stateMachine, "FrameCount");
                _runtimeInfo.StateTime = GetPropertyValue<float>(stateMachine, "StateTime");
                _runtimeInfo.LastUpdateTime = DateTime.Now;
            }
            catch (System.Exception e)
            {
                Log.Error($"[ProcedureMonitor] 更新状态机运行时信息失败: {e.Message}");
            }
        }


        /// <summary>
        /// 初始化状态机
        /// </summary>
        private void InitializeStateMachine()
        {
            Log.Debug("[ProcedureMonitor] 开始初始化状态机...");

            try
            {
                // 自动查找ProcedureStateMachine实例
                var stateMachineInstance = FindProcedureStateMachineInstance();
                if (stateMachineInstance != null)
                {
                    _stateMachineInfo = CreateStateMachineInfoFromInstance(stateMachineInstance);
                    if (_stateMachineInfo != null)
                    {
                        ScanStatesForStateMachine();
                        Log.Info($"[ProcedureMonitor] 成功初始化状态机: {_stateMachineInfo.StateMachineTypeName}");
                    }
                }
                else
                {
                    Log.Warning($"[ProcedureMonitor] 未找到状态机实例");
                }
            }
            catch (Exception e)
            {
                Log.Error($"[ProcedureMonitor] 初始化状态机失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 查找ProcedureStateMachine实例
        /// </summary>
        private object FindProcedureStateMachineInstance()
        {
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        
                        // 查找包含ProcedureStateMachine<T>字段的类型
                        foreach (var type in types)
                        {
                            var stateMachineFields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                                .Where(f => f.FieldType.IsGenericType && 
                                          f.FieldType.GetGenericTypeDefinition() == typeof(ProcedureStateMachine<>))
                                .ToList();

                            foreach (var field in stateMachineFields)
                            {
                                try
                                {
                                    var stateMachineInstance = field.GetValue(null);
                                    if (stateMachineInstance != null)
                                    {
                                        Log.Info($"[ProcedureMonitor] 找到状态机: {type.Name}.{field.Name}");
                                        return stateMachineInstance;
                                    }
                                }
                                catch (Exception e)
                                {
                                    Log.Warning($"[ProcedureMonitor] 获取状态机实例失败: {type.Name}.{field.Name}, 错误: {e.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // 忽略无法访问的程序集
                        continue;
                    }
                }

                Log.Warning("[ProcedureMonitor] 未找到ProcedureStateMachine实例");
                return null;
            }
            catch (Exception e)
            {
                Log.Error($"[ProcedureMonitor] 查找状态机实例失败: {e.Message}");
                return null;
            }
        }
        

        /// <summary>
        /// 扫描状态机的状态
        /// </summary>
        private void ScanStatesForStateMachine()
        {
            try
            {
                _stateInfos.Clear();
                
                Type stateEnumType = null;
                
                // 尝试从状态机信息中获取
                if (_stateMachineInfo?.StateEnumType != null)
                {
                    stateEnumType = _stateMachineInfo.StateEnumType;
                }
                else
                {
                    // 尝试根据用户输入的类型名查找
                    stateEnumType = FindEnumType(_stateEnumTypeName);
                }

                if (stateEnumType != null && stateEnumType.IsEnum)
                {
                    var enumValues = Enum.GetValues(stateEnumType);
                    foreach (var enumValue in enumValues)
                    {
                        var stateInfo = new ProcedureStateInfo
                        {
                            StateId = enumValue,
                            StateName = enumValue.ToString(),
                            StateEnumType = stateEnumType
                        };
                        _stateInfos.Add(stateInfo);
                    }
                    
                    Log.Debug($"[ProcedureMonitor] 扫描到 {_stateInfos.Count} 个状态");
                }
                else
                {
                    Log.Warning($"[ProcedureMonitor] 未找到状态枚举类型: {_stateEnumTypeName}");
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[ProcedureMonitor] 扫描状态失败: {e.Message}");
            }
        }

        /// <summary>
        /// 智能查找枚举类型
        /// </summary>
        private Type FindEnumType(string typeName)
        {
            try
            {
                // 1. 尝试直接获取类型
                var type = Type.GetType(typeName);
                if (type != null && type.IsEnum)
                {
                    Log.Debug($"[ProcedureMonitor] 直接找到枚举类型: {type.FullName}");
                    return type;
                }

                // 2. 在所有程序集中按完整名称搜索
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        type = assembly.GetType(typeName);
                        if (type != null && type.IsEnum)
                        {
                            Log.Debug($"[ProcedureMonitor] 在程序集中找到枚举类型: {type.FullName}");
                            return type;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                // 3. 模糊匹配：按类型名称搜索（忽略命名空间）
                var candidateTypes = new List<Type>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        foreach (var t in types)
                        {
                            if (t.IsEnum)
                            {
                                // 检查类型名是否匹配（忽略命名空间）
                                if (t.Name == typeName || t.FullName == typeName)
                                {
                                    candidateTypes.Add(t);
                                }
                                // 检查嵌套类型（如 Game+State）
                                else if (typeName.Contains("+") && t.FullName?.Replace(".", "+").Contains(typeName) == true)
                                {
                                    candidateTypes.Add(t);
                                }
                                // 检查是否包含指定的类型名（用于GameState这种情况）
                                else if (typeName.Contains("State") && t.Name.Contains("State"))
                                {
                                    candidateTypes.Add(t);
                                }
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (candidateTypes.Count > 0)
                {
                    // 优先选择名称完全匹配的
                    var exactMatch = candidateTypes.FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);
                    if (exactMatch != null)
                    {
                        Log.Info($"[ProcedureMonitor] 模糊匹配找到枚举类型: {exactMatch.FullName}");
                        return exactMatch;
                    }

                    // 如果没有完全匹配，选择第一个候选
                    var firstCandidate = candidateTypes.First();
                    Log.Info($"[ProcedureMonitor] 模糊匹配找到候选枚举类型: {firstCandidate.FullName}");
                    Log.Info($"[ProcedureMonitor] 共找到 {candidateTypes.Count} 个候选类型: {string.Join(", ", candidateTypes.Select(t => t.FullName))}");
                    return firstCandidate;
                }

                Log.Warning($"[ProcedureMonitor] 未找到枚举类型: {typeName}");
                return null;
            }
            catch (Exception e)
            {
                Log.Error($"[ProcedureMonitor] 查找枚举类型失败: {e.Message}");
                return null;
            }
        }


        /// <summary>
        /// 从实例创建状态机信息
        /// </summary>
        private ProcedureStateMachineInfo CreateStateMachineInfoFromInstance(object stateMachineInstance)
        {
            try
            {
                var stateMachineType = stateMachineInstance.GetType();
                
                // 获取泛型参数（状态枚举类型）
                var genericArguments = stateMachineType.GetGenericArguments();
                if (genericArguments.Length != 1)
                {
                    Log.Warning($"[ProcedureMonitor] 状态机类型参数数量不正确: {stateMachineType.Name}");
                    return null;
                }

                var stateEnumType = genericArguments[0];
                if (!stateEnumType.IsEnum)
                {
                    Log.Warning($"[ProcedureMonitor] 状态机参数不是枚举类型: {stateEnumType.Name}");
                    return null;
                }

                var stateMachineInfo = new ProcedureStateMachineInfo
                {
                    StateMachineType = stateMachineType,
                    StateMachineTypeName = stateMachineType.Name,
                    StateMachineInstance = stateMachineInstance,
                    StateEnumType = stateEnumType,
                    StateEnumTypeName = stateEnumType.Name,
                    AssemblyName = stateMachineType.Assembly.GetName().Name
                };

                return stateMachineInfo;
            }
            catch (Exception e)
            {
                Log.Warning($"[ProcedureMonitor] 从实例创建状态机信息失败: {e.Message}");
                return null;
            }
        }


        /// <summary>
        /// 通过反射获取属性值
        /// </summary>
        private T GetPropertyValue<T>(object obj, string propertyName)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(obj);
                    if (value is T result)
                    {
                        return result;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"[ProcedureMonitor] 获取属性值失败: {propertyName}, 错误: {e.Message}");
            }

            return default(T);
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// 状态机信息
    /// </summary>
    [System.Serializable]
    public class ProcedureStateMachineInfo
    {
        public Type StateMachineType;
        public string StateMachineTypeName;
        public object StateMachineInstance;
        public Type StateEnumType;
        public string StateEnumTypeName;
        public string AssemblyName;
    }

    /// <summary>
    /// 状态信息
    /// </summary>
    [System.Serializable]
    public class ProcedureStateInfo
    {
        public object StateId;
        public string StateName;
        public Type StateEnumType;
    }

    /// <summary>
    /// 运行时信息
    /// </summary>
    [System.Serializable]
    public class ProcedureRuntimeInfo
    {
        public bool IsRunning;
        public bool IsInitialized;
        public object CurrentStateId;
        public object PreviousStateId;
        public IProcedureState CurrentState;
        public long FrameCount;
        public float StateTime;
        public DateTime LastUpdateTime;
    }

    #endregion
}