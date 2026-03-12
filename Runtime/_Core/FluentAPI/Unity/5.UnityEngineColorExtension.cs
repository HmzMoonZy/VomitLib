/****************************************************************************
 * Copyright (c) 2015 - 2022 liangxiegame UNDER MIT License
 * 
 * http://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using UnityEngine;

namespace FluentAPI
{

    public static class UnityEngineColorExtension
    {

        public static Color HtmlStringToColor(this string htmlString)
        {
            var parseSucceed = ColorUtility.TryParseHtmlString(htmlString, out var retColor);
            return parseSucceed ? retColor : Color.black;
        }
    }
}