using Twenty2.VomitLib.Monitor;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// ProcedureMonitor专用Editor - 美化的状态机监控面板
    /// </summary>
    [CustomEditor(typeof(ProcedureMonitor))]
    public class ProcedureMonitorEditor : UnityEditor.Editor
    {
        private ProcedureMonitor _monitor;
        
        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _successStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _boldStyle;
        
        // 状态信息
        private ProcedureMonitor.ProcedureDebugInfo _lastDebugInfo;
        private double _lastUpdateTime;
        
        private void OnEnable()
        {
            _monitor = target as ProcedureMonitor;
            InitializeStyles();
        }
        
        public override void OnInspectorGUI()
        {
            if (_headerStyle == null) InitializeStyles();
            
            // 更新调试信息
            if (Application.isPlaying)
            {
                _lastDebugInfo = _monitor.GetDebugInfo();
                _lastUpdateTime = EditorApplication.timeSinceStartup;
            }
            
            DrawProcedureMonitorInspector();
            
            // 自动刷新
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
        
        /// <summary>
        /// 绘制Procedure监控Inspector
        /// </summary>
        private void DrawProcedureMonitorInspector()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            // 标题
            EditorGUILayout.LabelField("Procedure 状态机监控", _headerStyle);
            EditorGUILayout.LabelField("实时监控状态机运行状态和切换历史", _infoStyle);
            
            EditorGUILayout.Space(10);
            
            if (!Application.isPlaying)
            {
                DrawEditModeInfo();
            }
            else if (_lastDebugInfo?.IsValid == true)
            {
                DrawRuntimeInfo();
            }
            else
            {
                DrawErrorInfo();
            }
            
            EditorGUILayout.Space(10);
            DrawControlButtons();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制编辑模式信息
        /// </summary>
        private void DrawEditModeInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("编辑模式", _centerStyle);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("请进入Play模式查看状态机监控信息", _infoStyle);
            
            if (GUILayout.Button("进入Play模式", GUILayout.Height(30)))
            {
                EditorApplication.isPlaying = true;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制运行时信息
        /// </summary>
        private void DrawRuntimeInfo()
        {
            // 基本状态信息
            DrawBasicStatus();
            
            EditorGUILayout.Space(8);
            
            // 当前状态详情
            DrawCurrentStateDetails();
            
            EditorGUILayout.Space(8);
            
            // 性能统计
            DrawPerformanceStats();
            
            EditorGUILayout.Space(8);
            
            // 状态历史
            DrawStateHistory();
        }
        
        /// <summary>
        /// 绘制基本状态
        /// </summary>
        private void DrawBasicStatus()
        {
            EditorGUILayout.LabelField("基本状态", _subHeaderStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            // 运行状态
            var statusText = _lastDebugInfo.IsRunning ? "RUNNING" : "STOPPED";
            var statusStyle = _lastDebugInfo.IsRunning ? _successStyle : _errorStyle;
            DrawStatusCard("运行状态", statusText, statusStyle);
            
            // 当前状态
            var currentState = _lastDebugInfo.CurrentStateId?.ToString() ?? "None";
            DrawStatusCard("当前状态", currentState, _boldStyle);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            // 状态机类型
            DrawStatusCard("状态机类型", _lastDebugInfo.StateMachineType ?? "Unknown", _infoStyle);
            
            // 注册状态数
            DrawStatusCard("注册状态数", _lastDebugInfo.RegisteredStatesCount.ToString(), _infoStyle);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制当前状态详情
        /// </summary>
        private void DrawCurrentStateDetails()
        {
            EditorGUILayout.LabelField("当前状态详情", _subHeaderStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (_lastDebugInfo.IsRunning && _lastDebugInfo.CurrentStateId != null)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 状态运行时间
                var timeText = $"{_lastDebugInfo.StateTime:F2}s";
                DrawStatusCard("运行时间", timeText, _successStyle);
                
                // 运行帧数
                DrawStatusCard("运行帧数", _lastDebugInfo.FrameCount.ToString(), _infoStyle);
                
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("状态机未运行或状态为空", _errorStyle);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制性能统计
        /// </summary>
        private void DrawPerformanceStats()
        {
            EditorGUILayout.LabelField("性能统计", _subHeaderStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            // FPS计算
            var fps = _lastDebugInfo.StateTime > 0 ? _lastDebugInfo.FrameCount / _lastDebugInfo.StateTime : 0;
            var fpsText = $"{fps:F1} FPS";
            var fpsStyle = fps > 30 ? _successStyle : fps > 15 ? _warningStyle : _errorStyle;
            DrawStatusCard("平均FPS", fpsText, fpsStyle);
            
            // 更新频率
            var updateText = "1.0s";
            DrawStatusCard("监控频率", updateText, _infoStyle);
            
            EditorGUILayout.EndHorizontal();
            
            // 最后更新时间
            var lastUpdateText = $"{System.DateTime.Now:HH:mm:ss}";
            DrawStatusCard("最后更新", lastUpdateText, _infoStyle);
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制状态历史
        /// </summary>
        private void DrawStateHistory()
        {
            EditorGUILayout.LabelField("状态历史", _subHeaderStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (_lastDebugInfo.StateHistory != null && _lastDebugInfo.StateHistory.Length > 0)
            {
                EditorGUILayout.LabelField("最近状态切换:", _boldStyle);
                
                for (int i = 0; i < Mathf.Min(_lastDebugInfo.StateHistory.Length, 5); i++)
                {
                    var historyItem = _lastDebugInfo.StateHistory[i];
                    EditorGUILayout.LabelField($"  {i + 1}. {historyItem}", _infoStyle);
                }
                
                if (_lastDebugInfo.StateHistory.Length > 5)
                {
                    EditorGUILayout.LabelField($"  ... 共{_lastDebugInfo.StateHistory.Length}条记录", _infoStyle);
                }
            }
            else
            {
                EditorGUILayout.LabelField("暂无状态切换历史", _infoStyle);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制错误信息
        /// </summary>
        private void DrawErrorInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("监控错误", _errorStyle);
            EditorGUILayout.Space(5);
            
            var errorMsg = _lastDebugInfo?.ErrorMessage ?? "获取状态机信息失败";
            EditorGUILayout.LabelField($"错误信息: {errorMsg}", _errorStyle);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("可能的原因:", _boldStyle);
            EditorGUILayout.LabelField("• 状态机尚未初始化", _infoStyle);
            EditorGUILayout.LabelField("• 状态机类型配置错误", _infoStyle);
            EditorGUILayout.LabelField("• 反射访问权限问题", _infoStyle);
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制控制按钮
        /// </summary>
        private void DrawControlButtons()
        {
            EditorGUILayout.LabelField("控制操作", _subHeaderStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("手动刷新", GUILayout.Height(25)))
            {
                _monitor.ManualRefresh();
            }
            
            if (GUILayout.Button("重置监控", GUILayout.Height(25)))
            {
                // 重置监控状态的功能可以在这里实现
                Debug.Log("Procedure监控器已重置");
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (Application.isPlaying && GUILayout.Button("选择VomitMonitor", GUILayout.Height(25)))
            {
                var vomitMonitor = VomitMonitorManager.VomitMonitor;
                if (vomitMonitor != null)
                {
                    Selection.activeGameObject = vomitMonitor.gameObject;
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制状态卡片
        /// </summary>
        private void DrawStatusCard(string label, string value, GUIStyle valueStyle)
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(80));
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(value, valueStyle);
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }
        
        /// <summary>
        /// 初始化样式
        /// </summary>
        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black },
                alignment = TextAnchor.MiddleCenter
            };
            
            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.cyan : Color.blue }
            };
            
            _infoStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.gray : Color.black }
            };
            
            _successStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green }
            };
            
            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.6f, 0f) } // Orange
            };
            
            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.red }
            };
            
            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(15, 15, 10, 10)
            };
            
            _centerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            _boldStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
        }
    }
}