using System.Collections.Generic;
using System.Linq;
using Twenty2.VomitLib.Monitor;
using UnityEditor;
using UnityEngine;
using System;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// CommandMonitor的自定义Inspector - Command系统监控面板
    /// 提供Command类型扫描、执行和结果查看功能
    /// </summary>
    [CustomEditor(typeof(CommandMonitor))]
    public class CommandMonitorInspector : UnityEditor.Editor
    {
        #region Fields

        private CommandMonitor _monitor;
        private double _lastRefreshTime;

        // 界面状态
        private bool _showSettings = false;
        private Dictionary<Type, bool> _commandFoldoutStates = new Dictionary<Type, bool>();

        // 过滤和搜索
        private string _commandSearchFilter = "";
        private bool _showOnlyExecutable = false;
        private bool _showOnlyWithResults = false;
        
        // Command选择
        private string[] _availableCommandNames = new string[0];
        private int _selectedCommandIndex = 0;

        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _fieldStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _commandHeaderStyle;
        private GUIStyle _executionResultStyle;

        // 颜色
        private readonly Color _commandColor = new Color(0.8f, 0.4f, 0.8f, 0.3f);
        private readonly Color _fieldColor = new Color(0.9f, 0.9f, 0.9f, 0.1f);
        private readonly Color _executableColor = new Color(0.4f, 1f, 0.4f, 0.3f);
        private readonly Color _nonExecutableColor = new Color(1f, 0.4f, 0.4f, 0.3f);
        private readonly Color _successColor = new Color(0.4f, 0.8f, 0.4f, 0.4f);
        private readonly Color _errorColor = new Color(0.8f, 0.4f, 0.4f, 0.4f);

        // GUI操作相关
        private bool _needsRefresh;
        private Vector2 _commandScrollPosition;

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            _monitor = (CommandMonitor)target;
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
                DrawCommandMonitoring();
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
            EditorGUI.DrawRect(rect, _commandColor);
            
            var headerRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 30);
            EditorGUI.LabelField(headerRect, "QFramework Command Monitor", _headerStyle);
            
            if (Application.isPlaying && _monitor.EnableMonitoring)
            {
                var commandInfos = _monitor.GetCommandInfos();
                var executableCount = commandInfos.Count(c => c.CanInstantiate);
                var statusText = $"监控中 - {commandInfos.Count} 个Command类型 ({executableCount} 个可执行)";
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
                EditorGUILayout.LabelField("扫描选项", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_includeSystemAssemblies"), new GUIContent("包含系统程序集"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_includeUnityAssemblies"), new GUIContent("包含Unity程序集"));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("刷新数据", _buttonStyle))
                {
                    _monitor.RefreshCommandData();
                }
                if (GUILayout.Button("清空缓存", _buttonStyle))
                {
                    _monitor.ClearCache();
                }
                if (GUILayout.Button("清空执行结果", _buttonStyle))
                {
                    _monitor.ClearExecutionResults();
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
            EditorGUI.DrawRect(rect, _nonExecutableColor);
            
            var messageRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 40);
            EditorGUI.LabelField(messageRect, "Command监控功能仅在运行模式下可用\n请启动游戏来扫描和执行Command", _fieldStyle);
        }

        /// <summary>
        /// 绘制Command监控主面板
        /// </summary>
        private void DrawCommandMonitoring()
        {
            DrawCommandList();
        }

        /// <summary>
        /// 绘制Command列表
        /// </summary>
        private void DrawCommandList()
        {
            var commandInfos = _monitor.GetCommandInfos();
            var executionResults = _monitor.GetExecutionResults();
            
            // 搜索和过滤控件
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
            _commandSearchFilter = EditorGUILayout.TextField(_commandSearchFilter);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            _showOnlyExecutable = EditorGUILayout.Toggle("仅显示可执行Command", _showOnlyExecutable);
            _showOnlyWithResults = EditorGUILayout.Toggle("仅显示有执行结果", _showOnlyWithResults);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // 应用过滤
            var filteredCommands = commandInfos.Where(c =>
            {
                if (!string.IsNullOrEmpty(_commandSearchFilter) && 
                    !c.CommandTypeName.ToLower().Contains(_commandSearchFilter.ToLower()) &&
                    !c.FullTypeName.ToLower().Contains(_commandSearchFilter.ToLower()))
                {
                    return false;
                }
                
                if (_showOnlyExecutable && !c.CanInstantiate)
                {
                    return false;
                }
                
                if (_showOnlyWithResults && !executionResults.ContainsKey(c.CommandType))
                {
                    return false;
                }
                
                return true;
            }).ToList();

            if (filteredCommands.Count == 0)
            {
                var rect = EditorGUILayout.GetControlRect(false, 40);
                EditorGUI.DrawRect(rect, _nonExecutableColor);
                var messageRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 20);
                EditorGUI.LabelField(messageRect, "没有找到匹配的Command", _fieldStyle);
                return;
            }

            // 统计信息
            var totalCommands = commandInfos.Count;
            var executableCommands = commandInfos.Count(c => c.CanInstantiate);
            var withReturnValue = commandInfos.Count(c => c.HasReturnValue);
            
            EditorGUILayout.LabelField($"总Command数: {totalCommands}, 可执行: {executableCommands}, 有返回值: {withReturnValue}, 当前显示: {filteredCommands.Count}", _fieldStyle);

            EditorGUILayout.Space(2);

            // Command列表滚动区域
            _commandScrollPosition = EditorGUILayout.BeginScrollView(_commandScrollPosition, 
                GUILayout.Height(Mathf.Min(500, filteredCommands.Count * 40 + 50)));

            foreach (var commandInfo in filteredCommands.OrderBy(c => c.CommandTypeName))
            {
                DrawCommandInfo(commandInfo, executionResults.GetValueOrDefault(commandInfo.CommandType));
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制单个Command信息
        /// </summary>
        private void DrawCommandInfo(CommandInfo commandInfo, CommandExecutionResult executionResult)
        {
            var canExecute = commandInfo.CanInstantiate;
            var backgroundColor = canExecute ? _executableColor : _nonExecutableColor;
            
            // 如果有执行结果，根据成功/失败调整颜色
            if (executionResult != null)
            {
                backgroundColor = executionResult.Success ? _successColor : _errorColor;
            }
            
            var rect = EditorGUILayout.GetControlRect(false, 35);
            EditorGUI.DrawRect(rect, backgroundColor);
            
            // Command名称
            var nameRect = new Rect(rect.x + 5, rect.y + 2, 180, 16);
            EditorGUI.LabelField(nameRect, commandInfo.CommandTypeName, _commandHeaderStyle);
            
            // 返回值类型信息
            var typeInfoRect = new Rect(nameRect.x, nameRect.y + 16, 180, 14);
            var typeInfo = commandInfo.HasReturnValue ? $"返回: {GetSimpleTypeName(commandInfo.ReturnType)}" : "无返回值";
            EditorGUI.LabelField(typeInfoRect, typeInfo, _valueStyle);
            
            // 程序集信息
            var assemblyRect = new Rect(nameRect.xMax + 10, rect.y + 2, 120, 16);
            EditorGUI.LabelField(assemblyRect, commandInfo.AssemblyName, _valueStyle);
            
            // 可执行状态
            var statusRect = new Rect(assemblyRect.x, assemblyRect.y + 16, 120, 14);
            var statusText = canExecute ? "可执行" : "不可执行";
            EditorGUI.LabelField(statusRect, statusText, _valueStyle);
            
            // 执行按钮
            var buttonRect = new Rect(rect.xMax - 65, rect.y + 5, 60, 25);
            EditorGUI.BeginDisabledGroup(!canExecute);
            if (GUI.Button(buttonRect, "执行", _buttonStyle))
            {
                _monitor.ExecuteCommand(commandInfo.CommandType);
            }
            EditorGUI.EndDisabledGroup();
            
            // 执行结果信息
            if (executionResult != null)
            {
                var resultRect = new Rect(rect.x + 5, rect.y + 35, rect.width - 10, 0);
                DrawExecutionResult(resultRect, executionResult);
                GUILayoutUtility.GetRect(rect.width, 25); // 为执行结果预留空间
            }
        }

        /// <summary>
        /// 绘制执行结果
        /// </summary>
        private void DrawExecutionResult(Rect baseRect, CommandExecutionResult result)
        {
            var rect = new Rect(baseRect.x, baseRect.y, baseRect.width, 25);
            var resultColor = result.Success ? new Color(0.8f, 1f, 0.8f, 0.3f) : new Color(1f, 0.8f, 0.8f, 0.3f);
            EditorGUI.DrawRect(rect, resultColor);
            
            // 时间戳
            var timeRect = new Rect(rect.x + 5, rect.y + 2, 60, 16);
            EditorGUI.LabelField(timeRect, result.Timestamp.ToString("HH:mm:ss"), _executionResultStyle);
            
            // 执行时间
            var durationRect = new Rect(timeRect.xMax + 5, rect.y + 2, 80, 16);
            EditorGUI.LabelField(durationRect, $"{result.ExecutionTime.TotalMilliseconds:F1}ms", _executionResultStyle);
            
            // 结果状态
            var statusRect = new Rect(durationRect.xMax + 5, rect.y + 2, 60, 16);
            var statusText = result.Success ? "成功" : "失败";
            EditorGUI.LabelField(statusRect, statusText, _executionResultStyle);
            
            // 结果值或错误信息
            var resultRect = new Rect(statusRect.xMax + 5, rect.y + 2, rect.xMax - statusRect.xMax - 10, 16);
            string resultText;
            if (result.Success)
            {
                resultText = result.Result?.ToString() ?? "无返回值";
            }
            else
            {
                resultText = result.Exception?.Message ?? "未知错误";
            }
            EditorGUI.LabelField(resultRect, resultText, _executionResultStyle);
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

            _commandHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = Color.black }
            };

            _executionResultStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.2f, 0.2f, 0.2f, 1f) }
            };
        }

        /// <summary>
        /// 获取简化的类型名
        /// </summary>
        private string GetSimpleTypeName(Type type)
        {
            if (type == null) return "null";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(void)) return "void";
            
            return type.Name;
        }

        /// <summary>
        /// 刷新监控数据
        /// </summary>
        private void RefreshMonitorData()
        {
            if (_monitor == null || !_monitor.EnableMonitoring) return;
            
            _monitor.RefreshCommandData();
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