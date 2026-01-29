using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// System监控标签页 - 监控System层和调用方法
    /// </summary>
    public class SystemMonitorTab
    {
        #region Fields

        private string _searchFilter = "";
        private Vector2 _scrollPosition;
        private Type _selectedSystemType = null;

        private GUIStyle _systemHeaderStyle;
        private GUIStyle _methodStyle;

        #endregion

        #region Public Methods

        public void OnGUI(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            EnsureStylesInitialized();

            // 绘制头部
            DrawHeader(window, provider);

            EditorGUILayout.Space(5);

            // 搜索框
            DrawSearchBar(window);

            EditorGUILayout.Space(5);

            // 绘制System列表
            DrawSystemList(window, provider);
        }

        #endregion

        #region Drawing Methods

        private void DrawHeader(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var systemInfos = provider.SystemInfos;
            var initializedCount = systemInfos.Count(s => s.IsInitialized);

            var rect = EditorGUILayout.GetControlRect(false, 50);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.7f, 0.9f, 0.2f));

            var headerRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 40);
            EditorGUI.LabelField(headerRect, "System系统监控", window._headerStyle);

            var statsRect = new Rect(rect.x + 10, rect.y + 25, rect.width - 20, 20);
            var statsText = $"总计: {systemInfos.Count} | 已初始化: {initializedCount}";
            EditorGUI.LabelField(statsRect, statsText, window._infoStyle);
        }

        private void DrawSearchBar(VomitMonitorWindow window)
        {
            var rect = EditorGUILayout.GetControlRect(false, 25);
            _searchFilter = EditorGUI.TextField(new Rect(rect.x + 5, rect.y + 2, 200, 20), "搜索:", _searchFilter);
        }

        private void DrawSystemList(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var systemInfos = provider.SystemInfos;

            // 过滤
            var filteredSystems = systemInfos.Where(s =>
            {
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !(s.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return false;
                }
                return true;
            }).OrderBy(s => s.Name).ToList();

            if (filteredSystems.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到匹配的System", MessageType.Info);
                return;
            }

            // 分割视图
            EditorGUILayout.BeginHorizontal();

            // 左侧：System列表
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            foreach (var systemInfo in filteredSystems)
            {
                DrawSystemListItem(window, systemInfo);
            }
            EditorGUILayout.EndVertical();

            // 右侧：详情
            EditorGUILayout.BeginVertical();
            if (_selectedSystemType != null)
            {
                DrawSystemDetails(window, provider, _selectedSystemType);
            }
            else
            {
                EditorGUILayout.HelpBox("选择一个System查看详细信息", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSystemListItem(VomitMonitorWindow window, SystemInfo systemInfo)
        {
            var isSelected = _selectedSystemType == systemInfo.Type;
            var bgColor = isSelected ? new Color(0.3f, 0.6f, 1f, 0.3f) : new Color(0.9f, 0.9f, 0.9f, 0.1f);

            var rect = EditorGUILayout.GetControlRect(false, 22);
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUI.DrawRect(rect, bgColor);
            GUI.backgroundColor = oldBg;

            if (GUI.Button(rect, "", GUIStyle.none))
            {
                _selectedSystemType = systemInfo.Type;
            }

            var nameRect = new Rect(rect.x + 5, rect.y + 2, 180, 18);
            EditorGUI.LabelField(nameRect, systemInfo.Name, _systemHeaderStyle);

            var statusRect = new Rect(nameRect.xMax + 5, rect.y + 2, 40, 18);
            var statusStyle = systemInfo.IsInitialized ? window._successStyle : window._warningStyle;
            EditorGUI.LabelField(statusRect, systemInfo.IsInitialized ? "✓" : "○", statusStyle);
        }

        private void DrawSystemDetails(VomitMonitorWindow window, MonitorDataProvider provider, Type systemType)
        {
            var systemInfo = provider.SystemInfos.FirstOrDefault(s => s.Type == systemType);
            if (systemInfo == null) return;

            EditorGUILayout.BeginVertical(window._boxStyle);
            EditorGUILayout.LabelField($"System: {systemInfo.Name}", window._headerStyle);
            EditorGUILayout.LabelField($"类型: {systemInfo.FullTypeName}", window._infoStyle);
            EditorGUILayout.LabelField($"状态: {(systemInfo.IsInitialized ? "已初始化" : "未初始化")}",
                systemInfo.IsInitialized ? window._successStyle : window._warningStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // 方法列表
            EditorGUILayout.LabelField("公共方法:", EditorStyles.boldLabel);

            var methods = systemType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                if (method.IsSpecialName) continue;

                var parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                var returnType = method.ReturnType.Name;
                EditorGUILayout.LabelField($"{returnType} {method.Name}({parameters})", _methodStyle);
            }
        }

        #endregion

        #region Helper Methods

        private void EnsureStylesInitialized()
        {
            if (_systemHeaderStyle != null) return;

            // 统一的颜色变量
            var lightTextColor = new Color(0.95f, 0.95f, 0.95f);
            var mediumTextColor = new Color(0.75f, 0.75f, 0.75f);

            _systemHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = lightTextColor },
                padding = new RectOffset(5, 5, 2, 2)
            };

            _methodStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.6f, 0.8f, 1f) },
                fontStyle = FontStyle.Italic,
                wordWrap = false,
                padding = new RectOffset(5, 5, 2, 2)
            };
        }

        #endregion
    }
}
