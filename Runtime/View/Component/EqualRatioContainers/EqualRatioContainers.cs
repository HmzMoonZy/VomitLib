using System;
using QFramework;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View.Component
{
    /// <summary>
    /// 限定 UI 在一个矩形范围内保持原始的长宽比
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class EqualRatioContainers : MonoBehaviour
    {
        [SerializeField] private bool _canUpdate;
        [SerializeField] private bool _useNativeSize;
        
        [SerializeField] private Vector2 _size;
        
        private RectTransform _self;
        
        private void Start()
        {
            Refresh();
        }

        public void Refresh(Vector2 size, bool useNativeSize)
        {
            _size = size;
            _useNativeSize = useNativeSize;
            Refresh();
        }
        
        public void Refresh()
        {
             _self = transform.As<RectTransform>();
            var g = _self.GetComponent<Graphic>();
            if (_useNativeSize && g is not null)
            {
                g.SetNativeSize();
            }

            var r = Mathf.Min(_size.x / _self.sizeDelta.x, _size.y / _self.sizeDelta.y);

            _self.sizeDelta *= r;
        }
        
        private void Update()
        {
            if (_canUpdate)
            {
                Refresh(_size, _useNativeSize);
            }
        }
    }
}

