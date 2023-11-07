using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib
{
    // TODO List池.
    // TODO 异步事件标签缓存
    public static class EventExtension
    {
        public static Dictionary<string, List<UniTask>> s_taskListMap = new();
        
        public static Dictionary<Type, List<UniTask>> s_asyncEventCache = new();

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
            var taskList = GetTaskList(e);
            taskList.Add(task);
            
            LogKit.I($"{e.GetType().Name} 添加任务!! 当前任务数量 : {taskList.Count}");
        }

        /// <summary>
        /// 返回所有任务的 WhenAll 事件.
        /// 返回后清空事件.
        /// </summary>
        public static UniTask WhenAll<T>(this T e) where T : struct
        {
            string key = typeof(T).Name;
        
            if (!s_taskListMap.ContainsKey(key))
            {
                LogKit.I($"{key} 没有异步任务!");
                return UniTask.CompletedTask;
            }

            return UniTask.Create(async () =>
            {
                await UniTask.WhenAll(GetTaskList(e));

                s_taskListMap.Remove(key);
            });
        }
        
        public static UniTask SendAsyncEvent<T>(this ICanSendEvent self, T e) where T : struct
        {
            self.SendEvent(e);
            return e.WhenAll();
        }
        
        public static UniTask SendAsyncEvent<T>(this ICanSendEvent self) where T : struct
        {
            return self.SendAsyncEvent(new T());
        }
    }
}