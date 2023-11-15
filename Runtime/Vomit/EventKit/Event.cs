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
        public static UniTask WaitEvent<T>() where T : struct
        {
            var key = typeof(T);
            if (!s_waitEventToken.ContainsKey(key))
            {
                s_waitEventToken.Add(key, Vomit.Interface.RegisterEvent<T>(e =>
                {
                    BreakWaitEvent<T>();
                }));
            }

            return UniTask.WaitWhile(() => s_waitEventToken.ContainsKey(typeof(T)));
        }

        public static void BreakAll()
        {
            foreach (var (key, unRegister) in s_waitEventToken)
            {
                s_waitEventToken[key].UnRegister();
                s_waitEventToken.Remove(key);
            }
        }

        public static void BreakWaitEvent(Type type)
        {
            try
            {
                s_waitEventToken[type].UnRegister();
                s_waitEventToken.Remove(type);
            }
            catch (Exception e)
            {
                LogKit.E($"BreakWaitEvent Error : {type.Name}");
            }

        }
        
        public static void BreakWaitEvent<T>()
        {
            try
            {
                var key = typeof(T);
                s_waitEventToken[key].UnRegister();
                s_waitEventToken.Remove(typeof(T));
            }
            catch (Exception e)
            {
                LogKit.E($"BreakWaitEvent Error : {typeof(T).Name}");
            }

        }
    }
}