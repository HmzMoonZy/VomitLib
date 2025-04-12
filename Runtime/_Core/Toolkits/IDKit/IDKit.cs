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
        
        public static int NextIncreasing(string key)
        {
            if (_increasingDict.TryGetValue(key, out var result))
            {
                _increasingDict.Add(key, int.MinValue);
                _increasingDict[key]++;
            }

            result = _increasingDict[key];
            _increasingDict[key]++;
            return result;
        }
        
        
    }
}