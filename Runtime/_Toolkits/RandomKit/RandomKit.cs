using System;
using System.Collections.Generic;
using System.Linq;

namespace Twenty2.VomitLib.Tools
{
    public static class RandomTool
    {
        public static T Random<T, TValue>(this IDictionary<T, TValue> dic)
        {
            var keys = dic.Keys.ToList();
            var random = UnityEngine.Random.Range(0, keys.Count);
            return keys[random];
        }

        /// <summary>
        /// 给定一个值为权重的字典.
        /// 根据所有权重返回对应的KEY
        /// </summary>
        public static T RandomWeight<T>(this Dictionary<T, int> weightMap)
        {
            if (weightMap.Count == 1) return weightMap.Keys.First();

            int total = weightMap.Sum(w => w.Value);
            int landing = UnityEngine.Random.Range(1, total + 1);

            int step = 0;

            foreach (var weight in weightMap)
            {
                if (landing <= weight.Value + step)
                {
                    return weight.Key;
                }

                step += weight.Value;
            }

            throw new Exception("概率计算错误");
        }


        public static T Random<T>(this List<T> list, bool isTakeOut = false)
        {
            if (list.Count <= 0) throw new ArgumentOutOfRangeException();

            if (list.Count == 1) return list[0];

            int randomIndex = UnityEngine.Random.Range(0, list.Count);
            T result = list[randomIndex];

            if (isTakeOut)
            {
                list.Remove(result);
            }

            return result;
        }

        public static T Random<T>(this T[] arr)
        {
            if (arr.Length <= 0) throw new ArgumentOutOfRangeException();

            if (arr.Length == 1) return arr[0];

            int randomIndex = UnityEngine.Random.Range(0, arr.Length);
            T result = arr[randomIndex];

            return result;
        }

        public static T Random<T>(this HashSet<T> hashSet)
        {
            if (hashSet.Count <= 0) throw new ArgumentOutOfRangeException();

            if (hashSet.Count == 1) return hashSet.First();

            return hashSet.ElementAt(UnityEngine.Random.Range(0, hashSet.Count));
        }

        public static T Random<T>(this IEnumerable<T> enumerable)
        {
            var array = enumerable as T[] ?? enumerable.ToArray();
            return array.Random();
        }

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