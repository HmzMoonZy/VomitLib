using System;
using System.Collections.Generic;

namespace Twenty2.VomitLib.Tools
{
    /// <summary>
    /// 自增工具
    /// </summary>
    public static class IDKit
    {
        private static Dictionary<string, int> _increasingDict = new();
        

        public static int NextIncreasing<T>()
        {
            return NextIncreasing(typeof(T).Name);
        }
        
        public static void Register(string key, int startValue = 0)
        {
            _increasingDict[key] = startValue;
        }

        public static void Register<T>(int startValue = 0)
        {
            Register(typeof(T).Name, startValue);
        }

        public static int NextIncreasing(string key)
        {
            if (!_increasingDict.TryGetValue(key, out var result))
            {
                throw new KeyNotFoundException($"IDKit: key \"{key}\" 未注册，请先调用 Register");
            }

            _increasingDict[key] = result + 1;
            return result;
        }
        
        
    }
}