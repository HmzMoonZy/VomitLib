using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// Command监控标签页 - 监控和执行Command
    /// </summary>
    public class CommandMonitorTab
    {
        #region Fields

        // 过滤选项
        private string _searchFilter = "";
        private bool _showOnlyExecutable = false;
        private bool _showOnlyWithResults = false;
        private bool _groupByInterface = true;

        // UI状态
        private Vector2 _scrollPosition;
        private Dictionary<Type, bool> _commandFoldoutStates = new Dictionary<Type, bool>();
        private Dictionary<Type, bool> _interfaceFoldoutStates = new Dictionary<Type, bool>();

        // 选中的Command
        private Type _selectedCommandType = null;

        // 样式
        private GUIStyle _commandHeaderStyle;
        private GUIStyle _executableStyle;
        private GUIStyle _nonExecutableStyle;
        private GUIStyle _successStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _resultStyle;

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

            // 绘制Command列表
            DrawCommandList(window, provider);
        }

        #endregion

        #region Drawing Methods

        private void DrawHeader(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var commandInfos = provider.CommandInfos;
            var executableCount = commandInfos.Count(c => c.CanInstantiate);
            var withReturnValue = commandInfos.Count(c => c.HasReturnValue);
            var totalCount = commandInfos.Count;

            var rect = EditorGUILayout.GetControlRect(false, 50);
            EditorGUI.DrawRect(rect, new Color(0.8f, 0.4f, 0.8f, 0.2f));

            var headerRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 40);
            EditorGUI.LabelField(headerRect, "Command监控与执行", window._headerStyle);

            var statsRect = new Rect(rect.x + 10, rect.y + 25, rect.width - 20, 20);
            var statsText = $"总计: {totalCount} | 可执行: {executableCount} | 有返回值: {withReturnValue}";
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
            var toggleWidth = 120;
            var toggleX = searchRect.xMax + 20;

            _showOnlyExecutable = EditorGUI.Toggle(new Rect(toggleX, filterRect.y, toggleWidth, 20),
                "仅可执行", _showOnlyExecutable);
            toggleX += toggleWidth + 10;

            _showOnlyWithResults = EditorGUI.Toggle(new Rect(toggleX, filterRect.y, toggleWidth, 20),
                "仅显示执行结果", _showOnlyWithResults);
            toggleX += toggleWidth + 10;

            _groupByInterface = EditorGUI.Toggle(new Rect(toggleX, filterRect.y, toggleWidth, 20),
                "按接口分组", _groupByInterface);
        }

        private void DrawCommandList(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var commandInfos = provider.CommandInfos;
            var executionResults = provider.CommandExecutionResults;

            // 应用过滤
            var filteredCommands = commandInfos.Where(c =>
            {
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !(c.CommandTypeName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0) &&
                    !(c.FullTypeName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return false;
                }

                if (_showOnlyExecutable && !c.CanInstantiate) return false;

                if (_showOnlyWithResults && !executionResults.ContainsKey(c.CommandType)) return false;

                return true;
            }).ToList();

            if (filteredCommands.Count == 0)
            {
                var rect = EditorGUILayout.GetControlRect(false, 60);
                EditorGUI.DrawRect(rect, new Color(0.95f, 0.95f, 0.95f, 1f));
                var messageRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 40);
                EditorGUI.LabelField(messageRect, "没有找到匹配的Command\n调整过滤条件或搜索关键词", window._infoStyle);
                return;
            }

            // 统计信息
            var statsRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.LabelField(statsRect, $"当前显示: {filteredCommands.Count} 个Command", window._infoStyle);

            EditorGUILayout.Space(2);

            // Command列表
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_groupByInterface)
            {
                DrawCommandsGroupedByInterface(window, provider, filteredCommands, executionResults);
            }
            else
            {
                foreach (var commandInfo in filteredCommands.OrderBy(c => c.CommandTypeName))
                {
                    DrawCommandItem(window, provider, commandInfo, executionResults.GetValueOrDefault(commandInfo.CommandType));
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCommandsGroupedByInterface(VomitMonitorWindow window, MonitorDataProvider provider,
            List<CommandInfo> filteredCommands, Dictionary<Type, CommandExecutionResult> executionResults)
        {
            // 按接口分组
            var groupedCommands = filteredCommands
                .Where(c => c.IsNestedInInterface)
                .GroupBy(c => c.ParentInterfaceType)
                .OrderBy(g => g.Key?.Name);

            // 绘制有接口的Command
            foreach (var group in groupedCommands)
            {
                if (group.Key == null) continue;

                DrawInterfaceGroup(window, provider, group.Key, group.ToList(), executionResults);
            }

            // 绘制独立的Command
            var standaloneCommands = filteredCommands.Where(c => !c.IsNestedInInterface).ToList();
            if (standaloneCommands.Count > 0)
            {
                DrawStandaloneCommands(window, provider, standaloneCommands, executionResults);
            }
        }

        private void DrawInterfaceGroup(VomitMonitorWindow window, MonitorDataProvider provider,
            Type interfaceType, List<CommandInfo> commands, Dictionary<Type, CommandExecutionResult> executionResults)
        {
            if (!_interfaceFoldoutStates.ContainsKey(interfaceType))
            {
                _interfaceFoldoutStates[interfaceType] = true;
            }

            var executableCount = commands.Count(c => c.CanInstantiate);

            var rect = EditorGUILayout.GetControlRect(false, 26);
            var bgColor = new Color(0.6f, 0.8f, 1f, 0.3f);
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUI.DrawRect(rect, bgColor);
            GUI.backgroundColor = oldBg;

            // 折叠箭头和标题
            var foldoutRect = new Rect(rect.x + 5, rect.y + 2, rect.width - 10, 22);
            var groupTitle = $"{interfaceType.Name} ({commands.Count} Commands, {executableCount} 可执行)";
            _interfaceFoldoutStates[interfaceType] = EditorGUI.Foldout(foldoutRect,
                _interfaceFoldoutStates[interfaceType], groupTitle, true, EditorStyles.boldLabel);

            if (_interfaceFoldoutStates[interfaceType])
            {
                EditorGUI.indentLevel++;
                foreach (var commandInfo in commands.OrderBy(c => c.CommandType.Name))
                {
                    DrawCommandItem(window, provider, commandInfo, executionResults.GetValueOrDefault(commandInfo.CommandType));
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(2);
            }
        }

        private void DrawStandaloneCommands(VomitMonitorWindow window, MonitorDataProvider provider,
            List<CommandInfo> commands, Dictionary<Type, CommandExecutionResult> executionResults)
        {
            var executableCount = commands.Count(c => c.CanInstantiate);

            var rect = EditorGUILayout.GetControlRect(false, 26);
            var bgColor = new Color(0.9f, 0.9f, 0.7f, 0.3f);
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUI.DrawRect(rect, bgColor);
            GUI.backgroundColor = oldBg;

            // 折叠箭头和标题
            var foldoutRect = new Rect(rect.x + 5, rect.y + 2, rect.width - 10, 22);
            var groupTitle = $"独立Commands ({commands.Count} Commands, {executableCount} 可执行)";
            var showStandalone = EditorGUI.Foldout(foldoutRect, true, groupTitle, true, EditorStyles.boldLabel);

            if (showStandalone)
            {
                EditorGUI.indentLevel++;
                foreach (var commandInfo in commands.OrderBy(c => c.CommandTypeName))
                {
                    DrawCommandItem(window, provider, commandInfo, executionResults.GetValueOrDefault(commandInfo.CommandType));
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawCommandItem(VomitMonitorWindow window, MonitorDataProvider provider,
            CommandInfo commandInfo, CommandExecutionResult executionResult)
        {
            var canExecute = commandInfo.CanInstantiate;
            Color bgColor;

            if (executionResult != null)
            {
                bgColor = executionResult.Success ? _successStyle.normal.textColor :
                                                   new Color(1f, 0.4f, 0.4f, 0.3f);
            }
            else if (canExecute)
            {
                bgColor = new Color(0.4f, 1f, 0.6f, 0.15f);
            }
            else
            {
                bgColor = new Color(0.95f, 0.95f, 0.95f, 0.5f);
            }

            var rect = EditorGUILayout.GetControlRect(false, 24);
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUI.DrawRect(rect, bgColor);
            GUI.backgroundColor = oldBg;

            // Command名称
            var nameRect = new Rect(rect.x + 10, rect.y + 2, 150, 20);
            EditorGUI.LabelField(nameRect, commandInfo.CommandTypeName, _commandHeaderStyle);

            // 返回值类型
            var typeRect = new Rect(nameRect.xMax + 10, rect.y + 2, 80, 20);
            var typeText = commandInfo.HasReturnValue ? $"返回: {GetSimpleTypeName(commandInfo.ReturnType)}" : "无返回值";
            EditorGUI.LabelField(typeRect, typeText, window._infoStyle);

            // 可执行状态
            var statusRect = new Rect(typeRect.xMax + 10, rect.y + 2, 70, 20);
            var statusStyle = canExecute ? _executableStyle : _nonExecutableStyle;
            var statusText = canExecute ? "可执行" : "不可执行";
            EditorGUI.LabelField(statusRect, statusText, statusStyle);

            // 程序集
            var assemblyRect = new Rect(statusRect.xMax + 10, rect.y + 2, 80, 20);
            EditorGUI.LabelField(assemblyRect, commandInfo.AssemblyName, window._infoStyle);

            // 执行按钮
            var buttonRect = new Rect(rect.xMax - 60, rect.y + 1, 55, 22);
            var oldEnabled = GUI.enabled;
            GUI.enabled = canExecute;

            if (GUI.Button(buttonRect, "执行", EditorStyles.miniButton))
            {
                ExecuteCommand(provider, commandInfo);
            }

            GUI.enabled = oldEnabled;

            // 执行结果
            if (executionResult != null)
            {
                DrawExecutionResult(window, executionResult);
            }
        }

        private void DrawExecutionResult(VomitMonitorWindow window, CommandExecutionResult result)
        {
            var rect = EditorGUILayout.GetControlRect(false, 22);

            var resultColor = result.Success ? new Color(0.8f, 1f, 0.8f, 0.3f) : new Color(1f, 0.8f, 0.8f, 0.3f);
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = resultColor;
            EditorGUI.DrawRect(rect, resultColor);
            GUI.backgroundColor = oldBg;

            // 时间戳
            var timeRect = new Rect(rect.x + 10, rect.y + 2, 60, 18);
            EditorGUI.LabelField(timeRect, result.Timestamp.ToString("HH:mm:ss"), _resultStyle);

            // 执行时间
            var durationRect = new Rect(timeRect.xMax + 10, rect.y + 2, 70, 18);
            EditorGUI.LabelField(durationRect, $"{result.ExecutionTime.TotalMilliseconds:F1}ms", _resultStyle);

            // 状态
            var statusRect = new Rect(durationRect.xMax + 10, rect.y + 2, 50, 18);
            var statusStyle = result.Success ? _successStyle : _errorStyle;
            var statusText = result.Success ? "成功" : "失败";
            EditorGUI.LabelField(statusRect, statusText, statusStyle);

            // 结果值或错误信息
            var resultRect = new Rect(statusRect.xMax + 10, rect.y + 2, rect.xMax - statusRect.xMax - 20, 18);
            var resultText = result.Success ? (result.Result?.ToString() ?? "无返回值") : (result.Exception?.Message ?? "未知错误");
            EditorGUI.LabelField(resultRect, resultText, _resultStyle);
        }

        #endregion

        #region Helper Methods

        private void ExecuteCommand(MonitorDataProvider provider, CommandInfo commandInfo)
        {
            try
            {
                var result = provider.ExecuteCommand(commandInfo.CommandType);
                if (result != null)
                {
                    if (result.Success)
                    {
                        Debug.Log($"[CommandMonitorTab] 成功执行Command: {commandInfo.CommandTypeName}, 结果: {result.Result}");
                    }
                    else
                    {
                        Debug.LogError($"[CommandMonitorTab] 执行Command失败: {commandInfo.CommandTypeName}, 错误: {result.Exception?.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommandMonitorTab] 执行Command异常: {commandInfo.CommandTypeName}, 错误: {e.Message}");
            }
        }

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

        private void EnsureStylesInitialized()
        {
            if (_commandHeaderStyle != null) return;

            // 统一的颜色变量
            var lightTextColor = new Color(0.95f, 0.95f, 0.95f);
            var mediumTextColor = new Color(0.75f, 0.75f, 0.75f);
            var successColor = new Color(0.4f, 1f, 0.6f);
            var warningColor = new Color(1f, 0.85f, 0.3f);
            var errorColor = new Color(1f, 0.45f, 0.45f);

            _commandHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = lightTextColor },
                padding = new RectOffset(5, 5, 2, 2)
            };

            _executableStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = successColor },
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(5, 5, 2, 2)
            };

            _nonExecutableStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = warningColor },
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(5, 5, 2, 2)
            };

            _successStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = successColor },
                padding = new RectOffset(5, 5, 2, 2)
            };

            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = errorColor },
                padding = new RectOffset(5, 5, 2, 2)
            };

            _resultStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = lightTextColor },
                wordWrap = false,
                padding = new RectOffset(5, 5, 2, 2)
            };
        }

        #endregion
    }
}
