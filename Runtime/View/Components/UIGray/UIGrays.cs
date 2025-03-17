using Twenty2.VomitLib;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View.Component
{
    [DisallowMultipleComponent]
    public class UIGrays : MonoBehaviour
    {
        [SerializeField] private Graphic[] m_targets;

        [SerializeField] private Material m_grayMat;

        public bool m_isGray = false;

        /// <summary>
        /// 创建置灰材质球
        /// </summary>
        /// <returns></returns>
        private Material GetGrayMat()
        {
            if (m_grayMat == null)
            {
                Shader shader = Shader.Find("VomitLib/UI/UIGray");
                if (shader == null)
                {
                    Log.Debug("null");
                    return null;
                }

                Material mat = new Material(shader);
                m_grayMat = mat;
            }

            return m_grayMat;
        }

        /// <summary>
        /// 图片置灰
        /// </summary>
        [ContextMenu("置灰")]
        public void SetUIGray()
        {
            m_isGray = true;
            if (m_targets == null) return;
            for (int i = 0; i < m_targets.Length; i++)
            {
                var target = m_targets[i];
                
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
            if (m_targets == null) return;
            for (int i = 0; i < m_targets.Length; i++)
            {
                var target = m_targets[i];
                
                if (target == null)
                {
                    Log.Error($"UIGrays.m_targets index:{i} is null");
                    continue;
                }
                
                target.material = null;
                target.SetMaterialDirty();
            }
        }

        [ContextMenu("Find Graphic")]
        public void FindGraphic()
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "UIGrays.FindGraphic");
#endif
            m_targets = gameObject.GetComponentsInChildren<Graphic>();
        }
    }
}