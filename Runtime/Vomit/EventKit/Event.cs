using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;
using Twenty2.VomitLib.Tools;

namespace Twenty2.VomitLib
{
    public static class Event
    {
        private static Dictionary<Type, IUnRegister> s_waitEventToken = new();
        
        // TODO : UniTask.WhenAny 支持
        /// <summary>
        /// 等待下一次的事件T触发.
        /// </summary>
        /// <code>
        /// 相当于只监听一次事件,屏蔽掉了在回调中取消监听自己的麻烦写法.
        /// </code>
        public static async UniTask<T> WaitEvent<T>() where T : struct
        {
            var key = typeof(T);

            T result = default;
            
            if (!s_waitEventToken.ContainsKey(key))
            {
                s_waitEventToken.Add(key, Vomit.Interface.RegisterEvent<T>(e =>
                {
                    result = e;
                    BreakWaitEvent<T>();
                }));
            }

            await UniTask.WaitWhile(() => s_waitEventToken.ContainsKey(typeof(T)));
            
            return result;
        }
        
        /// <summary>
        /// 取消 WaitEvent.
        /// 当你使用 WhenAny 之类的方式同时监听多个事件后, 你可能需要手动移除多余的监听.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void BreakWaitEvent<T>()
        {
            try
            {
                var keyType = typeof(T);
                s_waitEventToken[keyType].UnRegister();
                s_waitEventToken.Remove(keyType);
            }
            catch (Exception e)
            {
                LogKit.W($"BreakWaitEvent Error : {typeof(T).Name} {e.Message}");
            }
        }
    }
}