using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// VomitLib框架监控窗口 - 统一的框架调试和监控界面
    /// 提供View、Model、System、Event、Command、Procedure、Luban数据的实时监控和调试功能
    /// </summary>
    public class VomitMonitorWindow : EditorWindow
    {
        #region Fields

        private const string WindowTitle = "Vomit Monitor";
        private const string MenuPath = "VomitLib/Monitor";

        // 标签页
        private enum TabType
        {
            Overview,
            View,
            Model,
            System,
            Event,
            Command,
            Procedure,
            Luban,
            Settings
        }

        private TabType _currentTab = TabType.Overview;
        private readonly string[] _tabNames =
        {
            "概览", "View", "Model", "System", "Event", "Command", "Procedure", "Luban", "设置"
        };

        // 工具栏
        private readonly string[] _toolbarIcons = { "\u2261", "\uE702", "\uE8D6", "\uE790", "\uE934", "\uE8D4", "\uE8C7", "\uE8C5", "\uE713" };
        private int _toolbarSelected = 0;

        // 数据提供者
        private MonitorDataProvider _dataProvider;
        private bool _isDataProviderInitialized = false;

        // 刷新控制
        private bool _autoRefresh = true;
        private float _refreshInterval = 0.5f;
        private double _lastRefreshTime;

        // 状态指示
        private bool _isPlaying = false;
        private bool _isFrameworkInitialized = false;
        private string _statusMessage = "等待框架初始化...";

        // 样式
        internal GUIStyle _tabButtonStyle;
        internal GUIStyle _headerStyle;
        internal GUIStyle _statusStyle;
        internal GUIStyle _boxStyle;
        internal GUIStyle _infoStyle;
        internal GUIStyle _warningStyle;
        internal GUIStyle _errorStyle;
        internal GUIStyle _successStyle;

        // 滚动视图
        private Vector2 _scrollPosition;

        // 各标签页
        private ViewMonitorTab _viewTab = new ViewMonitorTab();
        private ModelMonitorTab _modelTab = new ModelMonitorTab();
        private SystemMonitorTab _systemTab = new SystemMonitorTab();
        private EventMonitorTab _eventTab = new EventMonitorTab();
        private CommandMonitorTab _commandTab = new CommandMonitorTab();
        private ProcedureMonitorTab _procedureTab = new ProcedureMonitorTab();
        private LubanMonitorTab _lubanTab = new LubanMonitorTab();

        #endregion

        #region EditorWindow Lifecycle

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<VomitMonitorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.Show();
        }

        private void OnEnable()
        {
            // 注册自动刷新
            EditorApplication.update += OnEditorUpdate;

            // 初始化样式
            InitializeStyles();

            // 初始化数据提供者
            InitializeDataProvider();

            // 开始重绘
            Repaint();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;

            // 清理数据提供者
            CleanupDataProvider();
        }

        private void OnEditorUpdate()
        {
            // 检查运行状态
            _isPlaying = Application.isPlaying;

            // 检查框架初始化状态
            CheckFrameworkStatus();

            // 自动刷新
            if (_autoRefresh && _isPlaying && _isFrameworkInitialized)
            {
                if (EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
                {
                    _lastRefreshTime = EditorApplication.timeSinceStartup;
                    RefreshData();
                    Repaint();
                }
            }
        }

        private void OnFocus()
        {
            // 窗口获得焦点时刷新
            if (_isPlaying && _isFrameworkInitialized)
            {
                RefreshData();
                Repaint();
            }
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            // 确保样式已初始化
            if (_tabButtonStyle == null)
            {
                InitializeStyles();
            }

            // 绘制顶部工具栏
            DrawToolbar();

            // 绘制状态栏
            DrawStatusBar();

            // 绘制分隔线
            EditorGUILayout.Space(2);
            DrawSeparator();

            // 绘制内容区域
            DrawContent();
        }

        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.FlexibleSpace();

            // 刷新按钮
            EditorGUI.BeginDisabledGroup(!_isPlaying || !_isFrameworkInitialized);
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                RefreshData();
                Repaint();
            }
            EditorGUI.EndDisabledGroup();

            // 自动刷新开关
            bool newAutoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(70));
            if (newAutoRefresh != _autoRefresh)
            {
                _autoRefresh = newAutoRefresh;
                EditorPrefs.SetBool("VomitMonitor.AutoRefresh", _autoRefresh);
            }

            // 刷新间隔
            EditorGUI.BeginDisabledGroup(!_autoRefresh);
            GUILayout.Label("间隔:", EditorStyles.miniLabel, GUILayout.Width(30));
            float newInterval = EditorGUILayout.FloatField(_refreshInterval, EditorStyles.toolbarTextField, GUILayout.Width(40));
            if (Math.Abs(newInterval - _refreshInterval) > 0.01f)
            {
                _refreshInterval = Mathf.Clamp(newInterval, 0.1f, 5f);
                EditorPrefs.SetFloat("VomitMonitor.RefreshInterval", _refreshInterval);
            }
            EditorGUI.EndDisabledGroup();

            // 标签页选择
            int newSelection = GUILayout.Toolbar(_toolbarSelected, _tabNames, EditorStyles.toolbarButton);
            if (newSelection != _toolbarSelected)
            {
                _toolbarSelected = newSelection;
                _currentTab = (TabType)newSelection;
                EditorPrefs.SetInt("VomitMonitor.SelectedTab", _toolbarSelected);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制状态栏
        /// </summary>
        private void DrawStatusBar()
        {
            var bgColor = GUI.backgroundColor;

            if (!_isPlaying)
            {
                GUI.backgroundColor = new Color(1f, 0.7f, 0.5f);
            }
            else if (!_isFrameworkInitialized)
            {
                GUI.backgroundColor = new Color(1f, 0.9f, 0.5f);
            }
            else
            {
                GUI.backgroundColor = new Color(0.5f, 1f, 0.7f);
            }

            EditorGUILayout.BeginHorizontal(_boxStyle);

            GUILayout.Label(_statusMessage, _statusStyle);

            GUILayout.FlexibleSpace();

            if (_isPlaying && _isFrameworkInitialized)
            {
                var lastRefresh = TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - _lastRefreshTime);
                GUILayout.Label($"上次刷新: {lastRefresh.Seconds}s前", _infoStyle);
            }

            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = bgColor;
        }

        /// <summary>
        /// 绘制内容区域
        /// </summary>
        private void DrawContent()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (!_isPlaying)
            {
                DrawNotPlayingMessage();
            }
            else if (!_isFrameworkInitialized)
            {
                DrawFrameworkNotInitializedMessage();
            }
            else
            {
                switch (_currentTab)
                {
                    case TabType.Overview:
                        DrawOverviewTab();
                        break;
                    case TabType.View:
                        DrawViewTab();
                        break;
                    case TabType.Model:
                        DrawModelTab();
                        break;
                    case TabType.System:
                        DrawSystemTab();
                        break;
                    case TabType.Event:
                        DrawEventTab();
                        break;
                    case TabType.Command:
                        DrawCommandTab();
                        break;
                    case TabType.Procedure:
                        DrawProcedureTab();
                        break;
                    case TabType.Luban:
                        DrawLubanTab();
                        break;
                    case TabType.Settings:
                        DrawSettingsTab();
                        break;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制概览标签页
        /// </summary>
        private void DrawOverviewTab()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("框架概览", _headerStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // 框架状态卡片
            DrawStatusCard("框架状态", Vomit.IsInit ? "已初始化" : "未初始化", Vomit.IsInit);
            DrawStatusCard("运行模式", Application.isPlaying ? "运行中" : "未运行", Application.isPlaying);

            EditorGUILayout.Space(5);

            // 统计数据
            if (_dataProvider != null)
            {
                var stats = _dataProvider.GetOverviewStatistics();

                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.LabelField("系统统计", _headerStyle);
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(2);

                DrawStatisticRow("View", stats.ViewCount.ToString(), stats.VisibleViewCount > 0);
                DrawStatisticRow("Model", stats.ModelCount.ToString(), stats.ModelCount > 0);
                DrawStatisticRow("System", stats.SystemCount.ToString(), stats.SystemCount > 0);
                DrawStatisticRow("Event", stats.EventCount.ToString(), stats.EventCount > 0);
                DrawStatisticRow("Command", stats.CommandCount.ToString(), stats.CommandCount > 0);
                DrawStatisticRow("Procedure", stats.ProcedureCount.ToString(), stats.ProcedureCount > 0);
                DrawStatisticRow("Luban表", stats.LubanTableCount.ToString(), stats.LubanTableCount > 0);
            }
        }

        /// <summary>
        /// 绘制View标签页
        /// </summary>
        private void DrawViewTab()
        {
            _viewTab.OnGUI(this, _dataProvider);
        }

        /// <summary>
        /// 绘制Model标签页
        /// </summary>
        private void DrawModelTab()
        {
            _modelTab.OnGUI(this, _dataProvider);
        }

        /// <summary>
        /// 绘制System标签页
        /// </summary>
        private void DrawSystemTab()
        {
            _systemTab.OnGUI(this, _dataProvider);
        }

        /// <summary>
        /// 绘制Event标签页
        /// </summary>
        private void DrawEventTab()
        {
            _eventTab.OnGUI(this, _dataProvider);
        }

        /// <summary>
        /// 绘制Command标签页
        /// </summary>
        private void DrawCommandTab()
        {
            _commandTab.OnGUI(this, _dataProvider);
        }

        /// <summary>
        /// 绘制Procedure标签页
        /// </summary>
        private void DrawProcedureTab()
        {
            _procedureTab.OnGUI(this, _dataProvider);
        }

        /// <summary>
        /// 绘制Luban标签页
        /// </summary>
        private void DrawLubanTab()
        {
            _lubanTab.OnGUI(this, _dataProvider);
        }

        /// <summary>
        /// 绘制设置标签页
        /// </summary>
        private void DrawSettingsTab()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("监控设置", _headerStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("刷新设置", EditorStyles.boldLabel);
            _autoRefresh = EditorGUILayout.Toggle("自动刷新", _autoRefresh);
            _refreshInterval = EditorGUILayout.Slider("刷新间隔", _refreshInterval, 0.1f, 5f);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("显示设置", EditorStyles.boldLabel);
            // TODO: 添加更多显示设置选项

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(!_isPlaying);
            if (GUILayout.Button("清空所有缓存"))
            {
                _dataProvider?.ClearAllCache();
            }
            if (GUILayout.Button("重新扫描所有数据"))
            {
                RefreshData();
            }
            EditorGUI.EndDisabledGroup();
        }

        #endregion

        #region Helper Methods

        private void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        private void DrawStatusCard(string title, string status, bool isActive)
        {
            var bgColor = GUI.backgroundColor;
            GUI.backgroundColor = isActive ? new Color(0.5f, 1f, 0.7f) : new Color(1f, 0.5f, 0.5f);

            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(status, _infoStyle);
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = bgColor;
        }

        private void DrawStatisticRow(string label, string value, bool hasValue)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            GUILayout.Label(value, hasValue ? _successStyle : _infoStyle, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNotPlayingMessage()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("未在运行模式", _headerStyle, GUILayout.Height(30));
            EditorGUILayout.LabelField("请进入运行模式以使用监控功能", _infoStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawFrameworkNotInitializedMessage()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("框架未初始化", _headerStyle, GUILayout.Height(30));
            EditorGUILayout.LabelField("请等待框架初始化完成", _infoStyle);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("确保在游戏启动时调用了 Vomit.Init()", MessageType.Warning);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Data Management

        private void InitializeDataProvider()
        {
            if (_isDataProviderInitialized) return;

            _dataProvider = new MonitorDataProvider();
            _isDataProviderInitialized = true;

            RefreshData();
        }

        private void CleanupDataProvider()
        {
            if (_dataProvider != null)
            {
                _dataProvider.Dispose();
                _dataProvider = null;
            }
            _isDataProviderInitialized = false;
        }

        private void RefreshData()
        {
            if (_dataProvider != null && _isPlaying && _isFrameworkInitialized)
            {
                _dataProvider.RefreshAll();
            }
        }

        private void CheckFrameworkStatus()
        {
            bool wasInitialized = _isFrameworkInitialized;
            _isFrameworkInitialized = Vomit.IsInit;

            if (_isFrameworkInitialized && !wasInitialized)
            {
                // 框架刚初始化
                _statusMessage = "框架已初始化 - 监控就绪";
                if (!_isDataProviderInitialized)
                {
                    InitializeDataProvider();
                }
            }
            else if (!_isFrameworkInitialized)
            {
                _statusMessage = _isPlaying ? "等待框架初始化..." : "未在运行模式";
            }
            else
            {
                _statusMessage = "监控运行中";
            }
        }

        #endregion

        #region Styles

        private void InitializeStyles()
        {
            if (_tabButtonStyle != null) return;

            // 背景色定义
            var darkBgColor = new Color(0.22f, 0.22f, 0.22f, 1f); // #383838
            var lightTextColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            var mediumTextColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            var accentColor = new Color(0.3f, 0.8f, 1f, 1f);

            _tabButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 11,
                padding = new RectOffset(8, 8, 4, 4)
            };

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = lightTextColor },
                wordWrap = false
            };

            _statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = lightTextColor },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 2, 5, 2)
            };

            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 8, 12, 8),
                normal = { background = MakeTexture(2, 2, new Color(0.15f, 0.15f, 0.15f, 0.5f)) }
            };

            _infoStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = mediumTextColor },
                padding = new RectOffset(2, 1, 2, 1)
            };

            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) },
                padding = new RectOffset(2, 1, 2, 1)
            };

            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(1f, 0.45f, 0.45f) },
                padding = new RectOffset(2, 1, 2, 1)
            };

            _successStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.4f, 1f, 0.6f) },
                padding = new RectOffset(2, 1, 2, 1)
            };
        }

        /// <summary>
        /// 创建纯色纹理
        /// </summary>
        private Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        #endregion
    }
}
