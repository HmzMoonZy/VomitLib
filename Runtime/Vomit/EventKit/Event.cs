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
                LogKit.W($"BreakWaitEvent Error : {typeof(T).Name}");
            }

        }
    }
}