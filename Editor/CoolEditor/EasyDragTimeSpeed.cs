using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twenty2.VomitLib.Editor.CoolEditor
{
    /// <summary>
    /// 播放模式下的时间缩放滑块（Overlay 版）
    /// </summary>
    [EditorToolbarElement(id, typeof(SceneView))]
    class TimeScaleSliderElement : VisualElement
    {
        public const string id = "VomitLib/TimeScale";

        private const float MinSpeed = 0f;
        private const float MaxSpeed = 10f;
        private const float DefaultSpeed = 1f;

        private readonly Slider _slider;
        private readonly Label _label;

        public TimeScaleSliderElement()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            _label = new Label($"x{DefaultSpeed:F1}");
            _label.style.width = 30;
            _label.style.unityTextAlign = TextAnchor.MiddleRight;
            Add(_label);

            _slider = new Slider(MinSpeed, MaxSpeed);
            _slider.value = DefaultSpeed;
            _slider.style.width = 120;
            _slider.RegisterValueChangedCallback(OnSliderChanged);
            Add(_slider);

            var resetBtn = new Button(() =>
            {
                _slider.value = DefaultSpeed;
                Time.timeScale = DefaultSpeed;
                UpdateLabel(DefaultSpeed);
            }) { text = "↺" };
            resetBtn.style.width = 20;
            Add(resetBtn);

            // 仅播放模式可见
            style.display = EditorApplication.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnSliderChanged(ChangeEvent<float> evt)
        {
            var rounded = Mathf.Round(evt.newValue * 10f) / 10f;
            Time.timeScale = rounded;
            UpdateLabel(rounded);
        }

        private void UpdateLabel(float speed)
        {
            _label.text = $"x{speed:F1}";
            if (speed < 0.1f) _label.style.color = Color.red;
            else if (speed < 1f) _label.style.color = Color.yellow;
            else if (speed > 1f) _label.style.color = new Color(0.3f, 0.8f, 1f);
            else _label.style.color = Color.white;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                style.display = DisplayStyle.Flex;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                style.display = DisplayStyle.None;
                _slider.value = DefaultSpeed;
                Time.timeScale = DefaultSpeed;
                UpdateLabel(DefaultSpeed);
            }
        }
    }

    [Overlay(typeof(SceneView), "Time Scale")]
    public class TimeScaleOverlay : ToolbarOverlay
    {
        TimeScaleOverlay() : base(TimeScaleSliderElement.id) { }
    }
}
