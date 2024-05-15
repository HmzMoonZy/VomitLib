using QFramework;
using UnityEngine;
using Twenty2.VomitLib;

namespace Twenty2.VomitLib.View
{
    public class ViewComponent : MonoController
    {
        public RectTransform RectTransform => (RectTransform) transform;

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