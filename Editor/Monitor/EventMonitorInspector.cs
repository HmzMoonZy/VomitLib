using System.Collections.Generic;
using System.Linq;
using Twenty2.VomitLib.Monitor;
using UnityEditor;
using UnityEngine;
using System;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// EventMonitor的自定义Inspector - Event系统监控面板
    /// 提供Event数据的实时监控、事件触发和历史记录查看功能
    /// </summary>
    [CustomEditor(typeof(EventMonitor))]
    public class EventMonitorInspector : UnityEditor.Editor
    {
        #region Fields

        private EventMonitor _monitor;
        private double _lastRefreshTime;

        // 界面状态
        private bool _showSettings = false;
        private Dictionary<Type, bool> _eventFoldoutStates = new Dictionary<Type, bool>();

        // 过滤和搜索
        private string _eventSearchFilter = "";
        private bool _showOnlyWithListeners = false;
        
        // Event选择
        private string[] _availableEventNames = new string[0];
        private int _selectedEventIndex = 0;

        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _fieldStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _eventHeaderStyle;

        // 颜色
        private readonly Color _eventColor = new Color(0.6f, 0.4f, 0.9f, 0.3f);
        private readonly Color _fieldColor = new Color(0.9f, 0.9f, 0.9f, 0.1f);
        private readonly Color _activeEventColor = new Color(0.4f, 1f, 0.4f, 0.3f);
        private readonly Color _inactiveEventColor = new Color(1f, 0.4f, 0.4f, 0.3f);

        // GUI操作相关
        private bool _needsRefresh;
        private Vector2 _eventScrollPosition;

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            _monitor = (EventMonitor)target;
            EditorApplication.delayCall += InitializeStyles;

            if (Application.isPlaying && _monitor != null && _monitor.EnableMonitoring)
            {
                EditorApplication.delayCall += () => RefreshMonitorData();
            }
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= InitializeStyles;
        }

        public override void OnInspectorGUI()
        {
            if (_headerStyle == null)
            {
                InitializeStyles();
                if (_headerStyle == null) return;
            }

            ProcessPendingOperations();

            serializedObject.Update();

            DrawHeader();
            EditorGUILayout.Space(5);

            DrawSettings();
            EditorGUILayout.Space(5);

            if (Application.isPlaying && _monitor.EnableMonitoring)
            {
                DrawEventMonitoring();
            }
            else
            {
                DrawPlayModeInfo();
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Drawing Methods

        /// <summary>
        /// 绘制头部信息
        /// </summary>
        private void DrawHeader()
        {
            var rect = EditorGUILayout.GetControlRect(false, 40);
            EditorGUI.DrawRect(rect, _eventColor);
            
            var headerRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 30);
            EditorGUI.LabelField(headerRect, "QFramework Event Monitor", _headerStyle);
            
            if (Application.isPlaying && _monitor.EnableMonitoring)
            {
                var eventInfos = _monitor.GetEventInfos();
                var statusText = $"监控中 - {eventInfos.Count} 个事件类型";
                var statusRect = new Rect(headerRect.x, headerRect.y + 20, headerRect.width, 15);
                EditorGUI.LabelField(statusRect, statusText, _subHeaderStyle);
            }
        }

        /// <summary>
        /// 绘制设置面板
        /// </summary>
        private void DrawSettings()
        {
            var rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(rect, _fieldColor);
            
            _showSettings = EditorGUI.Foldout(new Rect(rect.x + 5, rect.y, rect.width - 30, rect.height),
                _showSettings, "监控设置", true, _subHeaderStyle);

            if (_showSettings)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("_enableMonitoring"), new GUIContent("启用监控"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_autoRefresh"), new GUIContent("自动刷新"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_refreshInterval"), new GUIContent("刷新间隔"));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("刷新数据", _buttonStyle))
                {
                    _monitor.RefreshEventData();
                }
                if (GUILayout.Button("清空缓存", _buttonStyle))
                {
                    _monitor.ClearCache();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// 绘制运行模式提示
        /// </summary>
        private void DrawPlayModeInfo()
        {
            var rect = EditorGUILayout.GetControlRect(false, 60);
            EditorGUI.DrawRect(rect, _inactiveEventColor);
            
            var messageRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 40);
            EditorGUI.LabelField(messageRect, "Event监控功能仅在运行模式下可用\n请启动游戏来查看Event系统状态", _fieldStyle);
        }

        /// <summary>
        /// 绘制Event监控主面板
        /// </summary>
        private void DrawEventMonitoring()
        {
            DrawEventList();
        }

        /// <summary>
        /// 绘制Event列表
        /// </summary>
        private void DrawEventList()
        {
            var eventInfos = _monitor.GetEventInfos();
            
            // 搜索和过滤控件
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
            _eventSearchFilter = EditorGUILayout.TextField(_eventSearchFilter);
            _showOnlyWithListeners = EditorGUILayout.Toggle("仅显示有监听器的事件", _showOnlyWithListeners);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // 应用过滤
            var filteredEvents = eventInfos.Where(e =>
            {
                if (!string.IsNullOrEmpty(_eventSearchFilter) && 
                    !e.EventTypeName.ToLower().Contains(_eventSearchFilter.ToLower()) &&
                    !e.FullTypeName.ToLower().Contains(_eventSearchFilter.ToLower()))
                {
                    return false;
                }
                
                if (_showOnlyWithListeners && e.ListenerCount == 0)
                {
                    return false;
                }
                
                return true;
            }).ToList();

            if (filteredEvents.Count == 0)
            {
                var rect = EditorGUILayout.GetControlRect(false, 40);
                EditorGUI.DrawRect(rect, _inactiveEventColor);
                var messageRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 20);
                EditorGUI.LabelField(messageRect, "没有找到匹配的事件", _fieldStyle);
                return;
            }

            // 统计信息
            var totalEvents = eventInfos.Count;
            var activeEvents = eventInfos.Count(e => e.ListenerCount > 0);
            EditorGUILayout.LabelField($"总事件数: {totalEvents}, 活跃事件数: {activeEvents}, 当前显示: {filteredEvents.Count}", _fieldStyle);

            EditorGUILayout.Space(2);

            // 事件列表滚动区域
            _eventScrollPosition = EditorGUILayout.BeginScrollView(_eventScrollPosition, 
                GUILayout.Height(Mathf.Min(400, filteredEvents.Count * 25 + 50)));

            foreach (var eventInfo in filteredEvents.OrderByDescending(e => e.ListenerCount).ThenBy(e => e.EventTypeName))
            {
                DrawEventInfo(eventInfo);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制单个Event信息
        /// </summary>
        private void DrawEventInfo(EventInfo eventInfo)
        {
            var hasListeners = eventInfo.ListenerCount > 0;
            var backgroundColor = hasListeners ? _activeEventColor : _inactiveEventColor;
            
            var rect = EditorGUILayout.GetControlRect(false, 22);
            EditorGUI.DrawRect(rect, backgroundColor);
            
            // Event类型名称
            var nameRect = new Rect(rect.x + 5, rect.y + 2, 150, 18);
            EditorGUI.LabelField(nameRect, eventInfo.EventTypeName, _eventHeaderStyle);
            
            // 监听器数量
            var countRect = new Rect(nameRect.xMax + 10, rect.y + 2, 80, 18);
            var countText = hasListeners ? $"监听器: {eventInfo.ListenerCount}" : "无监听器";
            EditorGUI.LabelField(countRect, countText, _fieldStyle);
            
            // 程序集信息
            var assemblyRect = new Rect(countRect.xMax + 10, rect.y + 2, 120, 18);
            EditorGUI.LabelField(assemblyRect, eventInfo.AssemblyName, _valueStyle);
            
            // 触发按钮
            var buttonRect = new Rect(rect.xMax - 60, rect.y + 1, 55, 20);
            if (GUI.Button(buttonRect, "触发", _buttonStyle))
            {
                _monitor.TriggerEvent(eventInfo.EventType);
            }
            
            // 详细信息（可折叠）
            var foldoutRect = new Rect(rect.x + rect.width - 80, rect.y + 2, 15, 18);
            if (!_eventFoldoutStates.ContainsKey(eventInfo.EventType))
                _eventFoldoutStates[eventInfo.EventType] = false;
                
            _eventFoldoutStates[eventInfo.EventType] = EditorGUI.Foldout(foldoutRect, 
                _eventFoldoutStates[eventInfo.EventType], "", true);
            
            if (_eventFoldoutStates[eventInfo.EventType])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"完整类型名: {eventInfo.FullTypeName}", _valueStyle);
                EditorGUILayout.LabelField($"是否为结构体: {(eventInfo.IsStruct ? "是" : "否")}", _valueStyle);
                EditorGUI.indentLevel--;
            }
        }


        #endregion

        #region Utility Methods

        /// <summary>
        /// 初始化样式
        /// </summary>
        private void InitializeStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };

            _subHeaderStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = Color.gray },
                fontStyle = FontStyle.Bold
            };

            _fieldStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = Color.black },
                wordWrap = true
            };

            _valueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                normal = { textColor = Color.gray },
                fontStyle = FontStyle.Italic
            };

            _buttonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 10
            };

            _eventHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = Color.black }
            };

        }

        /// <summary>
        /// 刷新监控数据
        /// </summary>
        private void RefreshMonitorData()
        {
            if (_monitor == null || !_monitor.EnableMonitoring) return;
            
            _monitor.RefreshEventData();
            Repaint();
        }

        /// <summary>
        /// 处理待执行操作
        /// </summary>
        private void ProcessPendingOperations()
        {
            if (_needsRefresh)
            {
                _needsRefresh = false;
                EditorApplication.delayCall += () =>
                {
                    RefreshMonitorData();
                    Repaint();
                };
            }
        }

        /// <summary>
        /// 检查自动刷新
        /// </summary>
        private void CheckAutoRefresh()
        {
            if (Application.isPlaying && _monitor != null && _monitor.EnableMonitoring && _monitor.AutoRefresh)
            {
                if (EditorApplication.timeSinceStartup - _lastRefreshTime > _monitor.RefreshInterval)
                {
                    _lastRefreshTime = EditorApplication.timeSinceStartup;
                    _needsRefresh = true;
                }
            }
        }

        #endregion
    }
}