using System.Collections.Generic;
using UnityEngine;

namespace Twenty2.VomitLib.View
{
    public class ViewLocker : IViewLocker
    {
        private HashSet<string> _lockSet = new();

        public void Lock(ViewLogic view)
        {
            if (_lockSet.Contains(view.ID))
            {
                return;
            }

            var locker = new GameObject("__AutoLock", typeof(NonDrawingGraphic));
            var graphic = locker.GetComponent<NonDrawingGraphic>();

            var rectTrans = graphic.GetComponent<RectTransform>();
            rectTrans.sizeDelta = new Vector2(50000, 50000);
            rectTrans.SetParent(view.transform);
            rectTrans.localPosition = Vector3.zero;
            rectTrans.localScale = Vector3.one;
            rectTrans.SetAsLastSibling();
            _lockSet.Add(view.ID);

#if UNITY_EDITOR
            {
                var mask = new GameObject("__AutoMask", typeof(UnityEngine.UI.Image));
                var img = mask.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(1, 0, 0, 0.3f);

                rectTrans = mask.GetComponent<RectTransform>();
                rectTrans.sizeDelta = new Vector2(50000, 50000);
                rectTrans.SetParent(view.transform);
                rectTrans.localPosition = Vector3.zero;
                rectTrans.localScale = Vector3.one;
                rectTrans.SetAsFirstSibling();
                rectTrans.SetParent(locker.transform);
            }
#endif
        }

        public void UnLock(ViewLogic view)
        {
            if (_lockSet.Contains(view.ID))
            {
                Object.Destroy(view.transform.Find("__AutoLock").gameObject);
                _lockSet.Remove(view.ID);
            }
        }
    }
}