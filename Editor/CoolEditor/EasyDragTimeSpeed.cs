using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.CoolEditor
{
    [InitializeOnLoad]
    public static class CoolDragTimeSpeed
    {
        private const float MinSpeed = 0f;
        private const float MaxSpeed = 10f;
        private const float DefaultSpeed = 1f;
        private const float SliderWidth = 120f;
        private const float LabelWidth = 30f;
        private const float ResetButtonWidth = 18f;

        private static float _timeScale = DefaultSpeed;

        static CoolDragTimeSpeed()
        {
            ToolbarExtender.RightToolbarGUI.Insert(0, OnToolbarGUI);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                _timeScale = DefaultSpeed;
                Time.timeScale = DefaultSpeed;
            }
        }

        private static void OnToolbarGUI()
        {
            // 仅在播放模式下显示
            if (!EditorApplication.isPlaying)
                return;

            GUILayout.FlexibleSpace();

            // 速度标签
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = GetSpeedColor(_timeScale) }
            };
            GUILayout.Label($"x{_timeScale:F1}", labelStyle, GUILayout.Width(LabelWidth));

            // 拖动条
            EditorGUI.BeginChangeCheck();
            _timeScale = GUILayout.HorizontalSlider(_timeScale, MinSpeed, MaxSpeed, GUILayout.Width(SliderWidth));
            if (EditorGUI.EndChangeCheck())
            {
                // 对齐到0.1的精度
                _timeScale = Mathf.Round(_timeScale * 10f) / 10f;
                Time.timeScale = _timeScale;
            }

            // 重置按钮
            if (GUILayout.Button("↺", EditorStyles.miniButton, GUILayout.Width(ResetButtonWidth)))
            {
                _timeScale = DefaultSpeed;
                Time.timeScale = DefaultSpeed;
            }
        }

        private static Color GetSpeedColor(float speed)
        {
            if (speed < 0.1f) return Color.red;
            if (speed < 1f) return Color.yellow;
            if (speed > 1f) return new Color(0.3f, 0.8f, 1f);
            return Color.white;
        }
    }
}
