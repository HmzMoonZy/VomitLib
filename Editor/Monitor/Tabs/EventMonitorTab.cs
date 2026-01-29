using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// Event监控标签页 - 监控Event系统和触发事件
    /// </summary>
    public class EventMonitorTab
    {
        #region Fields

        // 过滤选项
        private string _searchFilter = "";
        private bool _showOnlyWithListeners = false;

        // UI状态
        private Vector2 _scrollPosition;
        private Dictionary<Type, bool> _eventFoldoutStates = new Dictionary<Type, bool>();

        // 样式
        private GUIStyle _eventHeaderStyle;
        private GUIStyle _listenerStyle;
        private GUIStyle _activeStyle;
        private GUIStyle _inactiveStyle;

        #endregion

        #region Public Methods

        public void OnGUI(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            EnsureStylesInitialized();

            // 绘制头部
            DrawHeader(window, provider);

            EditorGUILayout.Space(5);

            // 绘制过滤选项
            DrawFilterOptions(window);

            EditorGUILayout.Space(5);

            // 绘制Event列表
            DrawEventList(window, provider);
        }

        #endregion

        #region Drawing Methods

        private void DrawHeader(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var eventInfos = provider.EventInfos;
            var activeEvents = eventInfos.Count(e => e.ListenerCount > 0);
            var totalCount = eventInfos.Count;

            var rect = EditorGUILayout.GetControlRect(false, 50);
            EditorGUI.DrawRect(rect, new Color(0.6f, 0.4f, 0.9f, 0.2f));

            var headerRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 40);
            EditorGUI.LabelField(headerRect, "Event系统监控", window._headerStyle);

            var statsRect = new Rect(rect.x + 10, rect.y + 25, rect.width - 20, 20);
            var statsText = $"总计: {totalCount} | 活跃: {activeEvents} | 空闲: {totalCount - activeEvents}";
            EditorGUI.LabelField(statsRect, statsText, window._infoStyle);
        }

        private void DrawFilterOptions(VomitMonitorWindow window)
        {
            var rect = EditorGUILayout.GetControlRect(false, 25);
            EditorGUI.DrawRect(rect, new Color(0.9f, 0.9f, 0.9f, 0.2f));

            var filterRect = new Rect(rect.x + 5, rect.y + 2, rect.width - 10, 20);

            // 搜索框
            var searchRect = new Rect(filterRect.x, filterRect.y, 200, 20);
            _searchFilter = EditorGUI.TextField(searchRect, "搜索:", _searchFilter);

            // 过滤选项
            var toggleRect = new Rect(searchRect.xMax + 20, filterRect.y, 150, 20);
            _showOnlyWithListeners = EditorGUI.Toggle(toggleRect, "仅显示有监听器", _showOnlyWithListeners);
        }

        private void DrawEventList(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var eventInfos = provider.EventInfos;

            // 应用过滤
            var filteredEvents = eventInfos.Where(e =>
            {
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !(e.EventTypeName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0) &&
                    !(e.FullTypeName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return false;
                }

                if (_showOnlyWithListeners && e.ListenerCount == 0) return false;

                return true;
            })
            .OrderByDescending(e => e.ListenerCount)
            .ThenBy(e => e.EventTypeName)
            .ToList();

            if (filteredEvents.Count == 0)
            {
                var rect = EditorGUILayout.GetControlRect(false, 60);
                EditorGUI.DrawRect(rect, new Color(0.95f, 0.95f, 0.95f, 1f));
                var messageRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 40);
                EditorGUI.LabelField(messageRect, "没有找到匹配的Event\n调整过滤条件或搜索关键词", window._infoStyle);
                return;
            }

            // 统计信息
            var statsRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.LabelField(statsRect, $"当前显示: {filteredEvents.Count} 个事件", window._infoStyle);

            EditorGUILayout.Space(2);

            // Event列表
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var eventInfo in filteredEvents)
            {
                DrawEventItem(window, provider, eventInfo);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEventItem(VomitMonitorWindow window, MonitorDataProvider provider, EventInfo eventInfo)
        {
            if (!_eventFoldoutStates.ContainsKey(eventInfo.EventType))
            {
                _eventFoldoutStates[eventInfo.EventType] = false;
            }

            var hasListeners = eventInfo.ListenerCount > 0;
            var bgColor = hasListeners ? new Color(0.4f, 1f, 0.6f, 0.15f) : new Color(0.95f, 0.95f, 0.95f, 0.5f);

            var rect = EditorGUILayout.GetControlRect(false, 24);
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUI.DrawRect(rect, bgColor);
            GUI.backgroundColor = oldBg;

            // 折叠箭头
            var foldoutRect = new Rect(rect.x + 5, rect.y + 2, 15, 20);
            _eventFoldoutStates[eventInfo.EventType] = EditorGUI.Foldout(foldoutRect,
                _eventFoldoutStates[eventInfo.EventType], "", true);

            // Event名称
            var nameRect = new Rect(foldoutRect.xMax + 5, rect.y + 2, 150, 20);
            EditorGUI.LabelField(nameRect, eventInfo.EventTypeName, _eventHeaderStyle);

            // 监听器数量
            var listenerRect = new Rect(nameRect.xMax + 10, rect.y + 2, 80, 20);
            var listenerText = hasListeners ? $"监听器: {eventInfo.ListenerCount}" : "无监听器";
            var listenerStyle = hasListeners ? _activeStyle : _inactiveStyle;
            EditorGUI.LabelField(listenerRect, listenerText, listenerStyle);

            // 类型标签
            var typeRect = new Rect(listenerRect.xMax + 10, rect.y + 2, 60, 20);
            var typeText = eventInfo.IsStruct ? "结构体" : "类";
            EditorGUI.LabelField(typeRect, typeText, window._infoStyle);

            // 程序集
            var assemblyRect = new Rect(typeRect.xMax + 10, rect.y + 2, 100, 20);
            EditorGUI.LabelField(assemblyRect, eventInfo.AssemblyName, window._infoStyle);

            // 触发按钮
            var buttonRect = new Rect(rect.xMax - 60, rect.y + 1, 55, 22);
            var oldEnabled = GUI.enabled;
            GUI.enabled = eventInfo.IsStruct; // 只允许触发结构体事件

            if (GUI.Button(buttonRect, "触发", EditorStyles.miniButton))
            {
                TriggerEvent(provider, eventInfo);
            }

            GUI.enabled = oldEnabled;

            // 详细信息
            if (_eventFoldoutStates[eventInfo.EventType])
            {
                EditorGUI.indentLevel++;
                DrawEventDetails(window, eventInfo);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawEventDetails(VomitMonitorWindow window, EventInfo eventInfo)
        {
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginVertical(window._boxStyle);

            // 完整类型名
            EditorGUILayout.LabelField($"完整类型名:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(eventInfo.FullTypeName, window._infoStyle);
            EditorGUILayout.Space(2);

            // 类型信息
            EditorGUILayout.LabelField($"类型信息:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"是否为结构体: {(eventInfo.IsStruct ? "是" : "否")}", window._infoStyle);
            EditorGUILayout.LabelField($"程序集: {eventInfo.AssemblyName}", window._infoStyle);
            EditorGUILayout.LabelField($"命名空间: {eventInfo.EventType?.Namespace ?? "全局"}", window._infoStyle);

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Helper Methods

        private void TriggerEvent(MonitorDataProvider provider, EventInfo eventInfo)
        {
            try
            {
                provider.TriggerEvent(eventInfo.EventType);
                Debug.Log($"[EventMonitorTab] 成功触发事件: {eventInfo.EventTypeName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventMonitorTab] 触发事件失败: {eventInfo.EventTypeName}, 错误: {e.Message}");
            }
        }

        private void EnsureStylesInitialized()
        {
            if (_eventHeaderStyle != null) return;

            // 统一的颜色变量
            var lightTextColor = new Color(0.95f, 0.95f, 0.95f);
            var mediumTextColor = new Color(0.75f, 0.75f, 0.75f);
            var successColor = new Color(0.4f, 1f, 0.6f);

            _eventHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = lightTextColor },
                padding = new RectOffset(5, 5, 2, 2)
            };

            _listenerStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.5f, 0.8f, 1f) },
                padding = new RectOffset(5, 5, 2, 2)
            };

            _activeStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = successColor },
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(5, 5, 2, 2)
            };

            _inactiveStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = mediumTextColor },
                padding = new RectOffset(5, 5, 2, 2)
            };
        }

        #endregion
    }
}
