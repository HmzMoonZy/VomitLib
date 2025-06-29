using UnityEngine;

namespace Twenty2.VomitLib.EngineKit
{
    /// <summary>
    /// 仅编辑器下生效的组件
    /// 挂载此组件的GameObject在发布版本中会立即销毁此组件
    /// 在编辑器的Scene视图中显示标签
    /// 适用于调试信息、开发工具等仅编辑器需要的功能
    /// </summary>
    public class EditorOnly : MonoBehaviour
    {
        [Header("Scene视图标签设置")]
        [SerializeField] private string _label = "Editor Only";
        [SerializeField] private Color _labelColor = Color.yellow;
        [SerializeField] private Vector3 _labelOffset = Vector3.up;
        
        [Header("底框设置")]
        [SerializeField] private bool _showBackground = true;
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.5f);
        [SerializeField] private Vector2 _backgroundPadding = new Vector2(8f, 4f);

#if UNITY_EDITOR
        
        private void OnDrawGizmos()
        {
            var labelPosition = transform.position + _labelOffset;
            var style = new GUIStyle();
            style.normal.textColor = _labelColor;
            style.alignment = TextAnchor.MiddleCenter;
            
            if (_showBackground)
            {
                // 计算文本大小
                var content = new GUIContent(_label);
                var textSize = style.CalcSize(content);
                
                // 计算背景矩形
                var backgroundSize = new Vector2(textSize.x + _backgroundPadding.x * 2, textSize.y + _backgroundPadding.y * 2);
                var worldPos = UnityEditor.HandleUtility.WorldToGUIPoint(labelPosition);
                var backgroundRect = new Rect(
                    worldPos.x - backgroundSize.x * 0.5f,
                    worldPos.y - backgroundSize.y * 0.5f,
                    backgroundSize.x,
                    backgroundSize.y
                );
                
                // 绘制背景
                UnityEditor.Handles.BeginGUI();
                var originalGUIColor = GUI.color;
                GUI.color = _backgroundColor;
                GUI.DrawTexture(backgroundRect, UnityEditor.EditorGUIUtility.whiteTexture);
                GUI.color = originalGUIColor;
                UnityEditor.Handles.EndGUI();
            }
            
            // 绘制文本标签
            var originalHandlesColor = UnityEditor.Handles.color;
            UnityEditor.Handles.color = _labelColor;
            UnityEditor.Handles.Label(labelPosition, _label, style);
            UnityEditor.Handles.color = originalHandlesColor;
        }
#endif
        
        private void Awake()
        {
#if !UNITY_EDITOR
            Destroy(this);
#endif
        }

        #region Runtime Interface

        /// <summary>
        /// 运行时修改标签文本（仅编辑器下生效）
        /// </summary>
        public void SetLabel(string newLabel)
        {
#if UNITY_EDITOR
            _label = newLabel;
#endif
        }

        /// <summary>
        /// 运行时修改标签颜色（仅编辑器下生效）
        /// </summary>
        public void SetLabelColor(Color newColor)
        {
#if UNITY_EDITOR
            _labelColor = newColor;
#endif
        }

        /// <summary>
        /// 运行时修改标签偏移位置（仅编辑器下生效）
        /// </summary>
        public void SetLabelOffset(Vector3 newOffset)
        {
#if UNITY_EDITOR
            _labelOffset = newOffset;
#endif
        }

        /// <summary>
        /// 运行时修改背景颜色（仅编辑器下生效）
        /// </summary>
        public void SetBackgroundColor(Color newColor)
        {
#if UNITY_EDITOR
            _backgroundColor = newColor;
#endif
        }

        /// <summary>
        /// 运行时切换背景显示（仅编辑器下生效）
        /// </summary>
        public void SetShowBackground(bool show)
        {
#if UNITY_EDITOR
            _showBackground = show;
#endif
        }

        /// <summary>
        /// 运行时修改背景内边距（仅编辑器下生效）
        /// </summary>
        public void SetBackgroundPadding(Vector2 newPadding)
        {
#if UNITY_EDITOR
            _backgroundPadding = newPadding;
#endif
        }

        /// <summary>
        /// 获取当前标签文本（仅编辑器下有效）
        /// </summary>
        public string GetLabel()
        {
#if UNITY_EDITOR
            return _label;
#else
            return string.Empty;
#endif
        }

        #endregion
    }
}