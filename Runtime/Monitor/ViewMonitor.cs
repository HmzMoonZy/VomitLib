#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Twenty2.VomitLib.Monitor
{
    /// <summary>
    /// View系统监控器 - 监控ViewRoot和View状态
    /// </summary>
    public class ViewMonitor : MonoBehaviour
    {
        [Header("View监控设置")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private float _refreshInterval = 1.0f;
        
        [Space(10)]
        [Header("监控信息")]
        [SerializeField, TextArea(6, 10)] 
        private string _monitorInfo = "View监控信息将在这里显示...";
        
        // 内部状态
        private float _lastRefreshTime;
        private object _cachedViewRoot;
        private ViewDebugInfo _debugInfo;
        
        #region Unity Callbacks
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            if (_enableMonitoring && Time.time - _lastRefreshTime > _refreshInterval)
            {
                RefreshMonitorInfo();
                _lastRefreshTime = Time.time;
            }
        }
        
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 初始化View监控
        /// </summary>
        private void Initialize()
        {
            try
            {
                // 查找ViewRoot（用于显示连接状态）
                var viewRootType = FindTypeByName("ViewRoot");
                if (viewRootType != null)
                {
                    _cachedViewRoot = FindObjectOfType(viewRootType);
                }
                
                RefreshMonitorInfo();
            }
            catch (Exception e)
            {
                _monitorInfo = $"[ERROR] View监控初始化失败: {e.Message}";
            }
        }
        
        #endregion
        
        #region Monitor Logic
        
        /// <summary>
        /// 刷新监控信息
        /// </summary>
        private void RefreshMonitorInfo()
        {
            try
            {
                _debugInfo = ExtractViewDebugInfo();
                _monitorInfo = BuildMonitorInfoText();
            }
            catch (Exception e)
            {
                _monitorInfo = $"[ERROR] 刷新View监控信息失败: {e.Message}";
            }
        }
        
        /// <summary>
        /// 提取View调试信息
        /// </summary>
        private ViewDebugInfo ExtractViewDebugInfo()
        {
            var info = new ViewDebugInfo();
            
            try
            {
                // 查找静态View类 - 使用正确的程序集名称
                var viewType = Type.GetType("Twenty2.VomitLib.View.View, Twenty2.VomitLib.View");
                if (viewType == null)
                {
                    // 尝试不带程序集名称
                    viewType = Type.GetType("Twenty2.VomitLib.View.View");
                    if (viewType == null)
                    {
                        // 备用查找方法 - 搜索所有程序集
                        viewType = FindViewTypeInAssemblies();
                        if (viewType == null)
                        {
                            info.ErrorMessage = "未找到View类 - 请确保View系统已初始化";
                            return info;
                        }
                    }
                }
                
                // 直接使用System.Collections.IDictionary作为返回类型
                info.OpenedViews = GetStaticFieldValue<System.Collections.IDictionary>(viewType, "_visibleViewMap");
                info.PreloadedViews = GetStaticFieldValue<System.Collections.IDictionary>(viewType, "_preLoadMap");
                info.LoadingViews = GetStaticFieldValue<System.Collections.IDictionary>(viewType, "_hiddenViewMap");
                
                info.OpenedCount = GetCollectionCount(info.OpenedViews);
                info.PreloadedCount = GetCollectionCount(info.PreloadedViews);
                info.LoadingCount = GetCollectionCount(info.LoadingViews);
                
                // 收集详细信息
                if (info.OpenedViews != null)
                {
                    info.OpenedViewDetails = ExtractViewDetails(info.OpenedViews, "opened");
                }
                if (info.PreloadedViews != null)
                {
                    info.PreloadedViewDetails = ExtractViewDetails(info.PreloadedViews, "preloaded");
                }
                if (info.LoadingViews != null)
                {
                    info.LoadingViewDetails = ExtractViewDetails(info.LoadingViews, "hidden");
                }
                
                info.IsValid = info.OpenedViews != null || info.PreloadedViews != null || info.LoadingViews != null;
                
                if (info.IsValid)
                {
                    info.ErrorMessage = null; // 清除调试信息
                }
            }
            catch (Exception e)
            {
                info.ErrorMessage = $"提取View信息异常: {e.Message} | StackTrace: {e.StackTrace}";
            }
            
            return info;
        }
        
        /// <summary>
        /// 构建监控信息文本
        /// </summary>
        private string BuildMonitorInfoText()
        {
            var text = "VIEW 系统监控\n";
            text += "────────────────────\n";
            text += $"更新时间: {DateTime.Now:HH:mm:ss}\n\n";
            
            if (_debugInfo?.IsValid == true)
            {
                text += "统计信息:\n";
                text += $"  可见View: {_debugInfo.OpenedCount}\n";
                text += $"  预加载: {_debugInfo.PreloadedCount}\n";
                text += $"  隐藏View: {_debugInfo.LoadingCount}\n\n";
                
                // 显示当前可见的View
                if (_debugInfo.OpenedCount > 0)
                {
                    text += "当前可见的View:\n";
                    var viewNames = GetOpenedViewNames(_debugInfo.OpenedViews);
                    foreach (var name in viewNames)
                    {
                        text += $"  - {name}\n";
                    }
                }
                else
                {
                    text += "当前没有可见的View\n";
                }
            }
            else
            {
                text += $"[ERROR] {_debugInfo?.ErrorMessage ?? "未知错误"}\n";
            }
            
            return text;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// 根据名称查找类型
        /// </summary>
        private Type FindTypeByName(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.Name == typeName || type.FullName?.EndsWith($".{typeName}") == true)
                        {
                            return type;
                        }
                    }
                }
                catch { continue; }
            }
            return null;
        }
        
        /// <summary>
        /// 在所有程序集中查找View类
        /// </summary>
        private Type FindViewTypeInAssemblies()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var assemblyName = assembly.GetName().Name;
                    // 检查程序集名称是否是Twenty2.VomitLib.View或包含VomitLib
                    if (assemblyName == "Twenty2.VomitLib.View" || 
                        assemblyName.Contains("VomitLib") || 
                        assemblyName.Contains("Assembly-CSharp"))
                    {
                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            if (type.FullName == "Twenty2.VomitLib.View.View" && 
                                type.IsClass && type.IsAbstract && type.IsSealed)
                            {
                                return type;
                            }
                        }
                    }
                }
                catch { continue; }
            }
            return null;
        }
        
        /// <summary>
        /// 获取字段值
        /// </summary>
        private T GetFieldValue<T>(object instance, string fieldName)
        {
            try
            {
                var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                var value = field?.GetValue(instance);
                return value is T ? (T)value : default(T);
            }
            catch
            {
                return default(T);
            }
        }
        
        /// <summary>
        /// 获取静态字段值
        /// </summary>
        private T GetStaticFieldValue<T>(Type type, string fieldName)
        {
            try
            {
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
                if (field == null)
                {
                    return default(T);
                }
                var value = field.GetValue(null);
                return value is T ? (T)value : default(T);
            }
            catch
            {
                return default(T);
            }
        }
        
        /// <summary>
        /// 获取集合数量
        /// </summary>
        private int GetCollectionCount(object collection)
        {
            if (collection == null) return 0;
            
            try
            {
                var countProperty = collection.GetType().GetProperty("Count");
                return countProperty != null ? (int)countProperty.GetValue(collection) : 0;
            }
            catch
            {
                return 0;
            }
        }
        
        /// <summary>
        /// 提取View详细信息
        /// </summary>
        private List<ViewDetailInfo> ExtractViewDetails(System.Collections.IDictionary viewCollection, string collectionType)
        {
            var details = new List<ViewDetailInfo>();
            
            if (viewCollection == null) return details;
            
            try
            {
                foreach (var key in viewCollection.Keys)
                {
                    var detail = new ViewDetailInfo();
                    detail.ViewName = key.ToString();
                    
                    var value = viewCollection[key];
                    if (value != null)
                    {
                        // 获取ViewLogic信息
                        if (collectionType == "opened" || collectionType == "hidden")
                        {
                            detail.ViewLogicInstance = value;
                            ExtractViewLogicInfo(detail, value);
                        }
                        else if (collectionType == "preloaded")
                        {
                            // 预加载的是GameObject
                            ExtractPreloadedViewInfo(detail, value);
                        }
                    }
                    
                    details.Add(detail);
                }
            }
            catch (Exception e)
            {
                var errorDetail = new ViewDetailInfo();
                errorDetail.ViewName = $"提取详情失败: {e.Message}";
                details.Add(errorDetail);
            }
            
            return details;
        }
        
        /// <summary>
        /// 提取ViewLogic详细信息
        /// </summary>
        private void ExtractViewLogicInfo(ViewDetailInfo detail, object viewLogic)
        {
            try
            {
                var viewType = viewLogic.GetType();
                detail.ViewTypeName = viewType.Name;
                
                // 获取ViewConfig信息
                var configProperty = viewType.GetProperty("Config");
                if (configProperty != null)
                {
                    var config = configProperty.GetValue(viewLogic);
                    if (config != null)
                    {
                        ExtractViewConfigInfo(detail, config);
                    }
                }
                
                // 获取SortOrder
                var sortOrderProperty = viewType.GetProperty("SortOrder");
                if (sortOrderProperty != null)
                {
                    detail.SortOrder = (int)sortOrderProperty.GetValue(viewLogic);
                }
                
                // 获取Canvas信息
                var canvasProperty = viewType.GetProperty("ViewCanvas");
                if (canvasProperty != null)
                {
                    var canvas = canvasProperty.GetValue(viewLogic);
                    ExtractCanvasInfo(detail, canvas);
                }
                
                detail.OpenTime = Time.time; // 当前时间作为占位符
            }
            catch (Exception e)
            {
                detail.ViewTypeName = $"获取信息失败: {e.Message}";
            }
        }
        
        /// <summary>
        /// 提取ViewConfig信息
        /// </summary>
        private void ExtractViewConfigInfo(ViewDetailInfo detail, object config)
        {
            try
            {
                var configType = config.GetType();
                
                // 获取各种配置属性
                var isCacheField = configType.GetField("IsCache");
                if (isCacheField != null)
                    detail.IsCache = (bool)isCacheField.GetValue(config);
                
                var layerField = configType.GetField("Layer");
                if (layerField != null)
                    detail.Layer = layerField.GetValue(config).ToString();
                
                var enableAutoMaskField = configType.GetField("EnableAutoMask");
                if (enableAutoMaskField != null)
                    detail.EnableAutoMask = (bool)enableAutoMaskField.GetValue(config);
                
                var autoBindButtonsField = configType.GetField("AutoBindButtons");
                if (autoBindButtonsField != null)
                    detail.AutoBindButtons = (bool)autoBindButtonsField.GetValue(config);
                
                var enableLocalizationField = configType.GetField("EnableLocalization");
                if (enableLocalizationField != null)
                    detail.EnableLocalization = (bool)enableLocalizationField.GetValue(config);
                
                var recordOpenField = configType.GetField("RecordOpen");
                if (recordOpenField != null)
                    detail.RecordOpen = (bool)recordOpenField.GetValue(config);
            }
            catch { }
        }
        
        /// <summary>
        /// 提取Canvas信息
        /// </summary>
        private void ExtractCanvasInfo(ViewDetailInfo detail, object canvas)
        {
            try
            {
                if (canvas != null)
                {
                    var rectTransform = GetFieldValue<object>(canvas, "transform");
                    if (rectTransform != null)
                    {
                        var sizeDelta = GetFieldValue<Vector2>(rectTransform, "sizeDelta");
                        detail.CanvasSize = sizeDelta;
                        
                        var parent = GetFieldValue<object>(rectTransform, "parent");
                        if (parent != null)
                        {
                            detail.ParentName = parent.ToString();
                        }
                    }
                    
                    var enabledProperty = canvas.GetType().GetProperty("enabled");
                    if (enabledProperty != null)
                    {
                        detail.IsActive = (bool)enabledProperty.GetValue(canvas);
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// 提取预加载View信息
        /// </summary>
        private void ExtractPreloadedViewInfo(ViewDetailInfo detail, object gameObject)
        {
            try
            {
                detail.ViewTypeName = "预加载GameObject";
                detail.IsPreloadable = true; // 在预加载字典中的都是可预加载的
                detail.PreloadPriority = 0; // 默认优先级
                detail.ResourcePath = "预加载资源";
                
                // 尝试获取ViewLogic组件
                var getComponentMethod = gameObject.GetType().GetMethod("GetComponent", new Type[] { });
                if (getComponentMethod != null)
                {
                    var viewLogicBaseType = FindTypeByName("ViewLogic");
                    if (viewLogicBaseType != null)
                    {
                        var genericMethod = getComponentMethod.MakeGenericMethod(viewLogicBaseType);
                        var viewLogic = genericMethod.Invoke(gameObject, null);
                        if (viewLogic != null)
                        {
                            ExtractViewLogicInfo(detail, viewLogic);
                            detail.ViewTypeName = viewLogic.GetType().Name + " (预加载)";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                detail.ViewTypeName = $"预加载信息获取失败: {e.Message}";
            }
        }
        
        
        /// <summary>
        /// 获取已打开的View名称
        /// </summary>
        private string[] GetOpenedViewNames(System.Collections.IDictionary openedViews)
        {
            try
            {
                if (openedViews == null) return new string[0];
                
                var names = new List<string>();
                foreach (var key in openedViews.Keys)
                {
                    names.Add(key.ToString());
                }
                return names.ToArray();
            }
            catch
            {
                return new string[0];
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 手动刷新监控信息
        /// </summary>
        [ContextMenu("刷新View监控")]
        public void ManualRefresh()
        {
            RefreshMonitorInfo();
        }
        
        /// <summary>
        /// 获取当前调试信息
        /// </summary>
        public ViewDebugInfo GetDebugInfo() => _debugInfo;
        
        /// <summary>
        /// 打开指定View
        /// </summary>
        public void OpenView(string viewName)
        {
            try
            {
                var viewType = FindViewTypeInAssemblies();
                if (viewType != null)
                {
                    var openMethod = viewType.GetMethod("Open", new Type[] { typeof(object) });
                    if (openMethod == null)
                    {
                        openMethod = viewType.GetMethod("Open", Type.EmptyTypes);
                    }
                    
                    if (openMethod != null)
                    {
                        if (openMethod.GetParameters().Length > 0)
                        {
                            openMethod.Invoke(null, new object[] { null });
                        }
                        else
                        {
                            openMethod.Invoke(null, null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"打开View失败: {viewName}, 错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 关闭指定View
        /// </summary>
        public void CloseView(string viewName)
        {
            try
            {
                var viewType = FindViewTypeInAssemblies();
                if (viewType != null)
                {
                    var closeMethod = viewType.GetMethod("Close", new Type[] { typeof(string), typeof(object) });
                    if (closeMethod == null)
                    {
                        closeMethod = viewType.GetMethod("Close", new Type[] { typeof(string) });
                    }
                    
                    if (closeMethod != null)
                    {
                        if (closeMethod.GetParameters().Length > 1)
                        {
                            closeMethod.Invoke(null, new object[] { viewName, null });
                        }
                        else
                        {
                            closeMethod.Invoke(null, new object[] { viewName });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"关闭View失败: {viewName}, 错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 冻结指定View
        /// </summary>
        public void FreezeView(string viewName)
        {
            try
            {
                var viewType = FindViewTypeInAssemblies();
                if (viewType != null)
                {
                    var freezeMethod = viewType.GetMethod("Freeze", new Type[] { typeof(string) });
                    if (freezeMethod != null)
                    {
                        freezeMethod.Invoke(null, new object[] { viewName });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"冻结View失败: {viewName}, 错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 解冻指定View
        /// </summary>
        public void UnfreezeView(string viewName)
        {
            try
            {
                var viewType = FindViewTypeInAssemblies();
                if (viewType != null)
                {
                    var unfreezeMethod = viewType.GetMethod("UnFreeze", new Type[] { typeof(string) });
                    if (unfreezeMethod != null)
                    {
                        unfreezeMethod.Invoke(null, new object[] { viewName });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"解冻View失败: {viewName}, 错误: {e.Message}");
            }
        }
        
        #endregion
        
        #region Data Classes
        
        /// <summary>
        /// View调试信息
        /// </summary>
        public class ViewDebugInfo
        {
            public bool IsValid;
            public string ErrorMessage;
            
            public System.Collections.IDictionary OpenedViews;
            public System.Collections.IDictionary PreloadedViews;
            public System.Collections.IDictionary LoadingViews;
            
            public int OpenedCount;
            public int PreloadedCount;
            public int LoadingCount;
            
            // 详细View信息
            public List<ViewDetailInfo> OpenedViewDetails = new List<ViewDetailInfo>();
            public List<ViewDetailInfo> PreloadedViewDetails = new List<ViewDetailInfo>();
            public List<ViewDetailInfo> LoadingViewDetails = new List<ViewDetailInfo>();
        }
        
        /// <summary>
        /// View详细信息
        /// </summary>
        public class ViewDetailInfo
        {
            public string ViewName;
            public string ViewTypeName;
            public bool IsCache;
            public string Layer;
            public int SortOrder;
            public bool EnableAutoMask;
            public bool AutoBindButtons;
            public bool EnableLocalization;
            public bool RecordOpen;
            public bool IsPreloadable;
            public int PreloadPriority;
            public string ResourcePath;
            public float OpenTime;  // 打开时间戳
            public object ViewLogicInstance;
            
            // 运行时状态
            public bool IsActive;
            public Vector2 CanvasSize;
            public string ParentName;
        }
        
        #endregion
    }
}
#endif