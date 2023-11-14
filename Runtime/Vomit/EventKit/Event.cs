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
        
        public static UniTask WaitEvent<T>() where T : struct
        {
            var key = typeof(T);
            if (!s_waitEventToken.ContainsKey(key))
            {
                s_waitEventToken.Add(key, Vomit.Interface.RegisterEvent<T>(e =>
                {
                    s_waitEventToken[key].UnRegister();
                    s_waitEventToken.Remove(typeof(T));
                }));

            }

            return UniTask.WaitWhile(() => s_waitEventToken.ContainsKey(typeof(T)));
        }
    }
}