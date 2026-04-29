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
        
        /// <summary>
        /// 从主种子 + 用途标签派生子种子（确定性哈希）。
        /// 同一 masterSeed + purpose 永远返回相同值。
        /// </summary>
        public static int DeriveSeed(int masterSeed, string purpose)
        {
            unchecked
            {
                int hash = masterSeed;
                foreach (char c in purpose)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }

        /// <summary>
        /// 从主种子 + 用途标签派生子种子（确定性哈希）。
        /// 同一 masterSeed + purpose 永远返回相同值。
        /// </summary>
        /// <returns></returns>
        public static int DeriveSeed(string master, string purpose)
        {
            return DeriveSeed(TextToSeed(master), purpose);
        }

        /// <summary>
        /// 带计数器的种子派生，适用于同一用途多次调用的场景（如每回合洗牌）。
        /// </summary>
        public static int DeriveSeed(int masterSeed, string purpose, int counter)
        {
            return DeriveSeed(DeriveSeed(masterSeed, purpose), counter.ToString());
        }

        /// <summary>
        /// 带计数器的种子派生，适用于同一用途多次调用的场景（如每回合洗牌）。
        /// </summary>
        public static int DeriveSeed(string masterSeed, string purpose, int counter)
        {
            return DeriveSeed(TextToSeed(masterSeed), purpose, counter);
        }
        
        public static int TextToSeed(string text)                                                                                               
        {                                                                                                                                       
            unchecked                                                                                                                           
            {                                                                                                                                   
                int hash = 5381; // DJB2 经典初始值                                                                                             
                foreach (char c in text)                                                                                                        
                    hash = hash * 33 + c;                                                                                                       
                return hash;                                                                                                                    
            }                                                                                                                                   
        }  

        /// <summary>
        /// Fisher-Yates 洗牌，相同种子产出相同结果
        /// </summary>
        public static void Shuffle(int[] array, int seed)
        {
            var rng = new Random(seed);
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}