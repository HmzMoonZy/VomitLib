using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// Luban数据监控标签页 - 监控Luban配置数据
    /// </summary>
    public class LubanMonitorTab
    {
        #region Fields

        private string _searchFilter = "";
        private Vector2 _scrollPosition;

        // 样式
        private GUIStyle _tableNameStyle;
        private GUIStyle _dataTypeStyle;
        private GUIStyle _countStyle;

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

            // 绘制数据表列表
            DrawTableList(window, provider);
        }

        #endregion

        #region Drawing Methods

        private void DrawHeader(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var tableInfos = provider.LubanTableInfos;
            var totalRecords = tableInfos.Sum(t => t.DataCount);

            var rect = EditorGUILayout.GetControlRect(false, 50);
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.9f, 0.7f, 0.2f));

            var headerRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 40);
            EditorGUI.LabelField(headerRect, "Luban数据监控", window._headerStyle);

            var statsRect = new Rect(rect.x + 10, rect.y + 25, rect.width - 20, 20);
            var statsText = $"数据表: {tableInfos.Count} | 总记录数: {totalRecords}";
            EditorGUI.LabelField(statsRect, statsText, window._infoStyle);
        }

        private void DrawSearchBar(VomitMonitorWindow window)
        {
            var rect = EditorGUILayout.GetControlRect(false, 25);
            _searchFilter = EditorGUI.TextField(new Rect(rect.x + 5, rect.y + 2, 200, 20), "搜索:", _searchFilter);
        }

        private void DrawTableList(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var tableInfos = provider.LubanTableInfos;

            // 过滤
            var filteredTables = tableInfos.Where(t =>
            {
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !(t.TableName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return false;
                }
                return true;
            })
            .OrderBy(t => t.TableName)
            .ToList();

            if (filteredTables.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到匹配的数据表", MessageType.Info);
                return;
            }

            // 表头
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("表名", _tableNameStyle, GUILayout.Width(200));
            GUILayout.Label("类型", _dataTypeStyle, GUILayout.Width(150));
            GUILayout.Label("记录数", _countStyle, GUILayout.Width(80));
            GUILayout.Label("键数", _countStyle, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            // 数据表列表
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var tableInfo in filteredTables)
            {
                DrawTableItem(window, tableInfo);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTableItem(VomitMonitorWindow window, LubanTableInfo tableInfo)
        {
            var rect = EditorGUILayout.GetControlRect(false, 22);
            var bgColor = tableInfo.DataCount > 0 ? new Color(0.95f, 1f, 0.95f, 0.3f) : new Color(0.95f, 0.95f, 0.95f, 0.3f);
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUI.DrawRect(rect, bgColor);
            GUI.backgroundColor = oldBg;

            EditorGUILayout.BeginHorizontal();

            // 表名
            GUILayout.Label(tableInfo.TableName, _tableNameStyle, GUILayout.Width(200));

            // 类型
            GUILayout.Label(tableInfo.DataTypeName ?? "Unknown", _dataTypeStyle, GUILayout.Width(150));

            // 记录数
            var countStyle = tableInfo.DataCount > 0 ? window._successStyle : window._warningStyle;
            GUILayout.Label(tableInfo.DataCount.ToString(), countStyle, GUILayout.Width(80));

            // 键数
            GUILayout.Label(tableInfo.KeyCount.ToString(), _countStyle, GUILayout.Width(80));

            // 最后更新时间
            if (tableInfo.LastUpdateTime > DateTime.MinValue)
            {
                var timeDiff = DateTime.Now - tableInfo.LastUpdateTime;
                var timeText = timeDiff.TotalSeconds < 60 ? $"{(int)timeDiff.TotalSeconds}s前" :
                              timeDiff.TotalMinutes < 60 ? $"{(int)timeDiff.TotalMinutes}m前" :
                              $"{(int)timeDiff.TotalHours}h前";
                GUILayout.Label(timeText, _countStyle);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void EnsureStylesInitialized()
        {
            if (_tableNameStyle != null) return;

            // 统一的颜色变量
            var lightTextColor = new Color(0.95f, 0.95f, 0.95f);
            var mediumTextColor = new Color(0.75f, 0.75f, 0.75f);

            _tableNameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = lightTextColor },
                padding = new RectOffset(5, 5, 2, 2)
            };

            _dataTypeStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.6f, 0.8f, 1f) },
                padding = new RectOffset(5, 5, 2, 2)
            };

            _countStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = mediumTextColor },
                padding = new RectOffset(5, 5, 2, 2)
            };
        }

        #endregion
    }
}
