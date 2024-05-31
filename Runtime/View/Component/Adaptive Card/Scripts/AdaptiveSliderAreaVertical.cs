using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Twenty2.VomitLib.View.Component
{
    public class AdaptiveSliderAreaVertical : AdaptiveSliderArea
    {
        private Vector3 _screenCenterPos;

        // 间隔
        public int _spacing = 10;

        // 拖拽响应
        public float _duration = 0.2f;

        // 缩放
        public float _unscaleValue = 0.8f;

        protected override void InitLayout(ref Vector2 size)
        {
            var rectTrans = transform as RectTransform;
            size.x = rectTrans.rect.width;

            size.y = (rectTrans.rect.height - (ShowCount - 1) * _spacing) / ShowCount;

            _screenCenterPos = Camera.main.ScreenToWorldPoint(new Vector3(size.y / 2, 0, 0));
        }

        protected override void OnSelectCard(int i)
        {
            foreach (var element in Elements)
            {
                if (element.gameObject == CurrElement)
                {
                    element.localScale = Vector3.one;
                }
                else
                {
                    element.transform.localScale = new Vector3(_unscaleValue, _unscaleValue, _unscaleValue);
                }
            }

            Node.DOLocalMoveY(-i * (ElementSize.y + _spacing), _duration);
        }

        protected override void OnSetTrans(RectTransform child)
        {
            child.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, ElementSize.x);
            child.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, ElementSize.y);
            child.anchorMax = new Vector2(0.5f, 0.5f);
            child.anchorMin = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = new Vector2(0, 0);
        }

        protected override void SetChildPosition()
        {
            float step = ElementSize.y + _spacing;

            for (int i = 0; i < Elements.Count; i++)
            {
                float posY = _screenCenterPos.y + i * step;
                Elements[i].anchoredPosition = new Vector3(0, posY, 0);
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            // throw new System.NotImplementedException();
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            Vector2 slideDir = (Vector2)Input.mousePosition - Touch;
            float x = slideDir.x;
            float y = slideDir.y;

            if (Mathf.Abs(x) < Mathf.Abs(y) && Mathf.Abs(y) > _slidingDistance)
            {
                if (y > 0)
                {
                    Slide(SlideDir.Next);
                }
                else
                {
                    Slide(SlideDir.Prev);
                }
            }
        }
    }
}