using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    public class VomitMonitorWindow : EditorWindow
    {
        [MenuItem("VomitLib/Monitor")]
        private static void Open()
        {
            GetWindow<VomitMonitorWindow>("Vomit Monitor");
        }

        // 反射缓存
        private object _procedureMgr;
        private PropertyInfo _currentStateId;
        private PropertyInfo _previousStateId;
        private PropertyInfo _isRunning;
        private PropertyInfo _isChanging;
        private PropertyInfo _isInitialized;
        private PropertyInfo _stateTime;
        private PropertyInfo _frameCount;
        private MethodInfo _getRegisteredStates;
        private bool _resolved;

        private Vector2 _scrollPos;

        private void OnEnable()
        {
            EditorApplication.update += OnUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
        }

        private void OnUpdate()
        {
        }

        private void OnGUI()
        {
            if (!ResolveProcedureMgr())
            {
                EditorGUILayout.HelpBox("未找到 ProcedureMgr，请先进入 Play 模式", MessageType.Info);
                return;
            }

            if (!(bool)_isInitialized.GetValue(_procedureMgr))
            {
                EditorGUILayout.HelpBox("ProcedureMgr 未初始化", MessageType.Info);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawCurrentState();
            EditorGUILayout.Space(5);
            DrawRegisteredStates();

            EditorGUILayout.EndScrollView();
        }

        #region Draw

        private void DrawCurrentState()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("当前流程", EditorStyles.boldLabel);

            var current = _currentStateId.GetValue(_procedureMgr);
            var previous = _previousStateId.GetValue(_procedureMgr);
            var running = (bool)_isRunning.GetValue(_procedureMgr);
            var changing = (bool)_isChanging.GetValue(_procedureMgr);
            var time = (float)_stateTime.GetValue(_procedureMgr);
            var frames = (long)_frameCount.GetValue(_procedureMgr);

            EditorGUILayout.LabelField("当前状态", current?.ToString() ?? "无");
            EditorGUILayout.LabelField("上一状态", previous?.ToString() ?? "无");
            EditorGUILayout.LabelField("运行时间", $"{time:F1}s");
            EditorGUILayout.LabelField("运行帧数", frames.ToString());
            EditorGUILayout.LabelField("状态", running ? "运行中" : "停止");

            if (changing)
                EditorGUILayout.LabelField("切换中", "是", EditorStyles.boldLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawRegisteredStates()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("已注册流程", EditorStyles.boldLabel);

            var states = _getRegisteredStates.Invoke(_procedureMgr, null) as System.Collections.IEnumerable;
            if (states != null)
            {
                foreach (var s in states)
                    EditorGUILayout.LabelField("• " + s);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Reflection

        private bool ResolveProcedureMgr()
        {
            if (_resolved && _procedureMgr != null) return true;
            if (!EditorApplication.isPlaying) return false;

            var gameType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .FirstOrDefault(t => t.FullName != null && t.FullName.EndsWith(".Game") && t.GetProperty("Procedure") != null);

            if (gameType == null) return false;

            var procProp = gameType.GetProperty("Procedure", BindingFlags.Public | BindingFlags.Static);
            if (procProp == null) return false;

            _procedureMgr = procProp.GetValue(null);
            if (_procedureMgr == null) return false;

            var mgrType = _procedureMgr.GetType();

            _currentStateId = mgrType.GetProperty("CurrentStateId");
            _previousStateId = mgrType.GetProperty("PreviousStateId");
            _isRunning = mgrType.GetProperty("IsRunning");
            _isChanging = mgrType.GetProperty("IsChanging");
            _isInitialized = mgrType.GetProperty("IsInitialized");
            _stateTime = mgrType.GetProperty("StateTime");
            _frameCount = mgrType.GetProperty("FrameCount");
            _getRegisteredStates = mgrType.GetMethod("GetRegisteredStates");

            _resolved = true;
            return true;
        }

        #endregion
    }
}
