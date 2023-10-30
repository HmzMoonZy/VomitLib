using QFramework;
using UnityEngine;

namespace Twenty2.VomitLib.View
{
    public class ViewComponent : MonoController
    {
        public RectTransform RectTransform => transform.As<RectTransform>();

        // private ViewLogic _parentView;
        //
        // public void SetParentView(ViewLogic view)
        // {
        //     _parentView = view;
        // }
        //
        // public ViewLogic GetParentView()
        // {
        //     return _parentView;
        // }
    }
}