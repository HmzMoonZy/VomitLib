using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;

namespace Twenty2.VomitLib.View
{
    /// <summary>
    /// 左右边缘自适应的 ViewLogic.
    /// 通常通过监听 CallType 和 CloseType 来触发开启和关闭.
    /// 当呼出时, 会从远离鼠标指针的一侧呼出.
    /// </summary>
    /// <typeparam name="CallType">监听的呼出事件</typeparam>
    /// <typeparam name="CloseType">监听的关闭事件</typeparam>
    public abstract class LREdgeAdaptiveViewLogic<CallType, CloseType> : ViewLogic, IController where CallType : struct where CloseType : struct
    {
        public abstract IArchitecture GetArchitecture();
        
        private enum Dir
        {
            L2R,
        
            R2L,
        }
    
        [SerializeField]
        protected RectTransform _view;

        private bool _isDisplay;
    
        private Dir _dir;

        private float _targetAnchorPosX;

        protected abstract void Show(CallType e, float targetAnchorX);
        protected abstract void Hide(CloseType e,  float targetAnchorX);
        
        public override UniTask OnOpened()
        {
            this.RegisterEvent<CallType>(__OnCallType);
            this.RegisterEvent<CloseType>(__OnCloseType);
            return UniTask.CompletedTask;
        }

        public override UniTask OnClose()
        {
            this.UnRegisterEvent<CallType>(__OnCallType);
            this.UnRegisterEvent<CloseType>(__OnCloseType);
            return UniTask.CompletedTask;
        }
    
        // TODO 目前对 DOTween 强关联. 把所需参数抛给 Show() 和 Hide()
        
        private void __Show()
        {
            _isDisplay = true;
            _dir = Input.mousePosition.x <= Screen.width / 2f ? Dir.R2L : Dir.L2R;

            if (_dir == Dir.L2R)
            {
                _view.anchorMin = new(0, 0.5f);
                _view.anchorMax = new(0, 0.5f);
                _view.anchoredPosition = new(-_view.rect.width, 0);
                _targetAnchorPosX = 0;
                return;
            }
        
            if (_dir == Dir.R2L)
            {
                _view.anchorMin = new(1, 0.5f);
                _view.anchorMax = new(1, 0.5f);
                _view.anchoredPosition = new(0, 0);
                _targetAnchorPosX = -_view.rect.width;
                return;
            }
        }
        
        public void __Hide()
        {
            if (!_isDisplay) return;
            _isDisplay = false;

            _targetAnchorPosX = _dir == Dir.L2R ? -_view.rect.width : 0;
        }

        private void __OnCallType(CallType e)
        {
            __Show();
            Show(e, _targetAnchorPosX);
        }

        private void __OnCloseType(CloseType e)
        {
            __Hide();
            Hide(e, _targetAnchorPosX);
        }
    }
}