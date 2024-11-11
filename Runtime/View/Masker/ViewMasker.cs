using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View
{
    public class ViewMasker : IViewMasker
    {
        private Color _color;

        public ViewMasker(Color color)
        {
            _color = color;
        }


        public void Mask(ViewLogic view)
        {
            var mask = new GameObject("__AutoMask", typeof(Image));
            var img = mask.GetComponent<Image>(); 
            img.color = _color;

            var rectTrans = mask.GetComponent<RectTransform>();
            rectTrans.sizeDelta = new Vector2(50000, 50000);
            rectTrans.SetParent(view.transform);
            rectTrans.localPosition = Vector3.zero;
            rectTrans.localScale = Vector3.one;
            rectTrans.SetAsFirstSibling();
            
            if (view.Config.ClickMaskTriggerClose)
            {
                var button = mask.AddComponent<Button>();
                button.targetGraphic = img;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() =>
                {
                    View.CloseAsync(view, false).Forget();
                });
            }
        }

        public void Unmask(ViewLogic view)
        {
            Object.Destroy(view.transform.Find("__AutoMask").gameObject);
        }
    }
}