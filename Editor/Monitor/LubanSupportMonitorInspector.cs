using System.Collections.Generic;
using System.Linq;
using Twenty2.VomitLib.Monitor;
using UnityEditor;
using UnityEngine;
using System;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// LubanSupportMonitor的自定义Inspector - Luban数据表监控面板
    /// 提供数据表概览、数据查询和统计信息功能
    /// </summary>
    [CustomEditor(typeof(LubanSupportMonitor))]
    public class LubanSupportMonitorInspector : UnityEditor.Editor
    {
        #region Fields

        private LubanSupportMonitor _monitor;
        private double _lastRefreshTime;

        // 界面状态
        private bool _showSettings = false;
        private bool _showTablesInfo = true;
        private bool _showTablesList = true;

        // 过滤和搜索
        private string _tableSearchFilter = "";

        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _fieldStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _tableHeaderStyle;
        private GUIStyle _dataStyle;

        // 颜色
        private readonly Color _lubanColor = new Color(0.2f, 0.7f, 0.9f, 0.3f);
        private readonly Color _fieldColor = new Color(0.9f, 0.9f, 0.9f, 0.1f);
        private readonly Color _tableColor = new Color(0.7f, 0.9f, 0.7f, 0.2f);
        private readonly Color _errorColor = new Color(1f, 0.4f, 0.4f, 0.3f);

        // GUI操作相关
        private bool _needsRefresh;
        private Vector2 _tablesScrollPosition;

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            _monitor = (LubanSupportMonitor)target;
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
                DrawLubanMonitoring();
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
            EditorGUI.DrawRect(rect, _lubanColor);
            
            var headerRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 30);
            EditorGUI.LabelField(headerRect, "VomitLib Luban Support Monitor", _headerStyle);
            
            if (Application.isPlaying && _monitor.EnableMonitoring)
            {
                var tablesInfo = _monitor.GetTablesInfo();
                var tableInfos = _monitor.GetTableInfos();
                
                string statusText;
                if (tablesInfo != null)
                {
                    var totalDataCount = tableInfos.Sum(t => t.DataCount);
                    statusText = $"监控中 - {tableInfos.Count} 个数据表, 共 {totalDataCount} 条数据";
                }
                else
                {
                    statusText = "未找到Tables实例";
                }
                
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
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Tables配置", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_tablesInstancePath"), new GUIContent("Tables实例路径"));
                EditorGUILayout.HelpBox("格式: ClassName.FieldName (如 Game.DB)", MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("刷新数据", _buttonStyle))
                {
                    _monitor.RefreshLubanData();
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
            var rect = EditorGUILayout.GetControlRect(false, 80);
            EditorGUI.DrawRect(rect, _errorColor);
            
            var messageRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 60);
            EditorGUI.LabelField(messageRect, "LubanSupport监控功能仅在运行模式下可用\n请启动游戏来监控数据表和数据\n\n注意：数据表会在Launch流程中初始化\n如果显示\"未找到Tables实例\"，请等待游戏完全启动", _fieldStyle);
        }

        /// <summary>
        /// 绘制Luban监控主面板
        /// </summary>
        private void DrawLubanMonitoring()
        {
            var tablesInfo = _monitor.GetTablesInfo();
            
            if (tablesInfo == null)
            {
                var rect = EditorGUILayout.GetControlRect(false, 60);
                EditorGUI.DrawRect(rect, _errorColor);
                var messageRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 40);
                EditorGUI.LabelField(messageRect, "未找到Tables实例\n请确保：1. 游戏已完全启动（通过Launch流程）\n2. Game.DB已初始化\n3. 配置路径正确", _fieldStyle);
                
                // 添加重试按钮
                var retryButtonRect = new Rect(rect.xMax - 80, rect.y + 5, 70, 20);
                if (GUI.Button(retryButtonRect, "重试", _buttonStyle))
                {
                    _monitor.RefreshLubanData();
                }
                
                return;
            }
            
            DrawTablesInfo(tablesInfo);
            DrawTablesList();
        }

        /// <summary>
        /// 绘制Tables信息
        /// </summary>
        private void DrawTablesInfo(LubanTablesInfo tablesInfo)
        {
            var rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(rect, _fieldColor);
            
            _showTablesInfo = EditorGUI.Foldout(new Rect(rect.x + 5, rect.y, rect.width - 10, rect.height),
                _showTablesInfo, "Tables信息", true, _subHeaderStyle);

            if (_showTablesInfo)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField($"类型: {tablesInfo.TablesTypeName}", _fieldStyle);
                EditorGUILayout.LabelField($"命名空间: {tablesInfo.Namespace}", _fieldStyle);
                EditorGUILayout.LabelField($"程序集: {tablesInfo.AssemblyName}", _fieldStyle);
                
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// 绘制数据表列表
        /// </summary>
        private void DrawTablesList()
        {
            var tableInfos = _monitor.GetTableInfos();
            if (tableInfos == null || tableInfos.Count == 0)
            {
                return;
            }
            
            var rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(rect, _fieldColor);
            
            _showTablesList = EditorGUI.Foldout(new Rect(rect.x + 5, rect.y, rect.width - 10, rect.height),
                _showTablesList, $"数据表列表 ({tableInfos.Count})", true, _subHeaderStyle);

            if (_showTablesList)
            {
                EditorGUI.indentLevel++;
                
                // 搜索过滤
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
                _tableSearchFilter = EditorGUILayout.TextField(_tableSearchFilter);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(2);

                // 应用过滤
                var filteredTables = tableInfos.Where(table =>
                {
                    if (!string.IsNullOrEmpty(_tableSearchFilter) && 
                        !table.TableName.ToLower().Contains(_tableSearchFilter.ToLower()) &&
                        !table.DataTypeName.ToLower().Contains(_tableSearchFilter.ToLower()))
                    {
                        return false;
                    }
                    
                    return true;
                }).ToList();

                if (filteredTables.Count == 0)
                {
                    var errorRect = EditorGUILayout.GetControlRect(false, 40);
                    EditorGUI.DrawRect(errorRect, _errorColor);
                    var messageRect = new Rect(errorRect.x + 10, errorRect.y + 10, errorRect.width - 20, 20);
                    EditorGUI.LabelField(messageRect, "没有找到匹配的数据表", _fieldStyle);
                    EditorGUI.indentLevel--;
                    return;
                }

                // 统计信息
                var totalDataCount = filteredTables.Sum(t => t.DataCount);
                EditorGUILayout.LabelField($"显示: {filteredTables.Count}/{tableInfos.Count} 个数据表, 共 {totalDataCount} 条数据", _fieldStyle);

                EditorGUILayout.Space(2);

                // 数据表列表滚动区域
                _tablesScrollPosition = EditorGUILayout.BeginScrollView(_tablesScrollPosition, 
                    GUILayout.Height(Mathf.Min(300, filteredTables.Count * 50 + 20)));

                foreach (var tableInfo in filteredTables.OrderBy(t => t.TableName))
                {
                    DrawTableInfo(tableInfo);
                }

                EditorGUILayout.EndScrollView();
                
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// 绘制单个数据表信息
        /// </summary>
        private void DrawTableInfo(LubanTableInfo tableInfo)
        {
            var tableRect = EditorGUILayout.GetControlRect(false, 45);
            EditorGUI.DrawRect(tableRect, _tableColor);
            
            // 表名
            var nameRect = new Rect(tableRect.x + 10, tableRect.y + 5, 200, 16);
            EditorGUI.LabelField(nameRect, tableInfo.TableName, _tableHeaderStyle);
            
            // 数据类型
            var typeRect = new Rect(nameRect.x, nameRect.y + 18, 200, 16);
            EditorGUI.LabelField(typeRect, $"数据类型: {tableInfo.DataTypeName}", _dataStyle);
            
            // 统计信息
            var countRect = new Rect(tableRect.x + 220, tableRect.y + 5, 150, 16);
            EditorGUI.LabelField(countRect, $"数据条数: {tableInfo.DataCount}", _valueStyle);
            
            var keyCountRect = new Rect(countRect.x, countRect.y + 18, 150, 16);
            EditorGUI.LabelField(keyCountRect, $"键数量: {tableInfo.KeyCount}", _valueStyle);
            
            // 更新时间
            var timeRect = new Rect(tableRect.x + 380, tableRect.y + 5, 200, 16);
            EditorGUI.LabelField(timeRect, $"更新时间: {tableInfo.LastUpdateTime:HH:mm:ss}", _valueStyle);
            
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

            _tableHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = Color.black }
            };

            _dataStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.2f, 0.4f, 0.8f, 1f) }
            };
        }

        /// <summary>
        /// 刷新监控数据
        /// </summary>
        private void RefreshMonitorData()
        {
            if (_monitor == null || !_monitor.EnableMonitoring) return;
            
            _monitor.RefreshLubanData();
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


        #endregion
    }
}