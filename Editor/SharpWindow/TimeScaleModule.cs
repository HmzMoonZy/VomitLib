using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.SharpWindow
{
    /// <summary>
    /// 时间缩放调试模块。直接读写 <see cref="Time.timeScale"/>。
    /// 编辑模式照常渲染，但调速仅在 Play 模式生效。
    /// </summary>
    internal class TimeScaleModule : ISharpModule
    {
        private const float MinSpeed = 0f;
        private const float MaxSpeed = 10f;
        private const float DefaultSpeed = 1f;

        // (value, label)。0 = Stop。
        private static readonly (float value, string label)[] Presets =
        {
            (0f,   "■ Stop"),
            (0.1f, "x0.1"),
            (0.5f, "x0.5"),
            (1f,   "x1"),
            (2f,   "x2"),
            (10f,  "x10"),
        };

        public string Name => "时间缩放";

        private float _sliderValue = DefaultSpeed;

        public void OnEnable()
        {
            _sliderValue = Mathf.Round(Time.timeScale * 10f) / 10f;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        public void OnGUI()
        {
            // 当前值 + 增量微调（重置由预设 x1 承担）
            using (new EditorGUILayout.HorizontalScope())
            {
                var color = GetSpeedColor(_sliderValue);
                var old = GUI.color;
                GUI.color = color;
                EditorGUILayout.LabelField($"当前 x{_sliderValue:F1}", EditorStyles.boldLabel);
                GUI.color = old;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("-1", GUILayout.Width(34)))
                {
                    ApplySpeed(_sliderValue - 1f);
                }
                if (GUILayout.Button("-0.1", GUILayout.Width(40)))
                {
                    ApplySpeed(_sliderValue - 0.1f);
                }
                if (GUILayout.Button("+0.1", GUILayout.Width(40)))
                {
                    ApplySpeed(_sliderValue + 0.1f);
                }
                if (GUILayout.Button("+1", GUILayout.Width(34)))
                {
                    ApplySpeed(_sliderValue + 1f);
                }
            }

            EditorGUILayout.Space(2);

            // 预设按钮
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var preset in Presets)
                {
                    if (GUILayout.Button(preset.label))
                    {
                        ApplySpeed(preset.value);
                    }
                }
            }

            EditorGUILayout.Space(2);

            // 自由滑块（GUI.changed 时才提交，避免空写）
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUILayout.Slider(_sliderValue, MinSpeed, MaxSpeed);
            if (EditorGUI.EndChangeCheck())
            {
                ApplySpeed(newValue);
            }

            // 编辑模式下提示
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox("时间缩放仅在 Play 模式生效。", MessageType.Info);
            }
        }

        private void ApplySpeed(float raw)
        {
            var rounded = Mathf.Round(Mathf.Clamp(raw, MinSpeed, MaxSpeed) * 10f) / 10f;
            _sliderValue = rounded;
            Time.timeScale = rounded;
        }

        private static Color GetSpeedColor(float speed)
        {
            if (speed < 0.1f)
            {
                return Color.red;
            }
            if (speed < 1f)
            {
                return Color.yellow;
            }
            if (speed > 1f)
            {
                return new Color(0.3f, 0.8f, 1f);
            }
            return Color.white;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                // 防止下次进入 Play 残留慢速
                _sliderValue = DefaultSpeed;
                Time.timeScale = DefaultSpeed;
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // 进入 Play 时按真实 timeScale 同步滑块
                _sliderValue = Mathf.Round(Time.timeScale * 10f) / 10f;
            }
        }
    }
}
