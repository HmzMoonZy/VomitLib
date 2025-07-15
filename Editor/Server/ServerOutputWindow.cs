using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Server
{
    public class ServerOutputWindow : EditorWindow
    {
        private List<string> _outputLines = new List<string>();
        private Vector2 _scrollPosition;
        private bool _autoScroll = true;
        private string _filterText = "";
        private bool _showTimestamp = true;
        private bool _wordWrap = true;
        private int _maxLines = 1000;
        
        private GUIStyle _logStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _warningStyle;
        
        private void OnEnable()
        {
            titleContent = new GUIContent("服务器输出", "显示服务器运行日志");
            
            // 订阅服务器输出事件
            ServerManagerTool.OnServerOutput += OnServerOutput;
            ServerManagerTool.OnServerStatusChanged += OnServerStatusChanged;
            
            // 初始化样式
            InitializeStyles();
        }
        
        private void OnDisable()
        {
            // 取消订阅事件
            ServerManagerTool.OnServerOutput -= OnServerOutput;
            ServerManagerTool.OnServerStatusChanged -= OnServerStatusChanged;
        }
        
        private void InitializeStyles()
        {
            _logStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true,
                fontSize = 11
            };
            
            _errorStyle = new GUIStyle(_logStyle)
            {
                normal = { textColor = Color.red }
            };
            
            _warningStyle = new GUIStyle(_logStyle)
            {
                normal = { textColor = Color.yellow }
            };
        }
        
        private void OnServerOutput(string output)
        {
            var timestamp = _showTimestamp ? $"[{DateTime.Now:HH:mm:ss}] " : "";
            var logLine = timestamp + output;
            
            _outputLines.Add(logLine);
            
            // 限制最大行数以避免内存问题
            if (_outputLines.Count > _maxLines)
            {
                _outputLines.RemoveAt(0);
            }
            
            // 自动滚动到底部
            if (_autoScroll)
            {
                _scrollPosition.y = float.MaxValue;
            }
            
            // 强制重绘窗口
            Repaint();
        }
        
        private void OnServerStatusChanged(bool isRunning)
        {
            var statusMessage = isRunning ? "服务器已启动" : "服务器已停止";
            OnServerOutput($"<color=cyan>[状态] {statusMessage}</color>");
        }
        
        private void OnGUI()
        {
            if (_logStyle == null)
            {
                InitializeStyles();
            }
            
            DrawToolbar();
            DrawLogArea();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // 服务器状态显示
            var isRunning = ServerManagerTool.IsServerRunning;
            var statusColor = isRunning ? "green" : "red";
            var statusText = isRunning ? "运行中" : "已停止";
            GUILayout.Label($"<color={statusColor}>● {statusText}</color>", new GUIStyle(EditorStyles.label) { richText = true });
            
            GUILayout.FlexibleSpace();
            
            // 过滤输入框
            GUILayout.Label("过滤:", GUILayout.Width(30));
            _filterText = GUILayout.TextField(_filterText, GUILayout.Width(150));
            
            GUILayout.Space(10);
            
            // 设置选项
            _showTimestamp = GUILayout.Toggle(_showTimestamp, "时间戳", GUILayout.Width(60));
            _autoScroll = GUILayout.Toggle(_autoScroll, "自动滚动", GUILayout.Width(70));
            _wordWrap = GUILayout.Toggle(_wordWrap, "自动换行", GUILayout.Width(70));
            
            GUILayout.Space(10);
            
            // 控制按钮
            if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                ClearLog();
            }
            
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                SaveLog();
            }
            
            // 最大行数设置
            GUILayout.Label("最大行数:", GUILayout.Width(60));
            _maxLines = EditorGUILayout.IntField(_maxLines, GUILayout.Width(60));
            _maxLines = Mathf.Clamp(_maxLines, 100, 10000);
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLogArea()
        {
            var logAreaRect = GUILayoutUtility.GetRect(0, position.height - 20, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            _scrollPosition = GUI.BeginScrollView(logAreaRect, _scrollPosition, 
                new Rect(0, 0, logAreaRect.width - 20, GetContentHeight()));
            
            var filteredLines = GetFilteredLines();
            var lineHeight = _logStyle.lineHeight + 2;
            
            for (int i = 0; i < filteredLines.Count; i++)
            {
                var line = filteredLines[i];
                var lineRect = new Rect(5, i * lineHeight, logAreaRect.width - 25, lineHeight);
                
                var style = GetLineStyle(line);
                style.wordWrap = _wordWrap;
                
                // 使用富文本显示日志
                GUI.Label(lineRect, line, style);
            }
            
            GUI.EndScrollView();
        }
        
        private List<string> GetFilteredLines()
        {
            if (string.IsNullOrEmpty(_filterText))
            {
                return _outputLines;
            }
            
            var filtered = new List<string>();
            foreach (var line in _outputLines)
            {
                if (line.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filtered.Add(line);
                }
            }
            return filtered;
        }
        
        private GUIStyle GetLineStyle(string line)
        {
            if (line.Contains("[Server Error]") || line.Contains("[Build Error]"))
            {
                return _errorStyle;
            }
            else if (line.Contains("Warning") || line.Contains("警告"))
            {
                return _warningStyle;
            }
            return _logStyle;
        }
        
        private float GetContentHeight()
        {
            var filteredLines = GetFilteredLines();
            return filteredLines.Count * (_logStyle.lineHeight + 2) + 10;
        }
        
        private void ClearLog()
        {
            _outputLines.Clear();
            _scrollPosition = Vector2.zero;
        }
        
        private void SaveLog()
        {
            var path = EditorUtility.SaveFilePanel("保存服务器日志", "", $"server_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    System.IO.File.WriteAllLines(path, _outputLines);
                    EditorUtility.DisplayDialog("成功", $"日志已保存到: {path}", "确定");
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("错误", $"保存日志失败: {e.Message}", "确定");
                }
            }
        }
        
        // 添加一些快捷键
        private void OnInspectorUpdate()
        {
            // 检查是否需要重绘（例如当有新的日志输出时）
            if (_autoScroll && _outputLines.Count > 0)
            {
                var maxScrollY = Mathf.Max(0, GetContentHeight() - position.height + 40);
                if (_scrollPosition.y < maxScrollY - 10) // 如果不在底部
                {
                    _scrollPosition.y = maxScrollY;
                    Repaint();
                }
            }
        }
    }
}