/****************************************************************************
 * Copyright (c) 2016 - 2023 liangxiegame UNDER MIT License
 * 
 * http://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FluentAPI
{

    public static class SystemStringExtension
    {

        public static bool IsNullOrEmpty(this string selfStr)
        {
            return string.IsNullOrEmpty(selfStr);
        }



        public static bool IsNotNullAndEmpty(this string selfStr)
        {
            return !string.IsNullOrEmpty(selfStr);
        }


        public static bool IsTrimNullOrEmpty(this string selfStr)
        {
            return selfStr == null || string.IsNullOrEmpty(selfStr.Trim());
        }

        /// <summary>
        /// Check Whether string trim is null or empty
        /// </summary>
        /// <param name="selfStr"></param>
        /// <returns></returns>

        public static bool IsTrimNotNullAndEmpty(this string selfStr)
        {
            return selfStr != null && !string.IsNullOrEmpty(selfStr.Trim());
        }


        /// <summary>
        /// 缓存
        /// </summary>
        private static readonly char[] mCachedSplitCharArray = { '.' };


        public static string[] Split(this string selfStr, char splitSymbol)
        {
            mCachedSplitCharArray[0] = splitSymbol;
            return selfStr.Split(mCachedSplitCharArray);
        }


        public static string FillFormat(this string selfStr, params object[] args)
        {
            return string.Format(selfStr, args);
        }


        public static StringBuilder Builder(this string selfStr)
        {
            return new StringBuilder(selfStr);
        }


        public static StringBuilder AddPrefix(this StringBuilder self, string prefixString)
        {
            self.Insert(0, prefixString);
            return self;
        }



        public static int ToInt(this string selfStr, int defaulValue = 0)
        {
            var retValue = defaulValue;
            return int.TryParse(selfStr, out retValue) ? retValue : defaulValue;
        }


        public static DateTime ToDateTime(this string selfStr, DateTime defaultValue = default(DateTime))
        {
            return DateTime.TryParse(selfStr, out var retValue) ? retValue : defaultValue;
        }



        public static float ToFloat(this string selfStr, float defaultValue = 0)
        {
            return float.TryParse(selfStr, out var retValue) ? retValue : defaultValue;
        }


        public static bool HasChinese(this string input)
        {
            return Regex.IsMatch(input, @"[\u4e00-\u9fa5]");
        }


        public static bool HasSpace(this string input)
        {
            return input.Contains(" ");
        }


        public static string RemoveString(this string str, params string[] targets)
        {
            return targets.Aggregate(str, (current, t) => current.Replace(t, string.Empty));
        }
        

        public static string StringJoin(this IEnumerable<string> self, string separator)
        {
                return string.Join(separator, self);
        }
    }
}