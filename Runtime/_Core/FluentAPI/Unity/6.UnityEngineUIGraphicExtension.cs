/****************************************************************************
 * Copyright (c) 2015 - 2022 liangxiegame UNDER MIT License
 * 
 * http://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using UnityEngine.UI;

namespace FluentAPI
{

    public static class UnityEngineUIGraphicExtension
    {

        public static T ColorAlpha<T>(this T selfGraphic, float alpha) where T : Graphic
        {
            var color = selfGraphic.color;
            color.a = alpha;
            selfGraphic.color = color;
            return selfGraphic;
        }


        public static Image FillAmount(this Image selfImage, float fillAmount)
        {
            selfImage.fillAmount = fillAmount;
            return selfImage;
        }
    }
}