using System.Collections.Generic;
using System.Linq;
using Twenty2.VomitLib.Monitor;
using Twenty2.VomitLib.Procedure;
using UnityEditor;
using UnityEngine;
using System;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// ProcedureMonitor的自定义Inspector - Procedure系统监控面板
    /// 提供状态机监控、状态切换和运行时信息查看功能
    /// </summary>
    [CustomEditor(typeof(ProcedureMonitor))]
    public class ProcedureMonitorInspector : UnityEditor.Editor
    {
        #region Fields

        private ProcedureMonitor _monitor;
        private double _lastRefreshTime;

        // 界面状态
        private bool _showSettings = false;
        private bool _showStateMachineInfo = true;
        private bool _showStates = true;
        private bool _showRuntimeInfo = true;
        
        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _fieldStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _stateMachineHeaderStyle;
        private GUIStyle _stateStyle;
        private GUIStyle _currentStateStyle;

        // 颜色
        private readonly Color _stateMachineColor = new Color(0.6f, 0.9f, 0.6f, 0.3f);
        private readonly Color _fieldColor = new Color(0.9f, 0.9f, 0.9f, 0.1f);
        private readonly Color _runningColor = new Color(0.4f, 1f, 0.4f, 0.3f);
        private readonly Color _stoppedColor = new Color(1f, 0.4f, 0.4f, 0.3f);
        private readonly Color _currentStateColor = new Color(0.9f, 0.7f, 0.2f, 0.4f);
        private readonly Color _normalStateColor = new Color(0.85f, 0.95f, 1f, 0.3f);

        // GUI操作相关
        private bool _needsRefresh;
        private Vector2 _statesScrollPosition;

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            _monitor = (ProcedureMonitor)target;
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
                DrawProcedureMonitoring();
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
            EditorGUI.DrawRect(rect, _stateMachineColor);
            
            var headerRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 30);
            EditorGUI.LabelField(headerRect, "VomitLib Procedure Monitor", _headerStyle);
            
            if (Application.isPlaying && _monitor.EnableMonitoring)
            {
                var stateMachineInfo = _monitor.GetStateMachineInfo();
                var runtimeInfo = _monitor.GetRuntimeInfo();
                
                string statusText;
                if (stateMachineInfo != null)
                {
                    var isRunning = runtimeInfo?.IsRunning ?? false;
                    statusText = $"监控中 - {stateMachineInfo.StateMachineTypeName} ({(isRunning ? "运行中" : "已停止")})";
                }
                else
                {
                    statusText = "未找到状态机";
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
                EditorGUILayout.LabelField("状态机配置", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_stateEnumTypeName"), new GUIContent("状态枚举类型名"));
                EditorGUILayout.HelpBox("支持格式: Game+State, GameState, MyNamespace.GameState 等", MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("刷新数据", _buttonStyle))
                {
                    _monitor.RefreshProcedureData();
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
            EditorGUI.DrawRect(rect, _stoppedColor);
            
            var messageRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 40);
            EditorGUI.LabelField(messageRect, "Procedure监控功能仅在运行模式下可用\n请启动游戏来监控状态机和状态", _fieldStyle);
        }

        /// <summary>
        /// 绘制Procedure监控主面板
        /// </summary>
        private void DrawProcedureMonitoring()
        {
            var stateMachineInfo = _monitor.GetStateMachineInfo();
            var runtimeInfo = _monitor.GetRuntimeInfo();
            
            if (stateMachineInfo == null)
            {
                var rect = EditorGUILayout.GetControlRect(false, 40);
                EditorGUI.DrawRect(rect, _stoppedColor);
                var messageRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 20);
                EditorGUI.LabelField(messageRect, "未找到状态机，请检查配置", _fieldStyle);
                return;
            }
            
            DrawStateMachineInfo(stateMachineInfo, runtimeInfo);
        }


        /// <summary>
        /// 绘制状态机信息
        /// </summary>
        private void DrawStateMachineInfo(ProcedureStateMachineInfo stateMachineInfo, ProcedureRuntimeInfo runtimeInfo)
        {
            var isRunning = runtimeInfo?.IsRunning ?? false;
            var backgroundColor = isRunning ? _runningColor : _stoppedColor;
            
            // 状态机头部
            var headerRect = EditorGUILayout.GetControlRect(false, 25);
            EditorGUI.DrawRect(headerRect, backgroundColor);
            
            var headerText = $"{stateMachineInfo.StateMachineTypeName}<{stateMachineInfo.StateEnumTypeName}>";
            if (runtimeInfo != null)
            {
                headerText += $" - {(isRunning ? "运行中" : "已停止")} - 当前: {runtimeInfo.CurrentStateId}";
            }
            
            var headerLabelRect = new Rect(headerRect.x + 10, headerRect.y + 4, headerRect.width - 20, 16);
            EditorGUI.LabelField(headerLabelRect, headerText, _stateMachineHeaderStyle);
            
            EditorGUILayout.Space(5);
            
            // 基本信息折叠面板
            DrawStateMachineBasicInfoFoldout(stateMachineInfo, runtimeInfo);
            
            // 运行时信息折叠面板
            if (runtimeInfo != null)
            {
                DrawRuntimeInfoFoldout(runtimeInfo);
            }
            
            // 状态列表折叠面板
            DrawStatesListFoldout(stateMachineInfo, runtimeInfo);
        }

        /// <summary>
        /// 绘制状态机基本信息折叠面板
        /// </summary>
        private void DrawStateMachineBasicInfoFoldout(ProcedureStateMachineInfo stateMachineInfo, ProcedureRuntimeInfo runtimeInfo)
        {
            var rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(rect, _fieldColor);
            
            _showStateMachineInfo = EditorGUI.Foldout(new Rect(rect.x + 5, rect.y, rect.width - 10, rect.height),
                _showStateMachineInfo, "状态机信息", true, _subHeaderStyle);

            if (_showStateMachineInfo)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField($"类型: {stateMachineInfo.StateMachineTypeName}", _fieldStyle);
                EditorGUILayout.LabelField($"状态枚举: {stateMachineInfo.StateEnumTypeName}", _fieldStyle);
                EditorGUILayout.LabelField($"程序集: {stateMachineInfo.AssemblyName}", _fieldStyle);
                
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// 绘制运行时信息折叠面板
        /// </summary>
        private void DrawRuntimeInfoFoldout(ProcedureRuntimeInfo runtimeInfo)
        {
            var rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(rect, _fieldColor);
            
            _showRuntimeInfo = EditorGUI.Foldout(new Rect(rect.x + 5, rect.y, rect.width - 10, rect.height),
                _showRuntimeInfo, "运行时信息", true, _subHeaderStyle);

            if (_showRuntimeInfo)
            {
                EditorGUI.indentLevel++;
                
                var statusText = $"{(runtimeInfo.IsInitialized ? "已初始化" : "未初始化")} | {(runtimeInfo.IsRunning ? "运行中" : "已停止")}";
                EditorGUILayout.LabelField($"状态: {statusText}", _fieldStyle);
                EditorGUILayout.LabelField($"当前状态: {runtimeInfo.CurrentStateId}", _fieldStyle);
                EditorGUILayout.LabelField($"上一状态: {runtimeInfo.PreviousStateId}", _fieldStyle);
                EditorGUILayout.LabelField($"运行时间: {runtimeInfo.StateTime:F1}s", _fieldStyle);
                EditorGUILayout.LabelField($"帧数: {runtimeInfo.FrameCount}", _fieldStyle);
                EditorGUILayout.LabelField($"更新时间: {runtimeInfo.LastUpdateTime:HH:mm:ss}", _fieldStyle);
                
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// 绘制状态列表折叠面板
        /// </summary>
        private void DrawStatesListFoldout(ProcedureStateMachineInfo stateMachineInfo, ProcedureRuntimeInfo runtimeInfo)
        {
            var stateInfos = _monitor.GetStateInfos();
            if (stateInfos == null || stateInfos.Count == 0)
            {
                return;
            }
            
            var rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(rect, _fieldColor);
            
            _showStates = EditorGUI.Foldout(new Rect(rect.x + 5, rect.y, rect.width - 10, rect.height),
                _showStates, $"状态列表 ({stateInfos.Count})", true, _subHeaderStyle);

            if (_showStates)
            {
                EditorGUI.indentLevel++;
                
                // 状态列表滚动区域
                _statesScrollPosition = EditorGUILayout.BeginScrollView(_statesScrollPosition, 
                    GUILayout.Height(Mathf.Min(200, stateInfos.Count * 30 + 20)));
                
                foreach (var stateInfo in stateInfos)
                {
                    var isCurrent = runtimeInfo?.CurrentStateId?.Equals(stateInfo.StateId) ?? false;
                    var stateColor = isCurrent ? _currentStateColor : _normalStateColor;
                    
                    var stateRect = EditorGUILayout.GetControlRect(false, 25);
                    EditorGUI.DrawRect(stateRect, stateColor);
                    
                    // 状态名称
                    var nameRect = new Rect(stateRect.x + 10, stateRect.y + 4, 150, 16);
                    var stateText = isCurrent ? $"► {stateInfo.StateName}" : stateInfo.StateName;
                    EditorGUI.LabelField(nameRect, stateText, isCurrent ? _currentStateStyle : _stateStyle);
                    
                    // 切换按钮
                    if (runtimeInfo?.IsRunning == true)
                    {
                        var changeButtonRect = new Rect(stateRect.xMax - 120, stateRect.y + 2, 55, 20);
                        EditorGUI.BeginDisabledGroup(isCurrent);
                        if (GUI.Button(changeButtonRect, "切换", _buttonStyle))
                        {
                            _monitor.ChangeState(stateInfo.StateId);
                        }
                        EditorGUI.EndDisabledGroup();
                        
                        var forceButtonRect = new Rect(changeButtonRect.xMax + 5, stateRect.y + 2, 55, 20);
                        if (GUI.Button(forceButtonRect, "强制", _buttonStyle))
                        {
                            _monitor.ForceChangeState(stateInfo.StateId);
                        }
                    }
                }
                
                EditorGUILayout.EndScrollView();
                
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

            _stateMachineHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = Color.black }
            };

            _stateStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.2f, 0.4f, 0.8f, 1f) }
            };

            _currentStateStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.8f, 0.4f, 0.0f, 1f) }
            };
        }

        /// <summary>
        /// 刷新监控数据
        /// </summary>
        private void RefreshMonitorData()
        {
            if (_monitor == null || !_monitor.EnableMonitoring) return;
            
            _monitor.RefreshProcedureData();
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