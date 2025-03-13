using FluentAPI;
using UnityEngine;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View.Component
{
    public static class UIGrayExtension
    {
        public static void SetGray(this Graphic graphic, bool isGray)
        {
            var gray = graphic.GetOrAddComponent<UIGray>();

            if (isGray)
            {
                gray.SetUIGray();
            }
            else
            {
                gray.Recovery();
            }
        }
    }
}