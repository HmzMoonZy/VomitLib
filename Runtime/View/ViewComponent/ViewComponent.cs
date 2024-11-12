using System;
using QFramework;
using UnityEngine;
using Twenty2.VomitLib;

namespace Twenty2.VomitLib.View
{
    public class ViewComponent : MonoController
    {
        public RectTransform RectTransform => (RectTransform) transform;

    }
}