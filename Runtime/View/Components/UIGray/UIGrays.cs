using Twenty2.VomitLib;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View
{
    [DisallowMultipleComponent]
    public class UIGrays : MonoBehaviour
    {
        private static Material s_grayMat;
        
        [SerializeField] private Graphic[] _targets;
        
        public bool m_isGray = false;

        private void Awake()
        {
            if (_targets == null || _targets.Length <= 0)
            {
                _targets = gameObject.GetComponentsInChildren<Graphic>();
            }
        }
        
        /// <summary>
        /// 图片置灰
        /// </summary>
        [ContextMenu("置灰")]
        public void SetUIGray()
        {
            m_isGray = true;
            if (_targets == null) return;
            for (int i = 0; i < _targets.Length; i++)
            {
                var target = _targets[i];
                
                if (target == null)
                {
                    //HLog.LogException($"UIGrays.m_targets index:{i} is null");
                    continue;
                }

                if (target) {
                    target.material = GetGrayMat();
                    target.SetMaterialDirty();
                }
            }
        }

        /// <summary>
        /// 图片回复
        /// </summary>
        [ContextMenu("恢复")]
        public void Recovery()
        {
            m_isGray = false;
            if (_targets == null) return;
            for (int i = 0; i < _targets.Length; i++)
            {
                var target = _targets[i];
                
                if (target == null)
                {
                    Log.Error($"UIGrays.m_targets index:{i} is null");
                    continue;
                }
                
                target.material = null;
                target.SetMaterialDirty();
            }
        }

        [ContextMenu("收集Graphic")]
        public void FindGraphic()
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "UIGrays.FindGraphic");
#endif
            _targets = gameObject.GetComponentsInChildren<Graphic>();
        }
        
        private Material GetGrayMat()
        {
            if (s_grayMat == null)
            {
                var shader = Shader.Find("VomitLib/UI/UIGray");
                if (shader == null)
                {
                    Log.Error("UIGrays.GetGrayMat() shader is null. Shader name : VomitLib/UI/UIGray");
                    return null;
                }
                
                s_grayMat = new Material(shader);
            }

            return s_grayMat;
        }
    }
}