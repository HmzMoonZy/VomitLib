using Twenty2.VomitLib.Monitor;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// VomitMonitor根组件的专用Editor
    /// </summary>
    [CustomEditor(typeof(VomitMonitor))]
    public class VomitMonitorEditor : UnityEditor.Editor
    {
        private VomitMonitor _vomitMonitor;
        
        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _successStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _boxStyle;
        
        private void OnEnable()
        {
            _vomitMonitor = target as VomitMonitor;
            InitializeStyles();
        }
        
        public override void OnInspectorGUI()
        {
            if (_headerStyle == null) InitializeStyles();
            
            // 绘制VomitMonitor专用Inspector
            DrawVomitMonitorInspector();
            
            // 自动刷新
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
        
        /// <summary>
        /// 绘制VomitMonitor Inspector
        /// </summary>
        private void DrawVomitMonitorInspector()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            // 头部信息
            EditorGUILayout.LabelField("VomitLib 监控器", _headerStyle);
            EditorGUILayout.LabelField("自动管理的监控系统 (仅Editor模式)", _infoStyle);
            
            EditorGUILayout.Space(10);
            
            // 基本信息
            DrawBasicInfo();
            
            EditorGUILayout.Space(10);
            
            // 监控器状态
            DrawMonitorStatus();
            
            EditorGUILayout.Space(10);
            
            // View监控信息
            DrawViewMonitorInfo();
            
            EditorGUILayout.Space(10);
            
            // Procedure监控信息
            DrawProcedureMonitorInfo();
            
            EditorGUILayout.Space(10);
            
            // 操作按钮
            DrawActionButtons();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制基本信息
        /// </summary>
        private void DrawBasicInfo()
        {
            EditorGUILayout.LabelField("基本信息", _subHeaderStyle);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var isInitialized = _vomitMonitor.IsFullyInitialized;
            EditorGUILayout.BeginHorizontal();
            DrawStatusItem("初始化状态", isInitialized ? "[OK]" : "[INCOMPLETE]", 
                isInitialized ? _successStyle : _errorStyle);
            DrawStatusItem("运行模式", Application.isPlaying ? "Play模式" : "Edit模式", _infoStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制监控器状态
        /// </summary>
        private void DrawMonitorStatus()
        {
            EditorGUILayout.LabelField("子监控器状态", _subHeaderStyle);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var viewMonitor = _vomitMonitor.ViewMonitor;
            var procedureMonitor = _vomitMonitor.ProcedureMonitor;
            
            EditorGUILayout.BeginHorizontal();
            DrawStatusItem("ViewMonitor", viewMonitor != null ? "[ACTIVE]" : "[MISSING]", 
                viewMonitor != null ? _successStyle : _errorStyle);
            DrawStatusItem("ProcedureMonitor", procedureMonitor != null ? "[ACTIVE]" : "[MISSING]", 
                procedureMonitor != null ? _successStyle : _errorStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制View监控信息
        /// </summary>
        private void DrawViewMonitorInfo()
        {
            EditorGUILayout.LabelField("View 系统监控", _subHeaderStyle);
            
            var viewMonitor = _vomitMonitor.ViewMonitor;
            if (viewMonitor != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                var debugInfo = viewMonitor.GetDebugInfo();
                if (debugInfo?.IsValid == true)
                {
                    EditorGUILayout.BeginHorizontal();
                    DrawStatusItem("已打开", debugInfo.OpenedCount.ToString(), _infoStyle);
                    DrawStatusItem("预加载", debugInfo.PreloadedCount.ToString(), _infoStyle);
                    DrawStatusItem("加载中", debugInfo.LoadingCount.ToString(), _infoStyle);
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField($"[ERROR] {debugInfo?.ErrorMessage ?? "获取信息失败"}", _errorStyle);
                }
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("[ERROR] ViewMonitor组件缺失", _errorStyle);
                EditorGUILayout.EndVertical();
            }
        }
        
        /// <summary>
        /// 绘制Procedure监控信息
        /// </summary>
        private void DrawProcedureMonitorInfo()
        {
            EditorGUILayout.LabelField("Procedure 系统监控", _subHeaderStyle);
            
            var procedureMonitor = _vomitMonitor.ProcedureMonitor;
            if (procedureMonitor != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                var debugInfo = procedureMonitor.GetDebugInfo();
                if (debugInfo?.IsValid == true)
                {
                    EditorGUILayout.BeginHorizontal();
                    DrawStatusItem("运行状态", debugInfo.IsRunning ? "[RUNNING]" : "[STOPPED]", 
                        debugInfo.IsRunning ? _successStyle : _errorStyle);
                    DrawStatusItem("当前状态", debugInfo.CurrentStateId?.ToString() ?? "无", _infoStyle);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    DrawStatusItem("运行时间", $"{debugInfo.StateTime:F1}s", _infoStyle);
                    DrawStatusItem("运行帧数", debugInfo.FrameCount.ToString(), _infoStyle);
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField($"[ERROR] {debugInfo?.ErrorMessage ?? "获取信息失败"}", _errorStyle);
                }
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("[ERROR] ProcedureMonitor组件缺失", _errorStyle);
                EditorGUILayout.EndVertical();
            }
        }
        
        /// <summary>
        /// 绘制操作按钮
        /// </summary>
        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("操作", _subHeaderStyle);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("刷新所有监控器"))
            {
                _vomitMonitor.RefreshAllMonitors();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (_vomitMonitor.ViewMonitor != null && GUILayout.Button("选择ViewMonitor"))
            {
                Selection.activeGameObject = _vomitMonitor.ViewMonitor.gameObject;
            }
            
            if (_vomitMonitor.ProcedureMonitor != null && GUILayout.Button("选择ProcedureMonitor"))
            {
                Selection.activeGameObject = _vomitMonitor.ProcedureMonitor.gameObject;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制状态项
        /// </summary>
        private void DrawStatusItem(string label, string value, GUIStyle style)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(value, style);
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }
        
        /// <summary>
        /// 初始化样式
        /// </summary>
        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
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
                normal = { textColor = Color.green }
            };
            
            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = Color.red }
            };
            
            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8)
            };
        }
    }
}