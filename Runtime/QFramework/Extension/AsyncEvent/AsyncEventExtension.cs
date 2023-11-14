using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Cysharp.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib
{
    // TODO List池.
    public static class AsyncEventExtension
    {
        private static Dictionary<string, List<UniTask>> s_taskListMap = new();
        
        private static Dictionary<string, Action> s_taskDone = new();

        public static void Register(string key)
        {
            s_taskListMap.Add(key, null);
            s_taskDone.Add(key, null);
            LogKit.I($"注册异步事件{key}");
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
        
        /// <summary>
        /// 增加一个事件
        /// </summary>
        public static void AddTask<T>(this T e, UniTask task) where T : struct
        {
            var taskList = GetTaskList(e);
            taskList.Add(task);
            
            LogKit.I($"{e.GetType().Name} 添加任务!! 当前任务数量 : {taskList.Count}");
        }

        public static void Done<T>(this T e, Action onDone)
        {
            string key = typeof(T).Name;
            s_taskDone[key] += onDone;
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

                s_taskDone[key]?.Invoke();
                s_taskDone[key] = null;
                s_taskListMap[key] = null;
            });
        }

        /// <summary>
        /// 返回当前事件的任务列表, 如果没有则新建一个.
        /// </summary>
        /// <returns></returns>
        private static List<UniTask> GetTaskList<T>(this T e) where T : struct
        {
            string key = typeof(T).Name;

            if (!s_taskListMap.TryGetValue(key, out var list))
            {
                throw new NotSupportedException($"{key}不是[AsyncEvent]");
            }

            list ??= new List<UniTask>();

            s_taskListMap[key] = list;
        
            return list;
        }
        
    }
}