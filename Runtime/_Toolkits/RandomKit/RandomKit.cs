using System;
using System.Collections.Generic;
using System.Linq;

namespace Twenty2.VomitLib.Tools
{
    public static class RandomTool
    {
        /// <summary>
        /// 输入返回true的概率 0~1,
        /// </summary>
        public static bool RandomWithProb(float prob)
        {
            if (prob <= 0) return false;

            if (prob >= 1) return true;

            return UnityEngine.Random.Range(0f, 1f) <= prob;
        }
    }
}