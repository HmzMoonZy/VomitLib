using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Twenty2.VomitLib
{
    public static class EventExtension
    {
        public static Dictionary<string, List<UniTask>> s_taskListMap = new();

        /// <summary>
        /// 返回当前事件的任务列表, 如果没有则新建一个.
        /// </summary>
        /// <returns></returns>
        public static List<UniTask> GetTaskList<T>(this T e) where T : struct
        {
            string key = typeof(T).Name;

            if (s_taskListMap.TryGetValue(key, out var list)) return list;
        
            list = new List<UniTask>();
        
            s_taskListMap.Add(key, list);
        
            return list;
        }
    
        /// <summary>
        /// 增加一个事件
        /// </summary>
        /// <param name="task"></param>
        public static void AddTask<T>(this T e, UniTask task) where T : struct
        {
            GetTaskList(e).Add(task);
        }

        /// <summary>
        /// 返回所有任务的 WhenAll 事件.
        /// 返回后清空事件.
        /// </summary>
        public static async UniTask WhenAll<T>(this T e) where T : struct
        {
            string key = typeof(T).Name;
        
            if (!s_taskListMap.ContainsKey(key)) return;
        
            await UniTask.WhenAll(GetTaskList(e));
        
            s_taskListMap.Remove(key);
        }
    
        public static UniTask SendAsync<T>(this T e) where T : struct
        {
            Vomit.Interface.SendEvent(e);
            return e.WhenAll();
        }
    }
}