using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using QFramework;


namespace Twenty2.VomitLib.Net
{
    public class MsgWaiter
    {
        #region Static

        private static readonly Dictionary<int, MsgWaiter> WaiterMap = new Dictionary<int, MsgWaiter>();
        
        public static async UniTask<bool> StartWait(int uniId, int timeout = 10000)
        {
            if (WaiterMap.ContainsKey(uniId))
            {
                LogKit.E("发现重复消息id：" + uniId);
                return true;
            }

            var waiter = new MsgWaiter();
            WaiterMap.Add(uniId, waiter);
            return await waiter.Start(timeout).Task;
        }

        public static void EndWait(int uniId, bool result)
        {
            LogKit.I("结束等待消息:" + uniId);
            if (!result)
            {
                LogKit.E("await失败：" + uniId);
            }
            
            if (!WaiterMap.ContainsKey(uniId))
            {
                if (uniId > 0)
                {
                    LogKit.E("找不到EndWait：" + uniId + ">size：" + WaiterMap.Count);
                }

                return;
            }

            var waiter = WaiterMap[uniId];
            waiter.Done(result);
            WaiterMap.Remove(uniId);
            if (WaiterMap.Count <= 0) //所有等待的消息都回来了再解屏
            {
                if (s_allTcs != null)
                {
                    s_allTcs.TrySetResult(true);
                    s_allTcs = null;
                }
            }
        }

        public static void Clear()
        {
            foreach (var kv in WaiterMap)
            { 
                kv.Value.Done(false);
            }
            WaiterMap.Clear();
        }
        
        public static void DisposeAll()
        {
            if (WaiterMap.Count <= 0) return;
            
            foreach (var item in WaiterMap)
            {
                item.Value.Timer?.Dispose();
            }
        }

        /// <summary>
        /// 是否所有消息都回来了
        /// </summary>
        /// <returns></returns>
        public static bool IsAllBack()
        {
            return WaiterMap.Count <= 0;
        }

        private static TaskCompletionSource<bool> s_allTcs;
        /// <summary>
        /// 等待所有消息回来
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> WaitAllBack()
        {
            if (WaiterMap.Count > 0)
            {
                if (s_allTcs == null || s_allTcs.Task.IsCompleted)
                {
                    s_allTcs = new TaskCompletionSource<bool>();
                }
                await s_allTcs.Task;
            }
            return true;
        }

        #endregion
        
        private UniTaskCompletionSource<bool> Tcs { get; set; }

        private Timer Timer { get; set; }
        
        private UniTaskCompletionSource<bool> Start(int timeout)
        {
            Tcs = new UniTaskCompletionSource<bool>();
            Timer = new Timer(_ =>
            {
                Done(false);
                UnityEngine.Debug.LogError("等待消息超时");
            }, null, timeout, -1);

            return Tcs;
        }

        private void Done(bool result)
        {
            Timer.Dispose();
            Tcs?.TrySetResult(result);
            Tcs = null;
        }
    }
}
