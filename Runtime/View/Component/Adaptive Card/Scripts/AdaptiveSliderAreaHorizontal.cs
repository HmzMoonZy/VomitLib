using System.Collections.Generic;
using Cysharp.Threading.Tasks.Triggers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Twenty2.VomitLib.View.Component
{
    public class AdaptiveSliderAreaHorizontal : AdaptiveSliderArea
    {
        private Vector3 _screenCenterPos;

        // 拖拽响应
        public float _duration = 0.2f;

        // 间隔
        public int _spacing = 10;

        public float _unscaleValue = 0.8f;

        protected override void InitLayout(ref Vector2 size)
        {
            var rectTrans = transform as RectTransform;
            size.y = rectTrans.rect.height;
            size.x = (rectTrans.rect.width - (ShowCount - 1) * _spacing) / ShowCount;

            _screenCenterPos = Camera.main.ScreenToWorldPoint(new Vector3(size.x / 2, 0, 0));
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

            Node.DOLocalMoveX(-i * (ElementSize.x + _spacing), _duration);
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
            float step = ElementSize.x + _spacing;

            for (int i = 0; i < Elements.Count; i++)
            {
                //trans[i].SetPosY(childHeight / 2);
                float posX = _screenCenterPos.x + i * step;
                Elements[i].anchoredPosition = new Vector3(posX, 0, 0);
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

            if (Mathf.Abs(x) > Mathf.Abs(y) && Mathf.Abs(x) > _slidingDistance)
            {
                if (x > 0)
                {
                    Slide(SlideDir.Next);
                }
                else
                {
                    Slide(SlideDir.Prev);
                }
            }
        }

        /// <summary>
        /// 设置滑灵敏度
        /// </summary>
        /// <param name="duration"></param>
        public void SetSlideDuration(float duration)
        {
            this._duration = duration;
        }

        /// <summary>
        /// 设置元素间隔, 需要ForceRefresh
        /// </summary>
        /// <param name="spacing"></param>
        public void SetSpacing(int spacing)
        {
            this._spacing = spacing;
        }

        /// <summary>
        /// 设置未选择元素的缩放值, 通常小于1,需要ForceRefresh
        /// </summary>
        public void SetScaleValue(float value)
        {
            this._unscaleValue = value;
        }
    }
}