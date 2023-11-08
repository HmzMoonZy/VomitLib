using System.Collections.Generic;
using Newtonsoft.Json;

namespace Twenty2.VomitLib.ClientDB
{
    /// <summary>
    /// 和 Luban cs-simple-json 生成代码风格一致的数据表
    /// </summary>
    public class CustomTable<T> where T : ICustomTableItem
    {
        private readonly List<T> _dataList;
        private readonly Dictionary<int, T> _dataMap;

        
        public CustomTable(string json)
        {
            _dataList = JsonConvert.DeserializeObject<List<T>>(json);
            _dataMap = new Dictionary<int, T>();

            foreach (var data in _dataList)
            {
                _dataMap.Add(data.GetID(), data);
            }
        }

        public T this[int id] => _dataMap[id];
    }
}