using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View
{
    /// <summary>
    /// 图片置灰
    /// </summary>
    public class UIGray : MonoBehaviour
    {
        [SerializeField] private Graphic _target;
        [SerializeField] private Material _grayMat;
        [SerializeField] private Material _defalutMat;
        
        private void Awake()
        {
            if (_target == null)
            {
                _target = GetComponent<Graphic>();
            }

            if (_defalutMat == null)
            {
                _defalutMat = _target?.material;
            }
            
            if (_grayMat == null)
            {
                Shader shader = Shader.Find("VomitLib/UI/UIGray");
                if (shader == null)
                {
                    Log.Error("VomitLib/UI/UIGray : null");
                }
                _grayMat = new Material(shader);
            }
        }
        

        /// <summary>
        /// 图片置灰
        /// </summary>
        /// <param name="img"></param>
        public void SetUIGray()
        {
            if (_target == null)
            {
                return;
            }

            _target.material = _grayMat;
            _target.SetMaterialDirty();
        }

        /// <summary>
        /// 图片回复
        /// </summary>
        /// <param name="img"></param>
        public void Recovery()
        {
            if (_target == null)
            {
                return;
            }
            _target.material = _defalutMat;
        }
    }
}
