/****************************************************************************
 * Copyright (c) 2015 - 2022 liangxiegame UNDER MIT License
 * 
 * http://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;

namespace FluentAPI
{

    public static class CollectionsExtension
    {

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> self, Action<T> action)
        {
            foreach (var item in self)
            {
                action(item);
            }

            return self;
        }


        public static List<T> ForEachReverse<T>(this List<T> selfList, Action<T> action)
        {
            for (var i = selfList.Count - 1; i >= 0; --i)
                action(selfList[i]);

            return selfList;
        }
        
        

        public static void ForEach<T>(this List<T> list, Action<int, T> action)
        {
            for (var i = 0; i < list.Count; i++)
            {
                action(i, list[i]);
            }
        }
        
        

        public static void ForEach<K, V>(this Dictionary<K, V> dict, Action<K, V> action)
        {
            var dictE = dict.GetEnumerator();

            while (dictE.MoveNext())
            {
                var current = dictE.Current;
                action(current.Key, current.Value);
            }

            dictE.Dispose();
        }
        
        

        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this Dictionary<TKey, TValue> dictionary,
            params Dictionary<TKey, TValue>[] dictionaries)
        {
            return dictionaries.Aggregate(dictionary,
                (current, dict) => current.Union(dict).ToDictionary(kv => kv.Key, kv => kv.Value));
        }


        public static void AddRange<K, V>(this Dictionary<K, V> dict, Dictionary<K, V> addInDict,
            bool isOverride = false)
        {
            var enumerator = addInDict.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (dict.ContainsKey(current.Key))
                {
                    if (isOverride)
                        dict[current.Key] = current.Value;
                    continue;
                }

                dict.Add(current.Key, current.Value);
            }

            enumerator.Dispose();
        }
        
        
        // TODO:
        public static bool IsNullOrEmpty<T>(this T[] collection) => collection == null || collection.Length == 0;
        // TODO:
        public static bool IsNullOrEmpty<T>(this IList<T> collection) => collection == null || collection.Count == 0;
        // TODO:
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection) => collection == null || !collection.Any();
        // TODO:
        public static bool IsNotNullAndEmpty<T>(this T[] collection) => !IsNullOrEmpty(collection);
        // TODO:
        public static bool IsNotNullAndEmpty<T>(this IList<T> collection) => !IsNullOrEmpty(collection);
        // TODO:
        public static bool IsNotNullAndEmpty<T>(this IEnumerable<T> collection) => !IsNullOrEmpty(collection);

        #region Random

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

        #endregion
    }
}