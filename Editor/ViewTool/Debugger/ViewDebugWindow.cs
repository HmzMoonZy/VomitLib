using System.Collections.Generic;
using System.Linq;
using Twenty2.VomitLib.View;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.View
{
    /// <summary>
    /// View系统调试窗口 - 独立的EditorWindow版本
    /// 提供更丰富的调试功能和更好的用户体验
    /// </summary>
    public class ViewDebugWindow : EditorWindow
    {
        #region Menu
        
        [MenuItem("VomitLib/View/Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<ViewDebugWindow>("View调试器");
            window.minSize = new Vector2(600, 500);
            window.maxSize = new Vector2(1200, 1000);
            window.autoRepaintOnSceneChange = true;
            
            // 设置初始尺寸
            var rect = window.position;
            if (rect.width < window.minSize.x || rect.height < window.minSize.y)
            {
                rect.width = Mathf.Max(rect.width, window.minSize.x);
                rect.height = Mathf.Max(rect.height, window.minSize.y);
                window.position = rect;
            }
            
            window.Show();
        }
        
        #endregion
        
        #region Fields
        
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private float _refreshInterval = 0.5f;
        private double _lastRefreshTime;
        private string _searchFilter = "";
        private ViewFilterType _filterType = ViewFilterType.All;
        
        // 折叠状态
        private bool _showVisibleViews = true;
        private bool _showHiddenViews = true;
        private bool _showPreloadedViews = true;
        private bool _showStatistics = true;
        
        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _viewItemStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _successStyle;
        
        // 数据缓存
        private List<ViewInfo> _allViews = new List<ViewInfo>();
        private ViewStatistics _statistics;
        
        #endregion
        
        #region Unity Callbacks
        
        private void OnEnable()
        {
            InitializeStyles();
            RefreshData();
        }
        
        private void OnGUI()
        {
            // 动态调整窗口尺寸
            HandleWindowResize();
            
            if (!Application.isPlaying)
            {
                DrawNotPlayingMessage();
                return;
            }
            
            // 自动刷新逻辑
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                RefreshData();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
            
            DrawToolbar();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawStatistics();
            DrawViewSections();
            
            EditorGUILayout.EndScrollView();
        }
        
        #endregion
        
        #region GUI Drawing
        
        /// <summary>
        /// 绘制非运行时消息
        /// </summary>
        private void DrawNotPlayingMessage()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(300), GUILayout.Height(150));
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.LabelField("🎮 View调试器", _headerStyle, GUILayout.Height(30));
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("请进入Play模式以查看View系统调试信息", _infoStyle);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("进入Play模式", GUILayout.Height(30)))
            {
                EditorApplication.isPlaying = true;
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.toolbar);
            
            // 第一行：标题和主要控制
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍 View系统调试器", _headerStyle);
            
            GUILayout.FlexibleSpace();
            
            // 刷新控制
            EditorGUI.BeginChangeCheck();
            _autoRefresh = EditorGUILayout.Toggle("自动刷新", _autoRefresh, GUILayout.Width(80));
            if (EditorGUI.EndChangeCheck() && _autoRefresh)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }
            
            if (_autoRefresh)
            {
                _refreshInterval = EditorGUILayout.Slider(_refreshInterval, 0.1f, 3f, GUILayout.Width(100));
                EditorGUILayout.LabelField("s", GUILayout.Width(15));
            }
            
            if (GUILayout.Button("🔄 刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshData();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
            
            // 第二行：搜索和过滤
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            
            EditorGUILayout.LabelField("过滤:", GUILayout.Width(40));
            _filterType = (ViewFilterType)EditorGUILayout.EnumPopup(_filterType, EditorStyles.toolbarPopup, GUILayout.Width(100));
            
            GUILayout.FlexibleSpace();
            
            // 全局操作按钮
            if (GUILayout.Button("关闭所有View", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                CloseAllViews();
            }
            
            if (GUILayout.Button("清理缓存", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ClearViewCache();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// 绘制统计信息
        /// </summary>
        private void DrawStatistics()
        {
            _showStatistics = EditorGUILayout.Foldout(_showStatistics, "📊 统计信息", true);
            if (!_showStatistics) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            DrawStatItem("可见View", _statistics.VisibleCount, _successStyle);
            DrawStatItem("隐藏View", _statistics.HiddenCount, _warningStyle);
            DrawStatItem("预加载View", _statistics.PreloadedCount, _infoStyle);
            DrawStatItem("总计", _statistics.TotalCount, _headerStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            DrawStatItem("缓存View", _statistics.CachedCount, _infoStyle);
            DrawStatItem("有遮罩View", _statistics.MaskedCount, _infoStyle);
            DrawStatItem("最高层级", _statistics.MaxSortOrder, _infoStyle);
            DrawStatItem("平均层级", _statistics.AvgSortOrder.ToString("F1"), _infoStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(15);
        }
        
        /// <summary>
        /// 绘制统计项
        /// </summary>
        private void DrawStatItem(string label, object value, GUIStyle style)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(value.ToString(), style ?? _headerStyle);
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }
        
        /// <summary>
        /// 绘制View分组
        /// </summary>
        private void DrawViewSections()
        {
            var filteredViews = FilterViews(_allViews);
            
            var visibleViews = filteredViews.Where(v => v.IsVisible).ToList();
            var hiddenViews = filteredViews.Where(v => v.IsHidden).ToList();
            var preloadedViews = filteredViews.Where(v => v.IsPreloaded).ToList();
            
            DrawViewSection("👁️ 可见的View", ref _showVisibleViews, visibleViews, Color.green, true);
            DrawViewSection("👻 隐藏的View", ref _showHiddenViews, hiddenViews, Color.yellow, true);
            
            // 仅在有预加载View时显示
            if (preloadedViews.Count > 0)
            {
                DrawViewSection("📦 预加载的View", ref _showPreloadedViews, preloadedViews, Color.cyan, false);
            }
        }
        
        /// <summary>
        /// 绘制View分组
        /// </summary>
        private void DrawViewSection(string title, ref bool isExpanded, List<ViewInfo> views, Color color, bool showCloseAllButton)
        {
            EditorGUILayout.BeginHorizontal();
            isExpanded = EditorGUILayout.Foldout(isExpanded, $"{title} ({views.Count})", true, _subHeaderStyle);
            
            GUILayout.FlexibleSpace();
            
            if (showCloseAllButton && views.Count > 0)
            {
                if (GUILayout.Button("全部关闭", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    foreach (var view in views)
                    {
                        CloseView(view.Name);
                    }
                    RefreshData();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (!isExpanded) return;
            
            if (views.Count == 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("  (无)", _infoStyle);
                EditorGUILayout.EndVertical();
            }
            else
            {
                foreach (var view in views.OrderByDescending(v => v.SortOrder).ThenBy(v => v.Name))
                {
                    DrawViewItem(view, color);
                }
            }
            
            EditorGUILayout.Space(3);
        }
        
        /// <summary>
        /// 绘制单个View项
        /// </summary>
        private void DrawViewItem(ViewInfo view, Color color)
        {
            var originalBgColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(color.r, color.g, color.b, 0.2f);
            
            EditorGUILayout.BeginVertical(_viewItemStyle);
            GUI.backgroundColor = originalBgColor;
            
            EditorGUILayout.BeginHorizontal();
            
            // View图标和名称
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            
            // 状态图标和名称
            string statusIcon = view.IsVisible ? "👁️" : view.IsHidden ? "👻" : "📦";
            
            EditorGUILayout.LabelField(statusIcon, GUILayout.Width(20));
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(view.Name, EditorStyles.boldLabel);
            
            // 显示属性标签（预加载、缓存、遮罩）
            if (view.IsPreloaded)
            {
                var originalColor = GUI.contentColor;
                GUI.contentColor = Color.cyan;
                EditorGUILayout.LabelField("[预加载]", EditorStyles.miniLabel, GUILayout.Width(50));
                GUI.contentColor = originalColor;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 特殊标记
            if (view.IsCache) EditorGUILayout.LabelField("💾", GUILayout.Width(20));
            if (view.HasMask) EditorGUILayout.LabelField("🎭", GUILayout.Width(20));
            
            EditorGUILayout.EndHorizontal();
            
            // 详细信息
            var info = $"层级: {view.SortOrder}";
            if (view.IsVisible || view.IsHidden)
            {
                info += $" | 类型: {(view.GameObject?.GetComponent<ViewLogic>()?.GetType().Name ?? "Unknown")}";
                // 显示属性信息
                if (view.IsCache) info += " | 缓存: ✓";
                if (view.HasMask) info += " | 遮罩: ✓";
            }
            else if (view.IsPreloaded)
            {
                info += " | 状态: 仅预加载，未显示";
            }
            
            EditorGUILayout.LabelField(info, _infoStyle);
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            // 操作按钮组
            EditorGUILayout.BeginVertical(GUILayout.Width(80));
            
            EditorGUILayout.BeginHorizontal();
            
            // 主操作按钮
            if (view.IsVisible)
            {
                if (GUILayout.Button("关闭", EditorStyles.miniButtonLeft))
                {
                    CloseView(view.Name);
                    RefreshData();
                }
            }
            else if (view.IsHidden)
            {
                if (GUILayout.Button("显示", EditorStyles.miniButtonLeft))
                {
                    ShowView(view.Name);
                    RefreshData();
                }
            }
            else if (view.IsPreloaded)
            {
                if (GUILayout.Button("显示", EditorStyles.miniButtonLeft))
                {
                    ShowView(view.Name);
                    RefreshData();
                }
            }
            
            // 选中按钮
            if (view.GameObject != null)
            {
                if (GUILayout.Button("📍", EditorStyles.miniButtonRight, GUILayout.Width(25)))
                {
                    Selection.activeGameObject = view.GameObject;
                    EditorGUIUtility.PingObject(view.GameObject);
                }
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("📍", EditorStyles.miniButtonRight, GUILayout.Width(25));
                GUI.enabled = true;
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        #endregion
        
        #region Data Management
        
        /// <summary>
        /// 刷新数据
        /// </summary>
        private void RefreshData()
        {
            if (!Application.isPlaying) return;
            
            _allViews.Clear();
            
            // 获取所有View信息
            var visibleViews = GetVisibleViews();
            var hiddenViews = GetHiddenViews();
            var preloadedViews = GetPreloadedViews();
            
            _allViews.AddRange(visibleViews);
            _allViews.AddRange(hiddenViews);
            
            // 预加载的View只在不在可见/隐藏列表中时才添加
            var existingViewNames = _allViews.Select(v => v.Name).ToHashSet();
            var independentPreloadedViews = preloadedViews.Where(v => !existingViewNames.Contains(v.Name));
            _allViews.AddRange(independentPreloadedViews);
            
            UpdateStatistics();
        }
        
        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            _statistics = new ViewStatistics
            {
                VisibleCount = _allViews.Count(v => v.IsVisible),
                HiddenCount = _allViews.Count(v => v.IsHidden),
                PreloadedCount = _allViews.Count(v => v.IsPreloaded),
                TotalCount = _allViews.Count,
                CachedCount = _allViews.Count(v => v.IsCache),
                MaskedCount = _allViews.Count(v => v.HasMask),
                MaxSortOrder = _allViews.Count > 0 ? _allViews.Max(v => v.SortOrder) : 0,
                AvgSortOrder = _allViews.Count > 0 ? _allViews.Average(v => v.SortOrder) : 0
            };
        }
        
        /// <summary>
        /// 过滤View列表
        /// </summary>
        private List<ViewInfo> FilterViews(List<ViewInfo> views)
        {
            var filtered = views.AsEnumerable();
            
            // 文本过滤
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                filtered = filtered.Where(v => v.Name.ToLower().Contains(_searchFilter.ToLower()));
            }
            
            // 类型过滤
            filtered = _filterType switch
            {
                ViewFilterType.Visible => filtered.Where(v => v.IsVisible),
                ViewFilterType.Hidden => filtered.Where(v => v.IsHidden),
                ViewFilterType.Preloaded => filtered.Where(v => v.IsPreloaded),
                ViewFilterType.Cached => filtered.Where(v => v.IsCache),
                ViewFilterType.Masked => filtered.Where(v => v.HasMask),
                _ => filtered
            };
            
            return filtered.ToList();
        }
        
        #endregion
        
        #region View Operations
        
        // 这里复用ViewRootInspector中的方法
        private List<ViewInfo> GetVisibleViews() => GetViewsFromMap("_visibleViewMap", true, false, false);
        private List<ViewInfo> GetHiddenViews() => GetViewsFromMap("_hiddenViewMap", false, true, false);
        private List<ViewInfo> GetPreloadedViews() => GetPreloadedViewsInternal();
        
        private List<ViewInfo> GetViewsFromMap(string fieldName, bool isVisible, bool isHidden, bool isPreloaded)
        {
            var views = new List<ViewInfo>();
            
            try
            {
                var field = typeof(Twenty2.VomitLib.View.View).GetField(fieldName, 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (field?.GetValue(null) is Dictionary<string, ViewLogic> viewMap)
                {
                    // 获取预加载View列表用于检查属性
                    var preloadedViewNames = GetPreloadedViewNames();
                    
                    foreach (var kvp in viewMap)
                    {
                        var viewLogic = kvp.Value;
                        if (viewLogic != null)
                        {
                            views.Add(new ViewInfo
                            {
                                Name = kvp.Key,
                                SortOrder = viewLogic.ViewCanvas?.sortingOrder ?? 0,
                                IsVisible = isVisible,
                                IsHidden = isHidden,
                                IsPreloaded = preloadedViewNames.Contains(kvp.Key), // 检查是否有预加载属性
                                IsCache = viewLogic.Config?.IsCache ?? false,
                                HasMask = viewLogic.Config?.EnableAutoMask ?? false,
                                GameObject = viewLogic.gameObject
                            });
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"获取{fieldName}信息失败: {e.Message}");
            }
            
            return views;
        }
        
        /// <summary>
        /// 获取预加载View名称列表
        /// </summary>
        private HashSet<string> GetPreloadedViewNames()
        {
            try
            {
                var preLoadMapField = typeof(Twenty2.VomitLib.View.View).GetField("_preLoadMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (preLoadMapField?.GetValue(null) is Dictionary<string, GameObject> preloadedViews)
                {
                    return preloadedViews.Keys.ToHashSet();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"获取预加载View名称失败: {e.Message}");
            }
            
            return new HashSet<string>();
        }
        
        private List<ViewInfo> GetPreloadedViewsInternal()
        {
            var views = new List<ViewInfo>();
            
            try
            {
                var preLoadMapField = typeof(Twenty2.VomitLib.View.View).GetField("_preLoadMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (preLoadMapField?.GetValue(null) is Dictionary<string, GameObject> preloadedViews)
                {
                    foreach (var kvp in preloadedViews)
                    {
                        views.Add(new ViewInfo
                        {
                            Name = kvp.Key,
                            SortOrder = 0,
                            IsPreloaded = true,
                            GameObject = kvp.Value
                        });
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"获取预加载View信息失败: {e.Message}");
            }
            
            return views;
        }
        
        private void CloseView(string viewName)
        {
            try
            {
                var closeMethod = typeof(Twenty2.VomitLib.View.View).GetMethod("Close", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null, new[] { typeof(string), typeof(ViewParameterBase) }, null);
                
                closeMethod?.Invoke(null, new object[] { viewName, null });
            }
            catch (System.Exception e)
            {
                Debug.LogError($"关闭View失赅: {e.Message}");
            }
        }
        
        private void ShowView(string viewName)
        {
            try
            {
                // View系统使用OpenAsync方法
                var openAsyncMethod = typeof(Twenty2.VomitLib.View.View).GetMethod("OpenAsync", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null, new[] { typeof(string), typeof(ViewParameterBase) }, null);
                
                if (openAsyncMethod != null)
                {
                    openAsyncMethod.Invoke(null, new object[] { viewName, null });
                }
                else
                {
                    Debug.LogWarning("未找到OpenAsync方法");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"显示View失败: {e.Message}");
            }
        }
        
        private void CloseAllViews()
        {
            var visibleViews = _allViews.Where(v => v.IsVisible).ToList();
            foreach (var view in visibleViews)
            {
                CloseView(view.Name);
            }
            RefreshData();
        }
        
        private void ClearViewCache()
        {
            try
            {
                // 这里需要根据实际的缓存清理API来实现
                Debug.Log("清理View缓存（功能待实现）");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"清理缓存失败: {e.Message}");
            }
        }
        
        #endregion
        
        #region Styles
        
        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black },
                alignment = TextAnchor.MiddleLeft
            };
            
            _subHeaderStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            
            _viewItemStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(0, 0, 2, 2)
            };
            
            _infoStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.gray : Color.black }
            };
            
            _warningStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.yellow }
            };
            
            _successStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.green }
            };
        }
        
        /// <summary>
        /// 处理窗口尺寸调整
        /// </summary>
        private void HandleWindowResize()
        {
            var rect = position;
            bool needsResize = false;
            
            // 检查最小尺寸
            if (rect.width < minSize.x)
            {
                rect.width = minSize.x;
                needsResize = true;
            }
            if (rect.height < minSize.y)
            {
                rect.height = minSize.y;
                needsResize = true;
            }
            
            // 检查最大尺寸
            if (rect.width > maxSize.x)
            {
                rect.width = maxSize.x;
                needsResize = true;
            }
            if (rect.height > maxSize.y)
            {
                rect.height = maxSize.y;
                needsResize = true;
            }
            
            if (needsResize)
            {
                position = rect;
            }
        }
        
        #endregion
        
        #region Data Classes
        
        private class ViewInfo
        {
            public string Name;
            public int SortOrder;
            public bool IsVisible;
            public bool IsHidden;
            public bool IsPreloaded;
            public bool IsCache;
            public bool HasMask;
            public GameObject GameObject;
        }
        
        private struct ViewStatistics
        {
            public int VisibleCount;
            public int HiddenCount;
            public int PreloadedCount;
            public int TotalCount;
            public int CachedCount;
            public int MaskedCount;
            public int MaxSortOrder;
            public double AvgSortOrder;
        }
        
        private enum ViewFilterType
        {
            All,
            Visible,
            Hidden,
            Preloaded,
            Cached,
            Masked
        }
        
        #endregion
    }
}