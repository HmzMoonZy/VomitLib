using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Twenty2.VomitLib.Monitor
{
    /// <summary>
    /// LubanSupport监控器 - 用于运行时监控和调试Luban数据表系统
    /// Runtime组件，监控功能仅在Editor下生效
    /// </summary>
    [System.Serializable]
    public class LubanSupportMonitor : MonoBehaviour
    {
        #region Fields

        [Header("LubanSupport监控设置")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private float _refreshInterval = 2.0f;
        [SerializeField] private bool _autoRefresh = true;

        [Header("数据表配置")]
        [SerializeField] private string _tablesInstancePath = "Game.DB";

        // Runtime数据（仅Editor可见）
        [System.NonSerialized] private LubanTablesInfo _tablesInfo;
        [System.NonSerialized] private List<LubanTableInfo> _tableInfos = new List<LubanTableInfo>();
        [System.NonSerialized] private Dictionary<string, object> _tableInstancesMap = new Dictionary<string, object>();
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

        public string TablesInstancePath
        {
            get => _tablesInstancePath;
            set => _tablesInstancePath = value;
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
                RefreshLubanData();
            }
            catch (System.Exception e)
            {
                Log.Warning($"[LubanSupportMonitor] 初始化监控失败: {e.Message}");
            }
        }

        /// <summary>
        /// 刷新Luban数据
        /// </summary>
        public void RefreshLubanData()
        {
            if (!_enableMonitoring)
            {
                Log.Warning("[LubanSupportMonitor] 监控未启用");
                return;
            }

            Log.Debug("[LubanSupportMonitor] 开始刷新Luban数据...");

            try
            {
                _tablesInfo = null;
                _tableInfos.Clear();
                _tableInstancesMap.Clear();

                InitializeTables();

                _cacheInitialized = true;
                Log.Debug($"[LubanSupportMonitor] 刷新完成 - 数据表: {_tablesInfo?.TablesTypeName ?? "未找到"}");
            }
            catch (System.Exception e)
            {
                Log.Error($"[LubanSupportMonitor] 刷新Luban数据失败: {e.Message}");
                Log.Error($"[LubanSupportMonitor] 堆栈: {e.StackTrace}");
            }
        }


        #endregion

        #region Data Access Interface (for Editor)

#if UNITY_EDITOR

        /// <summary>
        /// 获取Tables信息（仅Editor访问）
        /// </summary>
        public LubanTablesInfo GetTablesInfo() => _tablesInfo;

        /// <summary>
        /// 获取数据表信息列表（仅Editor访问）
        /// </summary>
        public List<LubanTableInfo> GetTableInfos() => _tableInfos ?? new List<LubanTableInfo>();

        /// <summary>
        /// 获取数据表实例映射（仅Editor访问）
        /// </summary>
        public Dictionary<string, object> GetTableInstancesMap() => _tableInstancesMap ?? new Dictionary<string, object>();

        /// <summary>
        /// 清空缓存（仅Editor访问）
        /// </summary>
        public void ClearCache()
        {
            _tablesInfo = null;
            _tableInfos.Clear();
            _tableInstancesMap.Clear();
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
                if (_tablesInfo != null)
                {
                    UpdateTablesRuntimeInfo();
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[LubanSupportMonitor] 刷新运行时数据失败: {e.Message}");
            }
        }

        /// <summary>
        /// 更新Tables运行时信息
        /// </summary>
        private void UpdateTablesRuntimeInfo()
        {
            try
            {
                foreach (var tableInfo in _tableInfos)
                {
                    if (_tableInstancesMap.TryGetValue(tableInfo.TableName, out var tableInstance))
                    {
                        // 更新数据表统计信息
                        var dataListProperty = tableInstance.GetType().GetProperty("DataList");
                        if (dataListProperty != null)
                        {
                            var dataList = dataListProperty.GetValue(tableInstance) as System.Collections.IList;
                            tableInfo.DataCount = dataList?.Count ?? 0;
                        }

                        var dataMapProperty = tableInstance.GetType().GetProperty("DataMap");
                        if (dataMapProperty != null)
                        {
                            var dataMap = dataMapProperty.GetValue(tableInstance) as System.Collections.IDictionary;
                            tableInfo.KeyCount = dataMap?.Count ?? 0;
                        }

                        tableInfo.LastUpdateTime = DateTime.Now;
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[LubanSupportMonitor] 更新Tables运行时信息失败: {e.Message}");
            }
        }

        /// <summary>
        /// 初始化Tables
        /// </summary>
        private void InitializeTables()
        {
            Log.Debug("[LubanSupportMonitor] 开始初始化Tables...");

            try
            {
                // 查找Tables实例
                var tablesInstance = FindTablesInstance();
                if (tablesInstance != null)
                {
                    _tablesInfo = CreateTablesInfoFromInstance(tablesInstance);
                    if (_tablesInfo != null)
                    {
                        ScanTablesForTables();
                        Log.Info($"[LubanSupportMonitor] 成功初始化Tables: {_tablesInfo.TablesTypeName}");
                    }
                }
                else
                {
                    Log.Warning($"[LubanSupportMonitor] 未找到Tables实例: {_tablesInstancePath}");
                }
            }
            catch (Exception e)
            {
                Log.Error($"[LubanSupportMonitor] 初始化Tables失败: {e.Message}");
            }
        }

        /// <summary>
        /// 查找Tables实例
        /// </summary>
        private object FindTablesInstance()
        {
            try
            {
                // 解析路径（例如 "Game.DB"）
                var parts = _tablesInstancePath.Split('.');
                if (parts.Length != 2)
                {
                    Log.Error($"[LubanSupportMonitor] 路径格式错误: {_tablesInstancePath}, 应为 'ClassName.PropertyName'");
                    return null;
                }

                var className = parts[0];
                var memberName = parts[1];

                // 查找类型
                Type targetType = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        targetType = assembly.GetType(className);
                        if (targetType != null) break;
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (targetType == null)
                {
                    Log.Error($"[LubanSupportMonitor] 未找到类型: {className}");
                    return null;
                }

                Log.Debug($"[LubanSupportMonitor] 找到类型: {targetType.FullName}");

                // 首先尝试获取属性
                var property = targetType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);
                if (property != null)
                {
                    Log.Debug($"[LubanSupportMonitor] 找到属性: {property.Name}, 类型: {property.PropertyType.Name}");
                    
                    var instance = property.GetValue(null);
                    if (instance == null)
                    {
                        Log.Warning($"[LubanSupportMonitor] 属性值为null: {targetType.Name}.{memberName} (可能还未初始化)");
                        return null;
                    }
                    
                    Log.Info($"[LubanSupportMonitor] 成功获取Tables实例: {instance.GetType().Name}");
                    return instance;
                }

                // 如果没有找到属性，尝试字段
                var field = targetType.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                {
                    Log.Debug($"[LubanSupportMonitor] 找到字段: {field.Name}, 类型: {field.FieldType.Name}");
                    
                    var instance = field.GetValue(null);
                    if (instance == null)
                    {
                        Log.Warning($"[LubanSupportMonitor] 字段值为null: {targetType.Name}.{memberName} (可能还未初始化)");
                        return null;
                    }
                    
                    Log.Info($"[LubanSupportMonitor] 成功获取Tables实例: {instance.GetType().Name}");
                    return instance;
                }

                Log.Error($"[LubanSupportMonitor] 未找到属性或字段: {targetType.Name}.{memberName}");
                return null;
            }
            catch (Exception e)
            {
                Log.Error($"[LubanSupportMonitor] 查找Tables实例失败: {e.Message}");
                Log.Error($"[LubanSupportMonitor] 堆栈: {e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 从实例创建Tables信息
        /// </summary>
        private LubanTablesInfo CreateTablesInfoFromInstance(object tablesInstance)
        {
            try
            {
                var tablesType = tablesInstance.GetType();

                var tablesInfo = new LubanTablesInfo
                {
                    TablesType = tablesType,
                    TablesTypeName = tablesType.Name,
                    TablesInstance = tablesInstance,
                    AssemblyName = tablesType.Assembly.GetName().Name,
                    Namespace = tablesType.Namespace
                };

                return tablesInfo;
            }
            catch (Exception e)
            {
                Log.Warning($"[LubanSupportMonitor] 从实例创建Tables信息失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 扫描Tables中的数据表
        /// </summary>
        private void ScanTablesForTables()
        {
            try
            {
                _tableInfos.Clear();
                _tableInstancesMap.Clear();

                var tablesType = _tablesInfo.TablesType;
                var tablesInstance = _tablesInfo.TablesInstance;

                // 获取所有公共属性
                var properties = tablesType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    // 过滤掉非数据表属性（通常数据表属性名以Tb开头）
                    if (!property.Name.StartsWith("Tb")) continue;

                    try
                    {
                        var tableInstance = property.GetValue(tablesInstance);
                        if (tableInstance != null)
                        {
                            var tableInfo = CreateTableInfo(property, tableInstance);
                            if (tableInfo != null)
                            {
                                _tableInfos.Add(tableInfo);
                                _tableInstancesMap[tableInfo.TableName] = tableInstance;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"[LubanSupportMonitor] 获取数据表实例失败: {property.Name}, 错误: {e.Message}");
                    }
                }

                Log.Debug($"[LubanSupportMonitor] 扫描到 {_tableInfos.Count} 个数据表");
            }
            catch (Exception e)
            {
                Log.Warning($"[LubanSupportMonitor] 扫描Tables数据表失败: {e.Message}");
            }
        }

        /// <summary>
        /// 创建数据表信息
        /// </summary>
        private LubanTableInfo CreateTableInfo(PropertyInfo property, object tableInstance)
        {
            try
            {
                var tableType = property.PropertyType;

                // 获取数据统计信息
                int dataCount = 0;
                int keyCount = 0;
                Type dataType = null;

                var dataListProperty = tableType.GetProperty("DataList");
                if (dataListProperty != null)
                {
                    var dataList = dataListProperty.GetValue(tableInstance) as System.Collections.IList;
                    dataCount = dataList?.Count ?? 0;

                    // 尝试获取数据类型
                    var listType = dataListProperty.PropertyType;
                    if (listType.IsGenericType)
                    {
                        dataType = listType.GetGenericArguments()[0];
                    }
                }

                var dataMapProperty = tableType.GetProperty("DataMap");
                if (dataMapProperty != null)
                {
                    var dataMap = dataMapProperty.GetValue(tableInstance) as System.Collections.IDictionary;
                    keyCount = dataMap?.Count ?? 0;
                }

                var tableInfo = new LubanTableInfo
                {
                    TableName = property.Name,
                    TableType = tableType,
                    TableTypeName = tableType.Name,
                    DataType = dataType,
                    DataTypeName = dataType?.Name ?? "Unknown",
                    DataCount = dataCount,
                    KeyCount = keyCount,
                    AssemblyName = tableType.Assembly.GetName().Name,
                    LastUpdateTime = DateTime.Now
                };

                return tableInfo;
            }
            catch (Exception e)
            {
                Log.Warning($"[LubanSupportMonitor] 创建数据表信息失败: {e.Message}");
                return null;
            }
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Tables信息
    /// </summary>
    [System.Serializable]
    public class LubanTablesInfo
    {
        public Type TablesType;
        public string TablesTypeName;
        public object TablesInstance;
        public string AssemblyName;
        public string Namespace;
    }

    /// <summary>
    /// 数据表信息
    /// </summary>
    [System.Serializable]
    public class LubanTableInfo
    {
        public string TableName;
        public Type TableType;
        public string TableTypeName;
        public Type DataType;
        public string DataTypeName;
        public int DataCount;
        public int KeyCount;
        public string AssemblyName;
        public DateTime LastUpdateTime;
    }

    #endregion
}