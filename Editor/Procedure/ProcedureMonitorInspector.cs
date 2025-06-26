using System;
using Twenty2.VomitLib.Procedure;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Procedure
{
    /// <summary>
    /// ProcedureMonitor的自定义Inspector - 运行时Procedure调试面板
    /// </summary>
    [CustomEditor(typeof(ProcedureMonitor))]
    public class ProcedureMonitorInspector : UnityEditor.Editor
    {
        #region Fields
        
        private ProcedureMonitor _procedureMonitor;
        private double _lastRefreshTime;
        
        // 折叠状态
        private bool _showConfiguration = true;
        private bool _showStateMachineInfo = true;
        private bool _showCurrentState = true;
        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _successStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _boxStyle;
        
        // 数据缓存
        private ProcedureDebugInfo _debugInfo;
        
        #endregion
        
        #region Unity Callbacks
        
        private void OnEnable()
        {
            _procedureMonitor = (ProcedureMonitor)target;
            InitializeStyles();
            RefreshDebugInfo();
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // 绘制配置部分
            DrawConfigurationSection();
            
            EditorGUILayout.Space(10);
            
            // 只在运行时显示调试信息
            if (Application.isPlaying)
            {
                DrawRuntimeDebugPanel();
            }
            else
            {
                DrawPlayModeInfo();
            }
            
            // 自动刷新
            var autoRefreshProperty = serializedObject.FindProperty("_autoRefresh");
            var refreshIntervalProperty = serializedObject.FindProperty("_refreshInterval");
            
            if (autoRefreshProperty.boolValue && Application.isPlaying && 
                EditorApplication.timeSinceStartup - _lastRefreshTime > refreshIntervalProperty.floatValue)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                RefreshDebugInfo();
                Repaint();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion
        
        #region GUI Drawing
        
        /// <summary>
        /// 绘制配置部分
        /// </summary>
        private void DrawConfigurationSection()
        {
            _showConfiguration = EditorGUILayout.Foldout(_showConfiguration, "🔧 监控配置", true, _subHeaderStyle);
            if (!_showConfiguration) return;
            
            EditorGUILayout.BeginVertical(_boxStyle);
            
            // 类型名称输入
            EditorGUILayout.LabelField("泛型类型全名", _headerStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_genericTypeFullName"), GUIContent.none);
            
            EditorGUILayout.Space(5);
            
            // 示例提示
            EditorGUILayout.LabelField("格式示例:", EditorStyles.miniLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("• 嵌套枚举: Game+State", _infoStyle);
            EditorGUILayout.LabelField("• 独立枚举: GameState", _infoStyle);
            EditorGUILayout.LabelField("• 完整类名: MyProject.States.GameState", _infoStyle);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // 自动刷新设置
            EditorGUILayout.BeginHorizontal();
            
            var autoRefreshProperty = serializedObject.FindProperty("_autoRefresh");
            var refreshIntervalProperty = serializedObject.FindProperty("_refreshInterval");
            
            // 自动刷新Toggle
            autoRefreshProperty.boolValue = EditorGUILayout.Toggle(new GUIContent("自动刷新", "启用后会定期自动刷新状态信息"), autoRefreshProperty.boolValue);
            
            // 刷新间隔输入框
            if (autoRefreshProperty.boolValue)
            {
                EditorGUILayout.LabelField("间隔(秒):", GUILayout.Width(50));
                refreshIntervalProperty.floatValue = EditorGUILayout.FloatField(refreshIntervalProperty.floatValue, GUILayout.Width(60));
                
                // 限制最小值
                if (refreshIntervalProperty.floatValue < 0.1f)
                {
                    refreshIntervalProperty.floatValue = 0.1f;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 显示当前设置的提示
            if (autoRefreshProperty.boolValue)
            {
                var intervalText = refreshIntervalProperty.floatValue < 1f 
                    ? $"{refreshIntervalProperty.floatValue * 1000:F0}毫秒" 
                    : $"{refreshIntervalProperty.floatValue:F1}秒";
                EditorGUILayout.LabelField($"• 将每{intervalText}自动刷新一次状态信息", _infoStyle);
            }
            else
            {
                EditorGUILayout.LabelField("• 已禁用自动刷新，需要手动点击刷新按钮", _infoStyle);
            }
            
            EditorGUILayout.Space(5);
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            
            if (Application.isPlaying && GUILayout.Button("🔄 刷新", _buttonStyle))
            {
                _procedureMonitor.ManualRefresh();
                RefreshDebugInfo();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制非运行时提示
        /// </summary>
        private void DrawPlayModeInfo()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("🔄 Procedure监控面板", _headerStyle);
            EditorGUILayout.LabelField("请在运行时查看Procedure系统调试信息", _infoStyle);
            
            if (GUILayout.Button("进入Play模式", GUILayout.Height(30)))
            {
                EditorApplication.isPlaying = true;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制运行时调试面板
        /// </summary>
        private void DrawRuntimeDebugPanel()
        {
            if (_debugInfo == null)
            {
                RefreshDebugInfo();
            }
            
            // 面板头部
            DrawPanelHeader();
            
            EditorGUILayout.Space(5);
            
            if (_debugInfo?.IsValid == true)
            {
                // 状态机信息
                DrawStateMachineInfo();
                
                // 当前状态信息
                DrawCurrentStateInfo();
            }
            else
            {
                DrawErrorInfo();
            }
        }
        
        /// <summary>
        /// 绘制面板头部
        /// </summary>
        private void DrawPanelHeader()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("🔍 Procedure系统监控", _headerStyle);
            
            GUILayout.FlexibleSpace();
            
            // 状态指示灯
            if (_debugInfo?.IsValid == true)
            {
                var statusText = _debugInfo.IsRunning ? "🟢 运行中" : "🔴 已停止";
                var statusStyle = _debugInfo.IsRunning ? _successStyle : _errorStyle;
                EditorGUILayout.LabelField(statusText, statusStyle, GUILayout.Width(80));
            }
            else
            {
                EditorGUILayout.LabelField("❌ 连接失败", _errorStyle, GUILayout.Width(80));
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制状态机信息
        /// </summary>
        private void DrawStateMachineInfo()
        {
            _showStateMachineInfo = EditorGUILayout.Foldout(_showStateMachineInfo, "📊 状态机总览", true, _subHeaderStyle);
            if (!_showStateMachineInfo) return;
            
            EditorGUILayout.BeginVertical(_boxStyle);
            
            EditorGUILayout.BeginHorizontal();
            DrawInfoItem("运行状态", _debugInfo.IsRunning ? "✅ 运行中" : "❌ 已停止", 
                _debugInfo.IsRunning ? _successStyle : _errorStyle);
            DrawInfoItem("初始化", _debugInfo.IsInitialized ? "✅ 已初始化" : "❌ 未初始化", 
                _debugInfo.IsInitialized ? _successStyle : _errorStyle);
            DrawInfoItem("注册状态数", _debugInfo.RegisteredStatesCount.ToString(), _infoStyle);
            EditorGUILayout.EndHorizontal();
            
            if (_debugInfo.IsRunning)
            {
                EditorGUILayout.BeginHorizontal();
                DrawInfoItem("运行时间", FormatTime(_debugInfo.StateTime), _infoStyle);
                DrawInfoItem("运行帧数", _debugInfo.FrameCount.ToString(), _infoStyle);
                DrawInfoItem("状态机类型", _debugInfo.StateMachineTypeName, _infoStyle);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// 绘制当前状态信息
        /// </summary>
        private void DrawCurrentStateInfo()
        {
            _showCurrentState = EditorGUILayout.Foldout(_showCurrentState, "🎯 当前状态", true, _subHeaderStyle);
            if (!_showCurrentState) return;
            
            EditorGUILayout.BeginVertical(_boxStyle);
            
            if (_debugInfo.CurrentStateId != null)
            {
                var originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f, 0.3f);
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = originalBgColor;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("🔸", GUILayout.Width(20));
                EditorGUILayout.LabelField(_debugInfo.CurrentStateId.ToString(), EditorStyles.boldLabel);
                
                // 状态类型
                var originalColor = GUI.contentColor;
                GUI.contentColor = Color.cyan;
                EditorGUILayout.LabelField($"[{_debugInfo.CurrentStateTypeName ?? "Unknown"}]", EditorStyles.miniLabel, GUILayout.Width(100));
                GUI.contentColor = originalColor;
                
                EditorGUILayout.EndHorizontal();
                
                // 状态详细信息
                var stateInfo = $"运行时间: {FormatTime(_debugInfo.StateTime)} | 运行帧数: {_debugInfo.FrameCount}";
                if (_debugInfo.PreviousStateId != null)
                {
                    stateInfo += $" | 上一状态: {_debugInfo.PreviousStateId}";
                }
                EditorGUILayout.LabelField(stateInfo, _infoStyle);
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.LabelField("⚪ 当前没有活动状态", _infoStyle);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// 绘制错误信息
        /// </summary>
        private void DrawErrorInfo()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            var originalColor = GUI.contentColor;
            GUI.contentColor = Color.red;
            EditorGUILayout.LabelField("⚠️ 连接状态", EditorStyles.boldLabel);
            GUI.contentColor = originalColor;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(_debugInfo?.ErrorMessage ?? "未知错误", _errorStyle);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 手动刷新", _buttonStyle))
            {
                _procedureMonitor.ManualRefresh();
                RefreshDebugInfo();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        
        /// <summary>
        /// 绘制信息项
        /// </summary>
        private void DrawInfoItem(string label, string value, GUIStyle style)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(value, style ?? _infoStyle);
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }
        
        #endregion
        
        #region Data Management
        
        /// <summary>
        /// 刷新调试信息
        /// </summary>
        private void RefreshDebugInfo()
        {
            if (!Application.isPlaying) return;
            
            _debugInfo = ExtractDebugInfo();
        }
        
        /// <summary>
        /// 从ProcedureMonitor提取调试信息
        /// </summary>
        private ProcedureDebugInfo ExtractDebugInfo()
        {
            var info = new ProcedureDebugInfo();
            
            try
            {
                // 通过反射获取ProcedureMonitor的私有字段
                var monitorType = typeof(ProcedureMonitor);
                var stateMachineInstanceField = monitorType.GetField("_stateMachineInstance", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var stateMachineTypeField = monitorType.GetField("_stateMachineType", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                var stateMachineInstance = stateMachineInstanceField?.GetValue(_procedureMonitor);
                var stateMachineType = stateMachineTypeField?.GetValue(_procedureMonitor) as Type;
                
                if (stateMachineInstance == null || stateMachineType == null)
                {
                    info.ErrorMessage = "状态机未正确初始化";
                    return info;
                }
                
                // 获取状态机信息
                info.IsRunning = GetPropertyValue<bool>(stateMachineInstance, stateMachineType, "IsRunning");
                info.IsInitialized = GetPropertyValue<bool>(stateMachineInstance, stateMachineType, "IsInitialized");
                info.CurrentStateId = GetPropertyValue<object>(stateMachineInstance, stateMachineType, "CurrentStateId");
                info.PreviousStateId = GetPropertyValue<object>(stateMachineInstance, stateMachineType, "PreviousStateId");
                info.FrameCount = GetPropertyValue<long>(stateMachineInstance, stateMachineType, "FrameCount");
                info.StateTime = GetPropertyValue<float>(stateMachineInstance, stateMachineType, "StateTime");
                info.StateMachineTypeName = stateMachineType.Name;
                
                // 获取注册状态数量
                var registeredStatesField = stateMachineType.GetField("_registeredStates", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (registeredStatesField != null)
                {
                    var registeredStates = registeredStatesField.GetValue(stateMachineInstance);
                    if (registeredStates != null)
                    {
                        var countProperty = registeredStates.GetType().GetProperty("Count");
                        if (countProperty != null)
                        {
                            info.RegisteredStatesCount = (int)countProperty.GetValue(registeredStates);
                        }
                    }
                }
                
                // 获取当前状态类型名称
                if (info.CurrentStateId != null)
                {
                    info.CurrentStateTypeName = info.CurrentStateId.GetType().Name;
                }
                
                info.IsValid = true;
            }
            catch (Exception e)
            {
                info.ErrorMessage = $"获取调试信息失败: {e.Message}";
            }
            
            return info;
        }
        
        /// <summary>
        /// 获取属性值
        /// </summary>
        private T GetPropertyValue<T>(object instance, Type type, string propertyName)
        {
            try
            {
                var property = type.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (property != null)
                {
                    var value = property.GetValue(instance);
                    if (value is T) return (T)value;
                    if (value != null) return (T)Convert.ChangeType(value, typeof(T));
                }
                return default(T);
            }
            catch
            {
                return default(T);
            }
        }
        
        
        /// <summary>
        /// 格式化时间显示
        /// </summary>
        private string FormatTime(float time)
        {
            if (time < 60f)
                return $"{time:F1}s";
            
            int minutes = (int)(time / 60);
            float seconds = time % 60;
            return $"{minutes}m {seconds:F1}s";
        }
        
        #endregion
        
        #region Styles
        
        /// <summary>
        /// 初始化样式
        /// </summary>
        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            _subHeaderStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            
            _infoStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.gray : Color.black }
            };
            
            _successStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.green }
            };
            
            _warningStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.yellow }
            };
            
            _errorStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.red }
            };
            
            _buttonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 10
            };
            
            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 6, 6)
            };
        }
        
        #endregion
        
        #region Data Classes
        
        /// <summary>
        /// Procedure调试信息
        /// </summary>
        private class ProcedureDebugInfo
        {
            public bool IsValid;
            public string ErrorMessage;
            
            // 状态机信息
            public bool IsRunning;
            public bool IsInitialized;
            public long FrameCount;
            public float StateTime;
            public string StateMachineTypeName;
            
            // 当前状态信息
            public object CurrentStateId;
            public object PreviousStateId;
            public string CurrentStateTypeName;
            
            // 注册状态信息
            public int RegisteredStatesCount;
        }
        
        #endregion
    }
}