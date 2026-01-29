using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// Procedure监控标签页 - 监控游戏流程状态
    /// </summary>
    public class ProcedureMonitorTab
    {
        #region Fields

        private Vector2 _scrollPosition;
        private List<ProcedureTransition> _transitionHistory = new List<ProcedureTransition>();

        #endregion

        #region Public Methods

        public void OnGUI(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            // 绘制头部
            DrawHeader(window, provider);

            EditorGUILayout.Space(5);

            // 绘制当前状态
            DrawCurrentState(window, provider);

            EditorGUILayout.Space(10);

            // 绘制状态转换历史
            DrawTransitionHistory(window);
        }

        #endregion

        #region Drawing Methods

        private void DrawHeader(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var procedureInfos = provider.ProcedureInfos;

            var rect = EditorGUILayout.GetControlRect(false, 50);
            EditorGUI.DrawRect(rect, new Color(0.9f, 0.6f, 0.2f, 0.2f));

            var headerRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 40);
            EditorGUI.LabelField(headerRect, "Procedure流程监控", window._headerStyle);

            if (procedureInfos.Count > 0)
            {
                var currentState = procedureInfos[0].StateName;
                var statsRect = new Rect(rect.x + 10, rect.y + 25, rect.width - 20, 20);
                EditorGUI.LabelField(statsRect, $"当前状态: {currentState}", window._successStyle);
            }
        }

        private void DrawCurrentState(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var procedureInfos = provider.ProcedureInfos;

            EditorGUILayout.BeginVertical(window._boxStyle);
            EditorGUILayout.LabelField("当前流程状态", EditorStyles.boldLabel);

            if (procedureInfos.Count > 0)
            {
                var currentProcedure = procedureInfos[0];

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"状态名称: {currentProcedure.StateName}", window._infoStyle);
                EditorGUILayout.LabelField($"是否活跃: {(currentProcedure.IsActive ? "是" : "否")}",
                    currentProcedure.IsActive ? window._successStyle : window._warningStyle);
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("未检测到Procedure状态", window._warningStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTransitionHistory(VomitMonitorWindow window)
        {
            EditorGUILayout.BeginVertical(window._boxStyle);
            EditorGUILayout.LabelField("状态转换历史", EditorStyles.boldLabel);

            if (_transitionHistory.Count == 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("暂无转换记录", window._infoStyle);
            }
            else
            {
                EditorGUILayout.Space(5);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));

                foreach (var transition in _transitionHistory.Take(50).Reverse())
                {
                    DrawTransitionItem(window, transition);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTransitionItem(VomitMonitorWindow window, ProcedureTransition transition)
        {
            var rect = EditorGUILayout.GetControlRect(false, 22);
            EditorGUI.DrawRect(rect, new Color(0.95f, 0.95f, 0.95f, 0.5f));

            var timeRect = new Rect(rect.x + 5, rect.y + 2, 70, 18);
            EditorGUI.LabelField(timeRect, transition.Timestamp.ToString("HH:mm:ss"), window._infoStyle);

            var transitionRect = new Rect(timeRect.xMax + 10, rect.y + 2, 200, 18);
            EditorGUI.LabelField(transitionRect, $"{transition.FromState} → {transition.ToState}", window._infoStyle);
        }

        #endregion
    }
}
