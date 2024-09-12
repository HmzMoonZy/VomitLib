using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib.Net
{
    public static class MsgWaiterMgr
    {
        private static readonly Dictionary<int, MsgWaiter> WaiterMap = new Dictionary<int, MsgWaiter>();

        public static async UniTask<bool> StartWait(int uniId, int timeout = 10000)
        {
            if (WaiterMap.ContainsKey(uniId))
            {
                LogKit.E("MsgWaiterMgr : 发现重复消息id：" + uniId);
                return true;
            }

            var waiter = new MsgWaiter();
            WaiterMap.Add(uniId, waiter);
            return await waiter.Start(timeout).Task;
        }

        public static void EndWait(int uniId, bool result)
        {
            LogKit.I($"{uniId}返回结果!");
            
            if (!WaiterMap.TryGetValue(uniId, out var waiter))
            {
                LogKit.E($"找不到EndWait! uniId {uniId}");
                return;
            }
            
            if (!result)
            {
                LogKit.E("await 失败：" + uniId);
            }
            
            waiter.Done(result);
            WaiterMap.Remove(uniId);
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
                item.Value.Dispose();
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
    }
}