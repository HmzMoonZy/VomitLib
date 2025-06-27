using System.Linq;
using Twenty2.VomitLib.Monitor;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// ViewMonitor专用Editor - 美化的View系统监控面板
    /// </summary>
    [CustomEditor(typeof(ViewMonitor))]
    public class ViewMonitorEditor : UnityEditor.Editor
    {
        private ViewMonitor _monitor;
        
        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _successStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _boldStyle;
        private GUIStyle _smallStyle;
        
        // 状态信息
        private ViewMonitor.ViewDebugInfo _lastDebugInfo;
        private bool _showOpenedViews = true;
        private bool _showPreloadedViews = false;
        private bool _showLoadingViews = false;
        
        // 高级显示选项
        private bool _showDetailedInfo = true;
        private bool _showViewConfig = true;
        private bool _showPerformanceMetrics = true;
        private Vector2 _scrollPosition;
        private string _filterText = "";
        
        // 选中的View详情
        private ViewMonitor.ViewDetailInfo _selectedViewDetail;
        
        private void OnEnable()
        {
            _monitor = target as ViewMonitor;
            InitializeStyles();
        }
        
        public override void OnInspectorGUI()
        {
            if (_headerStyle == null) InitializeStyles();
            
            // 更新调试信息
            if (Application.isPlaying)
            {
                _lastDebugInfo = _monitor.GetDebugInfo();
            }
            
            // 使用滚动视图
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawViewMonitorInspector();
            
            EditorGUILayout.EndScrollView();
            
            // 自动刷新
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
        
        /// <summary>
        /// 绘制View监控Inspector
        /// </summary>
        private void DrawViewMonitorInspector()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            // 标题
            EditorGUILayout.LabelField("View 系统监控", _headerStyle);
            EditorGUILayout.LabelField("高级View管理与实时监控工具", _infoStyle);
            
            EditorGUILayout.Space(10);
            
            // 显示选项控制
            DrawDisplayOptions();
            
            EditorGUILayout.Space(10);
            
            if (!Application.isPlaying)
            {
                DrawEditModeInfo();
            }
            else if (_lastDebugInfo?.IsValid == true)
            {
                DrawRuntimeInfo();
                
                if (_showDetailedInfo)
                {
                    EditorGUILayout.Space(10);
                    DrawDetailedViewLists();
                }
            }
            else
            {
                DrawErrorInfo();
            }
            
            EditorGUILayout.Space(10);
            DrawAdvancedControlButtons();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制编辑模式信息
        /// </summary>
        private void DrawEditModeInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("编辑模式", _centerStyle);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("请进入Play模式查看View系统监控信息", _infoStyle);
            
            if (GUILayout.Button("进入Play模式", GUILayout.Height(30)))
            {
                EditorApplication.isPlaying = true;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制运行时信息
        /// </summary>
        private void DrawRuntimeInfo()
        {
            // 统计概览
            DrawStatisticsOverview();
            
            EditorGUILayout.Space(8);
            
            // View列表
            DrawViewLists();
            
            EditorGUILayout.Space(8);
            
            // 性能指标
            DrawPerformanceMetrics();
        }
        
        /// <summary>
        /// 绘制统计概览
        /// </summary>
        private void DrawStatisticsOverview()
        {
            EditorGUILayout.LabelField("统计概览", _subHeaderStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            // 可见View数量
            var openedStyle = _lastDebugInfo.OpenedCount > 0 ? _successStyle : _infoStyle;
            DrawStatusCard("可见View", _lastDebugInfo.OpenedCount.ToString(), openedStyle);
            
            // 预加载View数量
            var preloadedStyle = _lastDebugInfo.PreloadedCount > 0 ? _successStyle : _infoStyle;
            DrawStatusCard("预加载", _lastDebugInfo.PreloadedCount.ToString(), preloadedStyle);
            
            // 隐藏View数量
            var hiddenStyle = _lastDebugInfo.LoadingCount > 0 ? _infoStyle : _infoStyle;
            DrawStatusCard("隐藏View", _lastDebugInfo.LoadingCount.ToString(), hiddenStyle);
            
            EditorGUILayout.EndHorizontal();
            
            // 总计和状态
            EditorGUILayout.BeginHorizontal();
            
            var totalCount = _lastDebugInfo.OpenedCount + _lastDebugInfo.PreloadedCount + _lastDebugInfo.LoadingCount;
            DrawStatusCard("总计", totalCount.ToString(), _boldStyle);
            
            var statusText = totalCount > 0 ? "ACTIVE" : "IDLE";
            var statusStyle = totalCount > 0 ? _successStyle : _infoStyle;
            DrawStatusCard("系统状态", statusText, statusStyle);
            
            EditorGUILayout.EndHorizontal();
            
            // 内存使用提示
            if (_lastDebugInfo.OpenedCount > 5)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("提示: 可见的View较多，注意内存使用", _warningStyle);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制View列表
        /// </summary>
        private void DrawViewLists()
        {
            EditorGUILayout.LabelField("View详情", _subHeaderStyle);
            
            // 可见的View
            DrawViewListSection("可见的View", _lastDebugInfo.OpenedCount, ref _showOpenedViews, 
                () => GetOpenedViewNames(_lastDebugInfo.OpenedViews), _successStyle);
            
            EditorGUILayout.Space(5);
            
            // 预加载的View
            DrawViewListSection("预加载的View", _lastDebugInfo.PreloadedCount, ref _showPreloadedViews,
                () => GetViewNames(_lastDebugInfo.PreloadedViews, "预加载View"), _infoStyle);
            
            EditorGUILayout.Space(5);
            
            // 隐藏的View
            DrawViewListSection("隐藏的View", _lastDebugInfo.LoadingCount, ref _showLoadingViews,
                () => GetViewNames(_lastDebugInfo.LoadingViews, "隐藏View"), _infoStyle);
        }
        
        /// <summary>
        /// 绘制View列表节
        /// </summary>
        private void DrawViewListSection(string title, int count, ref bool foldout, System.Func<string[]> getNamesFunc, GUIStyle countStyle)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            foldout = EditorGUILayout.Foldout(foldout, $"{title} ({count})", true);
            EditorGUILayout.LabelField(count.ToString(), countStyle, GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();
            
            if (foldout && count > 0)
            {
                EditorGUILayout.Space(3);
                
                var names = getNamesFunc();
                for (int i = 0; i < names.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(15);
                    EditorGUILayout.LabelField($"• {names[i]}", _smallStyle);
                    EditorGUILayout.EndHorizontal();
                }
                
                if (names.Length == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(15);
                    EditorGUILayout.LabelField("暂无数据", _infoStyle);
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制性能指标
        /// </summary>
        private void DrawPerformanceMetrics()
        {
            EditorGUILayout.LabelField("性能指标", _subHeaderStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            // 最后更新时间
            var lastUpdateText = $"{System.DateTime.Now:HH:mm:ss}";
            DrawStatusCard("最后更新", lastUpdateText, _infoStyle);
            
            // 监控频率
            DrawStatusCard("刷新间隔", "1.0s", _infoStyle);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            // ViewRoot状态
            var viewRootStatus = _lastDebugInfo != null ? "已连接" : "未连接";
            var viewRootStyle = _lastDebugInfo != null ? _successStyle : _errorStyle;
            DrawStatusCard("ViewRoot", viewRootStatus, viewRootStyle);
            
            // 监控状态
            DrawStatusCard("监控状态", "运行中", _successStyle);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制错误信息
        /// </summary>
        private void DrawErrorInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("监控错误", _errorStyle);
            EditorGUILayout.Space(5);
            
            var errorMsg = _lastDebugInfo?.ErrorMessage ?? "获取View系统信息失败";
            EditorGUILayout.LabelField($"错误信息: {errorMsg}", _errorStyle);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("可能的原因:", _boldStyle);
            EditorGUILayout.LabelField("• ViewRoot组件尚未初始化", _infoStyle);
            EditorGUILayout.LabelField("• ViewLogic系统未启动", _infoStyle);
            EditorGUILayout.LabelField("• 反射访问权限问题", _infoStyle);
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制显示选项
        /// </summary>
        private void DrawDisplayOptions()
        {
            EditorGUILayout.LabelField("显示选项", _subHeaderStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            _showDetailedInfo = EditorGUILayout.ToggleLeft("显示详细信息", _showDetailedInfo, GUILayout.Width(100));
            _showViewConfig = EditorGUILayout.ToggleLeft("ViewConfig", _showViewConfig, GUILayout.Width(100));
            _showPerformanceMetrics = EditorGUILayout.ToggleLeft("性能指标", _showPerformanceMetrics, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 过滤器
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("过滤器:", GUILayout.Width(50));
            _filterText = EditorGUILayout.TextField(_filterText);
            if (GUILayout.Button("清除", GUILayout.Width(40)))
            {
                _filterText = "";
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制详细View列表
        /// </summary>
        private void DrawDetailedViewLists()
        {
            EditorGUILayout.LabelField("详细View信息", _subHeaderStyle);
            
            // 可见的View
            DrawDetailedViewSection("可见的View", _lastDebugInfo.OpenedViewDetails, ref _showOpenedViews, _successStyle, true);
            
            EditorGUILayout.Space(5);
            
            // 预加载的View
            DrawDetailedViewSection("预加载的View", _lastDebugInfo.PreloadedViewDetails, ref _showPreloadedViews, _infoStyle, false);
            
            EditorGUILayout.Space(5);
            
            // 隐藏的View
            DrawDetailedViewSection("隐藏的View", _lastDebugInfo.LoadingViewDetails, ref _showLoadingViews, _infoStyle, true);
        }
        
        /// <summary>
        /// 绘制详细View节
        /// </summary>
        private void DrawDetailedViewSection(string title, System.Collections.Generic.List<ViewMonitor.ViewDetailInfo> viewDetails, ref bool foldout, GUIStyle countStyle, bool showControls)
        {
            if (viewDetails == null) return;
            
            // 应用过滤器
            var filteredDetails = viewDetails;
            if (!string.IsNullOrEmpty(_filterText))
            {
                filteredDetails = viewDetails.Where(d => d.ViewName.ToLower().Contains(_filterText.ToLower())).ToList();
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            foldout = EditorGUILayout.Foldout(foldout, $"{title} ({filteredDetails.Count})", true);
            EditorGUILayout.LabelField(filteredDetails.Count.ToString(), countStyle, GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();
            
            if (foldout && filteredDetails.Count > 0)
            {
                EditorGUILayout.Space(3);
                
                foreach (var detail in filteredDetails)
                {
                    DrawViewDetailItem(detail, showControls);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制View详情项
        /// </summary>
        private void DrawViewDetailItem(ViewMonitor.ViewDetailInfo detail, bool showControls)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // View名称和类型
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"• {detail.ViewName}", _boldStyle, GUILayout.Width(150));
            EditorGUILayout.LabelField($"[{detail.ViewTypeName}]", _smallStyle);
            
            if (showControls && Application.isPlaying)
            {
                if (GUILayout.Button("关闭", GUILayout.Width(40)))
                {
                    _monitor.CloseView(detail.ViewName);
                }
                if (GUILayout.Button("冻结", GUILayout.Width(40)))
                {
                    _monitor.FreezeView(detail.ViewName);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // ViewConfig信息
            if (_showViewConfig && detail.ViewLogicInstance != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15);
                EditorGUILayout.BeginVertical();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Layer: {detail.Layer}", _smallStyle, GUILayout.Width(80));
                EditorGUILayout.LabelField($"Sort: {detail.SortOrder}", _smallStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField($"Cache: {(detail.IsCache ? "✓" : "✗")}", _smallStyle, GUILayout.Width(50));
                EditorGUILayout.LabelField($"Mask: {(detail.EnableAutoMask ? "✓" : "✗")}", _smallStyle, GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"AutoBind: {(detail.AutoBindButtons ? "✓" : "✗")}", _smallStyle, GUILayout.Width(80));
                EditorGUILayout.LabelField($"L10N: {(detail.EnableLocalization ? "✓" : "✗")}", _smallStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField($"Record: {(detail.RecordOpen ? "✓" : "✗")}", _smallStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField($"Size: {detail.CanvasSize}", _smallStyle);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制高级控制按钮
        /// </summary>
        private void DrawAdvancedControlButtons()
        {
            EditorGUILayout.LabelField("控制操作", _subHeaderStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("手动刷新", GUILayout.Height(25)))
            {
                _monitor.ManualRefresh();
            }
            
            if (GUILayout.Button("展开所有", GUILayout.Height(25)))
            {
                _showOpenedViews = true;
                _showPreloadedViews = true;
                _showLoadingViews = true;
            }
            
            if (GUILayout.Button("折叠所有", GUILayout.Height(25)))
            {
                _showOpenedViews = false;
                _showPreloadedViews = false;
                _showLoadingViews = false;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (Application.isPlaying && GUILayout.Button("关闭所有View", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要关闭所有可见的View吗？", "确定", "取消"))
                {
                    if (_lastDebugInfo?.OpenedViewDetails != null)
                    {
                        foreach (var detail in _lastDebugInfo.OpenedViewDetails)
                        {
                            _monitor.CloseView(detail.ViewName);
                        }
                    }
                }
            }
            
            if (Application.isPlaying && GUILayout.Button("选择VomitMonitor", GUILayout.Height(25)))
            {
                var vomitMonitor = VomitMonitorManager.VomitMonitor;
                if (vomitMonitor != null)
                {
                    Selection.activeGameObject = vomitMonitor.gameObject;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制状态卡片
        /// </summary>
        private void DrawStatusCard(string label, string value, GUIStyle valueStyle)
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(70));
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(value, valueStyle);
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }
        
        /// <summary>
        /// 获取已打开的View名称
        /// </summary>
        private string[] GetOpenedViewNames(System.Collections.IDictionary openedViews)
        {
            try
            {
                if (openedViews == null) return new string[0];
                
                var names = new System.Collections.Generic.List<string>();
                foreach (var key in openedViews.Keys)
                {
                    names.Add(key.ToString());
                }
                return names.ToArray();
            }
            catch
            {
                return new string[] { "获取View名称失败" };
            }
        }
        
        /// <summary>
        /// 获取View名称（通用方法）
        /// </summary>
        private string[] GetViewNames(System.Collections.IDictionary viewCollection, string fallbackPrefix)
        {
            try
            {
                if (viewCollection == null) return new string[0];
                
                var count = viewCollection.Count;
                if (count == 0) return new string[0];
                
                var names = new System.Collections.Generic.List<string>();
                
                // 尝试获取实际的key名称
                foreach (var key in viewCollection.Keys)
                {
                    names.Add(key.ToString());
                    if (names.Count >= 10) break; // 最多显示10个
                }
                
                if (count > 10)
                {
                    names.Add($"... 还有 {count - 10} 个");
                }
                
                return names.ToArray();
            }
            catch
            {
                return new string[] { "获取信息失败" };
            }
        }
        
        /// <summary>
        /// 初始化样式
        /// </summary>
        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black },
                alignment = TextAnchor.MiddleCenter
            };
            
            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.cyan : Color.blue }
            };
            
            _infoStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.gray : Color.black }
            };
            
            _successStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green }
            };
            
            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.6f, 0f) } // Orange
            };
            
            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.red }
            };
            
            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(15, 15, 10, 10)
            };
            
            _centerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            _boldStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            _smallStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.gray : Color.black }
            };
        }
    }
}