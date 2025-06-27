using System.Collections.Generic;
using System.Linq;
using Twenty2.VomitLib.Monitor;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// ViewMonitor的自定义Inspector - 运行时View调试面板
    /// 基于ViewRootInspector的重绘逻辑，适配ViewMonitor
    /// </summary>
    [CustomEditor(typeof(ViewMonitor))]
    public class ViewMonitorInspector : UnityEditor.Editor
    {
        #region Fields
        
        private ViewMonitor _viewMonitor;
        private bool _showVisibleViews = true;
        private bool _showHiddenViews = true;
        private bool _showPreloadedViews = true;
        private double _lastRefreshTime;
        
        // GUI稳定性相关
        private bool _needsRefresh;
        private string _pendingCloseView;
        private string _pendingShowView;
        private bool _pendingCloseAllVisible;
        
        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _viewItemStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _statisticsStyle;
        
        // 颜色
        private readonly Color _visibleColor = new Color(0.4f, 0.8f, 0.4f, 0.3f);
        private readonly Color _hiddenColor = new Color(0.8f, 0.8f, 0.4f, 0.3f);
        private readonly Color _preloadColor = new Color(0.4f, 0.4f, 0.8f, 0.3f);
        
        #endregion
        
        #region Unity Callbacks
        
        private void OnEnable()
        {
            _viewMonitor = (ViewMonitor)target;
            
            // 延迟初始化样式，避免运行时EditorStyles未准备好的问题
            EditorApplication.delayCall += InitializeStyles;
            
            // 如果在运行时且启用监控，立即刷新一次数据
            if (Application.isPlaying && _viewMonitor != null && _viewMonitor.EnableMonitoring)
            {
                EditorApplication.delayCall += () => RefreshViewDataFromReflection();
            }
        }
        
        private void OnDisable()
        {
            // 清理延迟调用，避免内存泄漏
            EditorApplication.delayCall -= InitializeStyles;
        }
        
        public override void OnInspectorGUI()
        {
            // 确保样式已初始化
            if (_headerStyle == null)
            {
                InitializeStyles();
                if (_headerStyle == null) return; // 如果样式仍未初始化，跳过绘制
            }
            
            // 处理待执行的操作（在GUI绘制开始前）
            ProcessPendingOperations();
            
            // 绘制默认Inspector（基本设置）
            serializedObject.Update();
            
            EditorGUILayout.LabelField("ViewMonitor 配置", _headerStyle);
            EditorGUILayout.Space(5);
            
            // 绘制监控设置
            DrawMonitorSettings();
            
            EditorGUILayout.Space(10);
            
            // 只在运行时且启用监控时显示View调试信息
            if (Application.isPlaying && _viewMonitor.EnableMonitoring)
            {
                DrawViewDebugPanel();
            }
            else
            {
                DrawPlayModeInfo();
            }
            
            serializedObject.ApplyModifiedProperties();
            
            // 自动刷新检查（不在GUI绘制过程中执行）
            CheckAutoRefresh();
        }
        
        #endregion
        
        #region Operation Management
        
        /// <summary>
        /// 处理待执行的操作
        /// </summary>
        private void ProcessPendingOperations()
        {
            // 处理刷新请求
            if (_needsRefresh)
            {
                _needsRefresh = false;
                EditorApplication.delayCall += () =>
                {
                    RefreshViewDataFromReflection();
                    Repaint();
                };
            }
            
            // 处理关闭View请求
            if (!string.IsNullOrEmpty(_pendingCloseView))
            {
                var viewName = _pendingCloseView;
                _pendingCloseView = null;
                EditorApplication.delayCall += () =>
                {
                    CloseViewByReflection(viewName);
                };
            }
            
            // 处理显示View请求
            if (!string.IsNullOrEmpty(_pendingShowView))
            {
                var viewName = _pendingShowView;
                _pendingShowView = null;
                EditorApplication.delayCall += () =>
                {
                    ShowViewByReflection(viewName);
                };
            }
            
            // 处理关闭所有可见View请求
            if (_pendingCloseAllVisible)
            {
                _pendingCloseAllVisible = false;
                EditorApplication.delayCall += () =>
                {
                    CloseAllVisibleViewsByReflection();
                };
            }
        }
        
        /// <summary>
        /// 检查自动刷新（不在GUI绘制过程中执行刷新）
        /// </summary>
        private void CheckAutoRefresh()
        {
            if (_viewMonitor.AutoRefresh && _viewMonitor.EnableMonitoring && Application.isPlaying && 
                EditorApplication.timeSinceStartup - _lastRefreshTime > _viewMonitor.RefreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                EditorApplication.delayCall += () =>
                {
                    RefreshViewDataFromReflection();
                    Repaint();
                };
            }
        }
        
        #endregion
        
        #region GUI Drawing
        
        /// <summary>
        /// 绘制监控器设置
        /// </summary>
        private void DrawMonitorSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 启用监控
            var enableProp = serializedObject.FindProperty("_enableMonitoring");
            EditorGUILayout.PropertyField(enableProp, new GUIContent("启用监控", "是否启用View系统监控"));
            
            // 只有启用监控时才显示其他设置
            if (enableProp.boolValue)
            {
                EditorGUILayout.Space(3);
                
                // 自动刷新
                var autoRefreshProp = serializedObject.FindProperty("_autoRefresh");
                EditorGUILayout.PropertyField(autoRefreshProp, new GUIContent("自动刷新", "是否自动刷新View数据"));
                
                // 刷新间隔
                if (autoRefreshProp.boolValue)
                {
                    var refreshIntervalProp = serializedObject.FindProperty("_refreshInterval");
                    EditorGUILayout.PropertyField(refreshIntervalProp, new GUIContent("刷新间隔(秒)", "自动刷新的时间间隔"));
                    refreshIntervalProp.floatValue = Mathf.Clamp(refreshIntervalProp.floatValue, 0.1f, 5f);
                }
                
                EditorGUILayout.Space(3);
                
                // 显示选项
                EditorGUILayout.LabelField("显示选项", EditorStyles.boldLabel);
                var showVisibleProp = serializedObject.FindProperty("_showVisibleViews");
                var showHiddenProp = serializedObject.FindProperty("_showHiddenViews");
                var showPreloadedProp = serializedObject.FindProperty("_showPreloadedViews");
                
                EditorGUILayout.PropertyField(showVisibleProp, new GUIContent("显示可见View"));
                EditorGUILayout.PropertyField(showHiddenProp, new GUIContent("显示隐藏View"));
                EditorGUILayout.PropertyField(showPreloadedProp, new GUIContent("显示预加载View"));
                
                // 同步本地变量
                _showVisibleViews = showVisibleProp.boolValue;
                _showHiddenViews = showHiddenProp.boolValue;
                _showPreloadedViews = showPreloadedProp.boolValue;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制非运行时提示
        /// </summary>
        private void DrawPlayModeInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("🎮 View监控面板", _headerStyle);
                EditorGUILayout.LabelField("请在运行时查看View系统监控信息", _infoStyle);
            }
            else if (!_viewMonitor.EnableMonitoring)
            {
                EditorGUILayout.LabelField("⚠️ 监控未启用", _headerStyle);
                EditorGUILayout.LabelField("请启用监控以查看View系统信息", _infoStyle);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制View调试面板
        /// </summary>
        private void DrawViewDebugPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 标题和控制按钮
            DrawPanelHeader();
            
            EditorGUILayout.Space(5);
            
            // 统计信息
            DrawStatistics();
            
            EditorGUILayout.Space(3);
            
            // 可见的View
            if (_showVisibleViews)
            {
                DrawViewSection("👁️ 可见的View", _viewMonitor.GetCachedVisibleViews(), _visibleColor, true);
            }
            
            // 隐藏的View
            if (_showHiddenViews)
            {
                DrawViewSection("👻 隐藏的View", _viewMonitor.GetCachedHiddenViews(), _hiddenColor, false);
            }
            
            // 预加载的View
            if (_showPreloadedViews)
            {
                var preloadedViews = _viewMonitor.GetCachedPreloadedViews();
                if (preloadedViews.Count > 0)
                {
                    DrawViewSection("📦 预加载的View", preloadedViews, _preloadColor, false);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制面板头部
        /// </summary>
        private void DrawPanelHeader()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("🔍 View系统监控面板", _headerStyle);
            
            GUILayout.FlexibleSpace();
            
            // 手动刷新按钮
            if (GUILayout.Button("🔄 刷新", _buttonStyle, GUILayout.Width(60)))
            {
                _needsRefresh = true;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制统计信息
        /// </summary>
        private void DrawStatistics()
        {
            var stats = _viewMonitor.GetViewStatistics();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📊 统计信息", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"总计: {stats.TotalCount}", _statisticsStyle);
            EditorGUILayout.LabelField($"可见: {stats.VisibleCount}", _statisticsStyle);
            EditorGUILayout.LabelField($"隐藏: {stats.HiddenCount}", _statisticsStyle);
            EditorGUILayout.LabelField($"预加载: {stats.PreloadedCount}", _statisticsStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制View分组
        /// </summary>
        private void DrawViewSection(string title, List<ViewInfo> views, Color bgColor, bool showCloseAllButton)
        {
            EditorGUILayout.Space(3);
            
            // 分组头部
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{title} ({views.Count})", EditorStyles.boldLabel);
            
            // 全部关闭按钮（仅对可见View显示）
            if (showCloseAllButton && views.Count > 0)
            {
                if (GUILayout.Button("全部关闭", _buttonStyle, GUILayout.Width(60)))
                {
                    _pendingCloseAllVisible = true;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 分组内容
            if (views.Count == 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("  (无)", _infoStyle);
                EditorGUILayout.EndVertical();
            }
            else
            {
                foreach (var viewInfo in views.OrderBy(v => v.SortOrder).ThenBy(v => v.Name))
                {
                    DrawViewItem(viewInfo, bgColor);
                }
            }
        }
        
        /// <summary>
        /// 绘制单个View项
        /// </summary>
        private void DrawViewItem(ViewInfo viewInfo, Color bgColor)
        {
            // 背景色
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            
            EditorGUILayout.BeginVertical(_viewItemStyle);
            GUI.backgroundColor = originalColor;
            
            EditorGUILayout.BeginHorizontal();
            
            // View名称和状态
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📋 {viewInfo.Name}", EditorStyles.boldLabel);
            
            // 显示属性标签
            if (viewInfo.IsPreloaded)
            {
                originalColor = GUI.contentColor;
                GUI.contentColor = Color.cyan;
                EditorGUILayout.LabelField("[预加载]", EditorStyles.miniLabel, GUILayout.Width(50));
                GUI.contentColor = originalColor;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // View详细信息
            var infoText = $"层级: {viewInfo.SortOrder}";
            if (viewInfo.IsVisible)
            {
                if (viewInfo.IsCache) infoText += " | 缓存: ✓";
                if (viewInfo.HasMask) infoText += " | 遮罩: ✓";
            }
            else if (viewInfo.IsHidden)
            {
                if (viewInfo.IsCache) infoText += " | 缓存: ✓";
            }
            else if (viewInfo.IsPreloaded)
            {
                infoText += " | 状态: 仅预加载，未显示";
            }
            
            EditorGUILayout.LabelField(infoText, _infoStyle);
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            // 操作按钮
            EditorGUILayout.BeginVertical(GUILayout.Width(60));
            
            if (viewInfo.IsVisible)
            {
                if (GUILayout.Button("关闭", _buttonStyle))
                {
                    _pendingCloseView = viewInfo.Name;
                }
            }
            else if (viewInfo.IsHidden || viewInfo.IsPreloaded)
            {
                if (GUILayout.Button("显示", _buttonStyle))
                {
                    _pendingShowView = viewInfo.Name;
                }
            }
            
            // 选中按钮
            if (viewInfo.GameObject != null)
            {
                if (GUILayout.Button("选中", _buttonStyle))
                {
                    Selection.activeGameObject = viewInfo.GameObject;
                    EditorGUIUtility.PingObject(viewInfo.GameObject);
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        #endregion
        
        #region Styles
        
        /// <summary>
        /// 初始化样式
        /// </summary>
        private void InitializeStyles()
        {
            try
            {
                // 检查EditorStyles是否可用
                if (EditorStyles.boldLabel == null)
                {
                    // EditorStyles还未初始化，延迟到下一帧
                    EditorApplication.delayCall += InitializeStyles;
                    return;
                }
                
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                };
                
                _viewItemStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(8, 8, 4, 4)
                };
                
                _infoStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.gray : Color.black }
                };
                
                _buttonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize = 10
                };
                
                _statisticsStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.cyan : Color.blue },
                    fontStyle = FontStyle.Bold
                };
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ViewMonitorInspector] 初始化样式失败: {e.Message}，将使用默认样式");
                InitializeDefaultStyles();
            }
        }
        
        /// <summary>
        /// 初始化默认样式（当EditorStyles不可用时）
        /// </summary>
        private void InitializeDefaultStyles()
        {
            _headerStyle = new GUIStyle()
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };
            
            _viewItemStyle = new GUIStyle()
            {
                padding = new RectOffset(8, 8, 4, 4)
            };
            
            _infoStyle = new GUIStyle()
            {
                fontSize = 10,
                normal = { textColor = Color.gray }
            };
            
            _buttonStyle = new GUIStyle()
            {
                fontSize = 10
            };
            
            _statisticsStyle = new GUIStyle()
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.blue }
            };
        }
        
        #endregion
        
        #region Type Helper Methods
        
        /// <summary>
        /// 获取View类型，尝试多个可能的程序集
        /// </summary>
        private System.Type GetViewType()
        {
            // 尝试的程序集名称列表
            var assemblyNames = new[]
            {
                "Twenty2.VomitLib.View",           // VomitLib View专用程序集
                "Assembly-CSharp",                 // 默认程序集
                "Twenty2.VomitLib",               // VomitLib核心程序集  
                "VomitLib.Runtime",               // Runtime程序集
                "Twenty2.VomitLib.Runtime"        // VomitLib Runtime程序集
            };
            
            foreach (var assemblyName in assemblyNames)
            {
                try
                {
                    var type = System.Type.GetType($"Twenty2.VomitLib.View.View, {assemblyName}");
                    if (type != null)
                    {
                        // Debug.Log($"[ViewMonitorInspector] 在程序集 {assemblyName} 中找到View类型");
                        return type;
                    }
                }
                catch (System.Exception)
                {
                    // 继续尝试下一个程序集
                }
            }
            
            // 如果上述方法都失败，尝试扫描所有已加载的程序集
            try
            {
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType("Twenty2.VomitLib.View.View");
                    if (type != null)
                    {
                        // Debug.Log($"[ViewMonitorInspector] 在程序集 {assembly.GetName().Name} 中找到View类型");
                        return type;
                    }
                }
            }
            catch (System.Exception e)
            {
                // Debug.LogWarning($"[ViewMonitorInspector] 扫描程序集时出错: {e.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取ViewParameterBase类型
        /// </summary>
        private System.Type GetViewParameterType()
        {
            // 尝试的程序集名称列表
            var assemblyNames = new[]
            {
                "Twenty2.VomitLib.View",
                "Assembly-CSharp",
                "Twenty2.VomitLib",
                "VomitLib.Runtime",
                "Twenty2.VomitLib.Runtime"
            };
            
            foreach (var assemblyName in assemblyNames)
            {
                try
                {
                    var type = System.Type.GetType($"Twenty2.VomitLib.View.ViewParameterBase, {assemblyName}");
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch (System.Exception)
                {
                    // 继续尝试下一个程序集
                }
            }
            
            // 如果上述方法都失败，尝试扫描所有已加载的程序集
            try
            {
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType("Twenty2.VomitLib.View.ViewParameterBase");
                    if (type != null)
                    {
                        return type;
                    }
                }
            }
            catch (System.Exception)
            {
                // 忽略错误
            }
            
            return null;
        }
        
        #endregion
        
        #region Reflection Operations
        
        /// <summary>
        /// 通过反射刷新View数据并更新ViewMonitor
        /// </summary>
        private void RefreshViewDataFromReflection()
        {
            if (!Application.isPlaying || !_viewMonitor.EnableMonitoring)
                return;
                
            var visibleViews = GetVisibleViewsFromReflection();
            var hiddenViews = GetHiddenViewsFromReflection();
            var preloadedViews = GetPreloadedViewsFromReflection();
            
            _viewMonitor.UpdateVisibleViews(visibleViews);
            _viewMonitor.UpdateHiddenViews(hiddenViews);
            _viewMonitor.UpdatePreloadedViews(preloadedViews);
        }
        
        /// <summary>
        /// 通过反射获取可见View信息
        /// </summary>
        private List<ViewInfo> GetVisibleViewsFromReflection()
        {
            var views = new List<ViewInfo>();
            
            try
            {
                var viewType = GetViewType();
                if (viewType == null)
                {
                    // Debug.LogWarning("[ViewMonitorInspector] 无法找到View类型");
                    return views;
                }
                
                var visibleViewMapField = viewType.GetField("_visibleViewMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (visibleViewMapField?.GetValue(null) is System.Collections.IDictionary visibleViews)
                {
                    var preloadedViewNames = GetPreloadedViewNamesFromReflection();
                    
                    foreach (System.Collections.DictionaryEntry kvp in visibleViews)
                    {
                        var viewName = kvp.Key as string;
                        var viewLogic = kvp.Value;
                        
                        if (viewLogic != null && !string.IsNullOrEmpty(viewName))
                        {
                            var viewLogicType = viewLogic.GetType();
                            var viewCanvasProperty = viewLogicType.GetProperty("ViewCanvas");
                            var configProperty = viewLogicType.GetProperty("Config");
                            var gameObjectProperty = viewLogicType.GetProperty("gameObject");
                            
                            var viewCanvas = viewCanvasProperty?.GetValue(viewLogic);
                            var config = configProperty?.GetValue(viewLogic);
                            var gameObject = gameObjectProperty?.GetValue(viewLogic) as GameObject;
                            
                            var sortingOrder = 0;
                            if (viewCanvas != null)
                            {
                                var sortingOrderProperty = viewCanvas.GetType().GetProperty("sortingOrder");
                                if (sortingOrderProperty != null)
                                {
                                    sortingOrder = (int)(sortingOrderProperty.GetValue(viewCanvas) ?? 0);
                                }
                            }
                            
                            var isCache = false;
                            var hasMask = false;
                            if (config != null)
                            {
                                var isCacheProperty = config.GetType().GetProperty("IsCache");
                                var enableAutoMaskProperty = config.GetType().GetProperty("EnableAutoMask");
                                
                                isCache = (bool)(isCacheProperty?.GetValue(config) ?? false);
                                hasMask = (bool)(enableAutoMaskProperty?.GetValue(config) ?? false);
                            }
                            
                            views.Add(new ViewInfo
                            {
                                Name = viewName,
                                SortOrder = sortingOrder,
                                IsVisible = true,
                                IsPreloaded = preloadedViewNames.Contains(viewName),
                                IsCache = isCache,
                                HasMask = hasMask,
                                GameObject = gameObject
                            });
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                // Debug.LogWarning($"[ViewMonitorInspector] 获取可见View信息失败: {e.Message}");
            }
            
            return views;
        }
        
        /// <summary>
        /// 通过反射获取隐藏View信息
        /// </summary>
        private List<ViewInfo> GetHiddenViewsFromReflection()
        {
            var views = new List<ViewInfo>();
            
            try
            {
                var viewType = GetViewType();
                if (viewType == null) return views;
                
                var hiddenViewMapField = viewType.GetField("_hiddenViewMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (hiddenViewMapField?.GetValue(null) is System.Collections.IDictionary hiddenViews)
                {
                    var preloadedViewNames = GetPreloadedViewNamesFromReflection();
                    
                    foreach (System.Collections.DictionaryEntry kvp in hiddenViews)
                    {
                        var viewName = kvp.Key as string;
                        var viewLogic = kvp.Value;
                        
                        if (viewLogic != null && !string.IsNullOrEmpty(viewName))
                        {
                            var viewLogicType = viewLogic.GetType();
                            var viewCanvasProperty = viewLogicType.GetProperty("ViewCanvas");
                            var configProperty = viewLogicType.GetProperty("Config");
                            var gameObjectProperty = viewLogicType.GetProperty("gameObject");
                            
                            var viewCanvas = viewCanvasProperty?.GetValue(viewLogic);
                            var config = configProperty?.GetValue(viewLogic);
                            var gameObject = gameObjectProperty?.GetValue(viewLogic) as GameObject;
                            
                            var sortingOrder = 0;
                            if (viewCanvas != null)
                            {
                                var sortingOrderProperty = viewCanvas.GetType().GetProperty("sortingOrder");
                                if (sortingOrderProperty != null)
                                {
                                    sortingOrder = (int)(sortingOrderProperty.GetValue(viewCanvas) ?? 0);
                                }
                            }
                            
                            var isCache = false;
                            if (config != null)
                            {
                                var isCacheProperty = config.GetType().GetProperty("IsCache");
                                isCache = (bool)(isCacheProperty?.GetValue(config) ?? false);
                            }
                            
                            views.Add(new ViewInfo
                            {
                                Name = viewName,
                                SortOrder = sortingOrder,
                                IsHidden = true,
                                IsPreloaded = preloadedViewNames.Contains(viewName),
                                IsCache = isCache,
                                GameObject = gameObject
                            });
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                // Debug.LogWarning($"[ViewMonitorInspector] 获取隐藏View信息失败: {e.Message}");
            }
            
            return views;
        }
        
        /// <summary>
        /// 通过反射获取预加载View信息
        /// </summary>
        private List<ViewInfo> GetPreloadedViewsFromReflection()
        {
            var views = new List<ViewInfo>();
            
            try
            {
                var viewType = GetViewType();
                if (viewType == null) return views;
                
                var preLoadMapField = viewType.GetField("_preLoadMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (preLoadMapField?.GetValue(null) is System.Collections.IDictionary preloadedViews)
                {
                    var activeViewNames = GetActiveViewNamesFromReflection();
                    
                    foreach (System.Collections.DictionaryEntry kvp in preloadedViews)
                    {
                        var viewName = kvp.Key as string;
                        var gameObject = kvp.Value as GameObject;
                        
                        if (!string.IsNullOrEmpty(viewName) && !activeViewNames.Contains(viewName))
                        {
                            views.Add(new ViewInfo
                            {
                                Name = viewName,
                                SortOrder = 0,
                                IsPreloaded = true,
                                GameObject = gameObject
                            });
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                // Debug.LogWarning($"[ViewMonitorInspector] 获取预加载View信息失败: {e.Message}");
            }
            
            return views;
        }
        
        /// <summary>
        /// 通过反射获取预加载View名称列表
        /// </summary>
        private HashSet<string> GetPreloadedViewNamesFromReflection()
        {
            try
            {
                var viewType = GetViewType();
                if (viewType == null) return new HashSet<string>();
                
                var preLoadMapField = viewType.GetField("_preLoadMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (preLoadMapField?.GetValue(null) is System.Collections.IDictionary preloadedViews)
                {
                    var names = new HashSet<string>();
                    foreach (System.Collections.DictionaryEntry kvp in preloadedViews)
                    {
                        if (kvp.Key is string name)
                            names.Add(name);
                    }
                    return names;
                }
            }
            catch (System.Exception e)
            {
                // Debug.LogWarning($"[ViewMonitorInspector] 获取预加载View名称失败: {e.Message}");
            }
            
            return new HashSet<string>();
        }
        
        /// <summary>
        /// 通过反射获取所有活跃的View名称
        /// </summary>
        private HashSet<string> GetActiveViewNamesFromReflection()
        {
            var names = new HashSet<string>();
            
            try
            {
                var viewType = GetViewType();
                if (viewType == null) return names;
                
                // 获取可见View名称
                var visibleViewMapField = viewType.GetField("_visibleViewMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (visibleViewMapField?.GetValue(null) is System.Collections.IDictionary visibleViews)
                {
                    foreach (System.Collections.DictionaryEntry kvp in visibleViews)
                    {
                        if (kvp.Key is string name)
                            names.Add(name);
                    }
                }
                
                // 获取隐藏View名称
                var hiddenViewMapField = viewType.GetField("_hiddenViewMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (hiddenViewMapField?.GetValue(null) is System.Collections.IDictionary hiddenViews)
                {
                    foreach (System.Collections.DictionaryEntry kvp in hiddenViews)
                    {
                        if (kvp.Key is string name)
                            names.Add(name);
                    }
                }
            }
            catch (System.Exception e)
            {
                // Debug.LogWarning($"[ViewMonitorInspector] 获取活跃View名称失败: {e.Message}");
            }
            
            return names;
        }
        
        /// <summary>
        /// 通过反射关闭指定View
        /// </summary>
        private void CloseViewByReflection(string viewName)
        {
            try
            {
                var viewType = GetViewType();
                if (viewType == null)
                {
                    // Debug.LogError("[ViewMonitorInspector] 无法找到View类型");
                    return;
                }
                
                var viewParameterType = GetViewParameterType();
                var closeMethod = viewType.GetMethod("Close", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null, new[] { typeof(string), viewParameterType }, null);
                
                closeMethod?.Invoke(null, new object[] { viewName, null });
                
                // 延迟刷新数据
                EditorApplication.delayCall += () =>
                {
                    RefreshViewDataFromReflection();
                    Repaint();
                };
            }
            catch (System.Exception e)
            {
                // Debug.LogError($"[ViewMonitorInspector] 关闭View失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 通过反射显示指定View
        /// </summary>
        private void ShowViewByReflection(string viewName)
        {
            try
            {
                var viewType = GetViewType();
                if (viewType == null)
                {
                    // Debug.LogError("[ViewMonitorInspector] 无法找到View类型");
                    return;
                }
                
                var viewParameterType = GetViewParameterType();
                var openAsyncMethod = viewType.GetMethod("OpenAsync", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null, new[] { typeof(string), viewParameterType }, null);
                
                openAsyncMethod?.Invoke(null, new object[] { viewName, null });
                
                // 延迟刷新数据
                EditorApplication.delayCall += () =>
                {
                    RefreshViewDataFromReflection();
                    Repaint();
                };
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ViewMonitorInspector] 显示View失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 通过反射关闭所有可见View
        /// </summary>
        private void CloseAllVisibleViewsByReflection()
        {
            var visibleViews = _viewMonitor.GetCachedVisibleViews();
            foreach (var view in visibleViews)
            {
                CloseViewByReflection(view.Name);
            }
        }
        
        #endregion
    }
}