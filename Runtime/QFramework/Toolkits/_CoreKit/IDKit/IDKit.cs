using System.Collections.Generic;

namespace Twenty2.VomitLib.Tools
{
    /// <summary>
    /// 自增工具
    /// </summary>
    public static class IDKit
    {
        private static Dictionary<System.Type, int> _map = new();

        public static int NextID<T>()
        {
            var key = typeof(T);
            if (!_map.TryGetValue(key, out var result))
            {
                _map.Add(key, int.MinValue);
            }

            result = _map[key];
            _map[key]++;
            return result;
        }
    }
}