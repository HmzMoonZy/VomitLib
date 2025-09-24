using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib
{
    public static class EventWait
    {
        private static Dictionary<Type, IUnRegister> s_waitEventToken = new();
        
        /// <summary>
        /// 等待下一次的事件T触发.
        /// </summary>
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
        /// 等待下一次的事件T1或T2触发.
        /// </summary>
        public static async UniTask<(int winArgumentIndex, T1 result1, T2 result2)> WaitEvent<T1, T2>() where T1 : struct where T2 : struct
        {

           (int winArgumentIndex, T1 result1, T2 result2) result = await UniTask.WhenAny(WaitEvent<T1>(), WaitEvent<T2>());

           if (result.winArgumentIndex == 0) BreakWaitEvent<T2>();
           if (result.winArgumentIndex == 1) BreakWaitEvent<T1>();
           
            return result;
        }
        
        /// <summary>
        /// 等待下一次的事件T1或T2或T3触发.
        /// </summary>
        public static async UniTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3)> WaitEvent<T1, T2, T3>() where T1 : struct where T2 : struct where T3 : struct
        {

            (int winArgumentIndex, T1 result1, T2 result2, T3 result3) result = await UniTask.WhenAny(WaitEvent<T1>(), WaitEvent<T2>(), WaitEvent<T3>());

            switch (result.winArgumentIndex)
            {
                case 0:
                    BreakWaitEvent<T2>();
                    BreakWaitEvent<T3>();
                    break;
                case 1:
                    BreakWaitEvent<T1>();
                    BreakWaitEvent<T3>();
                    break;
                case 2:
                    BreakWaitEvent<T1>();
                    BreakWaitEvent<T2>();
                    break;
            }

            return result;
        }
        
        /// <summary>
        /// 取消 WaitEvent.
        /// 当你使用 WhenAny 之类的方式同时监听多个事件后, 你可能需要手动移除多余的监听.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private static void BreakWaitEvent<T>()
        {
            try
            {
                var keyType = typeof(T);
                s_waitEventToken[keyType].UnRegister();
                s_waitEventToken.Remove(keyType);
            }
            catch (Exception e)
            {
                Log.Warning($"BreakWaitEvent Error : {typeof(T).Name} {e.Message}");
            }
        }
    }
}