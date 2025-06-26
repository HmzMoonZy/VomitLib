using System.Collections.Generic;
using System.Linq;
using Twenty2.VomitLib.View;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.View
{
    /// <summary>
    /// ViewRoot的自定义Inspector - 运行时View调试面板
    /// </summary>
    [CustomEditor(typeof(ViewRoot))]
    public class ViewRootInspector : UnityEditor.Editor
    {
        #region Fields
        
        private ViewRoot _viewRoot;
        private bool _showVisibleViews = true;
        private bool _showHiddenViews = true;
        private bool _showPreloadedViews = true;
        private bool _autoRefresh = true;
        private float _refreshInterval = 0.5f;
        private double _lastRefreshTime;
        
        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _viewItemStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _buttonStyle;
        
        // 颜色
        private readonly Color _visibleColor = new Color(0.4f, 0.8f, 0.4f, 0.3f);
        private readonly Color _hiddenColor = new Color(0.8f, 0.8f, 0.4f, 0.3f);
        private readonly Color _preloadColor = new Color(0.4f, 0.4f, 0.8f, 0.3f);
        
        #endregion
        
        #region Unity Callbacks
        
        private void OnEnable()
        {
            _viewRoot = (ViewRoot)target;
            InitializeStyles();
        }
        
        public override void OnInspectorGUI()
        {
            // 绘制默认Inspector（Camera和HiddenCanvas）
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            // 只在运行时显示View调试信息
            if (Application.isPlaying)
            {
                DrawViewDebugPanel();
            }
            else
            {
                DrawPlayModeInfo();
            }
            
            // 自动刷新
            if (_autoRefresh && Application.isPlaying && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }
        
        #endregion
        
        #region GUI Drawing
        
        /// <summary>
        /// 绘制非运行时提示
        /// </summary>
        private void DrawPlayModeInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎮 View调试面板", _headerStyle);
            EditorGUILayout.LabelField("请在运行时查看View系统调试信息", _infoStyle);
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
            
            // 可见的View
            DrawViewSection("👁️ 可见的View", _showVisibleViews, GetVisibleViews(), _visibleColor, 
                show => _showVisibleViews = show);
            
            // 隐藏的View
            DrawViewSection("👻 隐藏的View", _showHiddenViews, GetHiddenViews(), _hiddenColor,
                show => _showHiddenViews = show);
            
            // 预加载的View（仅在有时显示）
            var preloadedViews = GetPreloadedViews();
            if (preloadedViews.Count > 0)
            {
                DrawViewSection("📦 预加载的View", _showPreloadedViews, preloadedViews, _preloadColor,
                    show => _showPreloadedViews = show);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制面板头部
        /// </summary>
        private void DrawPanelHeader()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("🔍 View系统调试面板", _headerStyle);
            
            GUILayout.FlexibleSpace();
            
            // 自动刷新切换
            EditorGUI.BeginChangeCheck();
            _autoRefresh = EditorGUILayout.Toggle("自动刷新", _autoRefresh, GUILayout.Width(80));
            if (EditorGUI.EndChangeCheck() && _autoRefresh)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }
            
            // 刷新间隔
            if (_autoRefresh)
            {
                _refreshInterval = EditorGUILayout.Slider(_refreshInterval, 0.1f, 2f, GUILayout.Width(100));
            }
            
            // 手动刷新按钮
            if (GUILayout.Button("🔄", _buttonStyle, GUILayout.Width(30)))
            {
                Repaint();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制View分组
        /// </summary>
        private void DrawViewSection(string title, bool isExpanded, List<ViewInfo> views, Color bgColor, System.Action<bool> onToggle)
        {
            EditorGUILayout.Space(3);
            
            // 分组头部
            EditorGUILayout.BeginHorizontal();
            bool newExpanded = EditorGUILayout.Foldout(isExpanded, $"{title} ({views.Count})", true);
            if (newExpanded != isExpanded)
            {
                onToggle(newExpanded);
            }
            
            // 全部关闭按钮（仅对可见View显示）
            if (isExpanded && views.Count > 0 && title.Contains("可见"))
            {
                if (GUILayout.Button("全部关闭", _buttonStyle, GUILayout.Width(60)))
                {
                    CloseAllVisibleViews();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 分组内容
            if (isExpanded)
            {
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
            
            // 显示属性标签（预加载、缓存、遮罩）
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
                    CloseView(viewInfo.Name);
                }
            }
            else if (viewInfo.IsHidden)
            {
                if (GUILayout.Button("显示", _buttonStyle))
                {
                    ShowView(viewInfo.Name);
                }
            }
            else if (viewInfo.IsPreloaded)
            {
                if (GUILayout.Button("显示", _buttonStyle))
                {
                    ShowView(viewInfo.Name);
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
        
        #region Data Collection
        
        /// <summary>
        /// 获取可见View信息
        /// </summary>
        private List<ViewInfo> GetVisibleViews()
        {
            var views = new List<ViewInfo>();
            
            try
            {
                var visibleViewMapField = typeof(Twenty2.VomitLib.View.View).GetField("_visibleViewMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (visibleViewMapField?.GetValue(null) is Dictionary<string, ViewLogic> visibleViews)
                {
                    // 获取预加载View列表用于检查属性
                    var preloadedViewNames = GetPreloadedViewNames();
                    
                    foreach (var kvp in visibleViews)
                    {
                        var viewLogic = kvp.Value;
                        if (viewLogic != null)
                        {
                            views.Add(new ViewInfo
                            {
                                Name = kvp.Key,
                                SortOrder = viewLogic.ViewCanvas?.sortingOrder ?? 0,
                                IsVisible = true,
                                IsPreloaded = preloadedViewNames.Contains(kvp.Key),
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
                Debug.LogWarning($"获取可见View信息失败: {e.Message}");
            }
            
            return views;
        }
        
        /// <summary>
        /// 获取隐藏View信息
        /// </summary>
        private List<ViewInfo> GetHiddenViews()
        {
            var views = new List<ViewInfo>();
            
            try
            {
                var hiddenViewMapField = typeof(Twenty2.VomitLib.View.View).GetField("_hiddenViewMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (hiddenViewMapField?.GetValue(null) is Dictionary<string, ViewLogic> hiddenViews)
                {
                    // 获取预加载View列表用于检查属性
                    var preloadedViewNames = GetPreloadedViewNames();
                    
                    foreach (var kvp in hiddenViews)
                    {
                        var viewLogic = kvp.Value;
                        if (viewLogic != null)
                        {
                            views.Add(new ViewInfo
                            {
                                Name = kvp.Key,
                                SortOrder = viewLogic.ViewCanvas?.sortingOrder ?? 0,
                                IsHidden = true,
                                IsPreloaded = preloadedViewNames.Contains(kvp.Key),
                                IsCache = viewLogic.Config?.IsCache ?? false,
                                GameObject = viewLogic.gameObject
                            });
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"获取隐藏View信息失败: {e.Message}");
            }
            
            return views;
        }
        
        /// <summary>
        /// 获取预加载View信息（仅未显示/隐藏的）
        /// </summary>
        private List<ViewInfo> GetPreloadedViews()
        {
            var views = new List<ViewInfo>();
            
            try
            {
                var preLoadMapField = typeof(Twenty2.VomitLib.View.View).GetField("_preLoadMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                if (preLoadMapField?.GetValue(null) is Dictionary<string, GameObject> preloadedViews)
                {
                    // 获取已显示/隐藏的View名称
                    var activeViewNames = GetActiveViewNames();
                    
                    foreach (var kvp in preloadedViews)
                    {
                        // 只显示那些不在可见/隐藏列表中的预加载View
                        if (!activeViewNames.Contains(kvp.Key))
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
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"获取预加载View信息失败: {e.Message}");
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
        
        /// <summary>
        /// 获取所有活跃的View名称（可见+隐藏）
        /// </summary>
        private HashSet<string> GetActiveViewNames()
        {
            var names = new HashSet<string>();
            
            try
            {
                // 获取可见View名称
                var visibleViewMapField = typeof(Twenty2.VomitLib.View.View).GetField("_visibleViewMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (visibleViewMapField?.GetValue(null) is Dictionary<string, ViewLogic> visibleViews)
                {
                    foreach (var name in visibleViews.Keys)
                        names.Add(name);
                }
                
                // 获取隐藏View名称
                var hiddenViewMapField = typeof(Twenty2.VomitLib.View.View).GetField("_hiddenViewMap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (hiddenViewMapField?.GetValue(null) is Dictionary<string, ViewLogic> hiddenViews)
                {
                    foreach (var name in hiddenViews.Keys)
                        names.Add(name);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"获取活跃View名称失败: {e.Message}");
            }
            
            return names;
        }
        
        #endregion
        
        #region Operations
        
        /// <summary>
        /// 关闭指定View
        /// </summary>
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
                Debug.LogError($"关闭View失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 显示指定View
        /// </summary>
        private void ShowView(string viewName)
        {
            try
            {
                // View系统使用OpenAsync方法，我们需要通过反射调用
                var openAsyncMethod = typeof(Twenty2.VomitLib.View.View).GetMethod("OpenAsync", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null, new[] { typeof(string), typeof(ViewParameterBase) }, null);
                
                if (openAsyncMethod != null)
                {
                    openAsyncMethod.Invoke(null, new object[] { viewName, null });
                }
                else
                {
                    Debug.LogWarning("未找到OpenAsync方法，尝试使用反射调用私有方法");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"显示View失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 关闭所有可见View
        /// </summary>
        private void CloseAllVisibleViews()
        {
            var visibleViews = GetVisibleViews();
            foreach (var view in visibleViews)
            {
                CloseView(view.Name);
            }
        }
        
        #endregion
        
        #region Styles
        
        /// <summary>
        /// 初始化样式
        /// </summary>
        private void InitializeStyles()
        {
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
        }
        
        #endregion
        
        #region View Info Class
        
        /// <summary>
        /// View信息数据类
        /// </summary>
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
        
        #endregion
    }
}