using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Twenty2.VomitLib.Tools;

namespace Twenty2.VomitLib.Net
{
    public class MsgWaiter
    {
        /// <summary>
        /// 全局等待消息字典
        /// </summary>
        private static readonly Dictionary<int, MsgWaiter> _waitDic = new();

        /// <summary>
        /// 清空等待消息字典
        /// </summary>
        public static void Clear()
        {
            foreach (var kv in _waitDic)
            {
                kv.Value.Complete(false);
            }
            _waitDic.Clear();
            _allTcs?.TrySetResult(false);
            _allTcs = null;
        }

        /// <summary>
        /// 是否所有消息都回来了
        /// </summary>
        /// <returns></returns>
        public static bool IsAllBack()
        {
            return _waitDic.Count <= 0;
        }

        private static UniTaskCompletionSource<bool> _allTcs;

        /// <summary>
        /// 等待所有消息回来
        /// </summary>
        /// <returns></returns>
        public static async UniTask<bool> WaitAllBack()
        {
            if (_waitDic.Count > 0)
            {
                if (_allTcs == null || _allTcs.GetStatus(0) != UniTaskStatus.Pending)
                {
                    _allTcs = new UniTaskCompletionSource<bool>();
                }
                await _allTcs.Task;
            }
            return true;
        }

        public static void DisposeAll()
        {
            if (_waitDic.Count > 0)
            {
                foreach (var item in _waitDic)
                {
                    item.Value.Cancel();
                }
            }
            _waitDic.Clear();
            _allTcs?.TrySetResult(false);
        }

        /// <summary>
        /// 开始等待指定消息
        /// </summary>
        /// <param name="uniId">消息唯一ID</param>
        /// <param name="timeoutMs">超时时间(毫秒)</param>
        /// <returns></returns>
        public static async UniTask<bool> StartWait(int uniId, int timeoutMs = 10000)
        {
            if (_waitDic.ContainsKey(uniId))
            {
                Log.Error($"发现重复消息id：{uniId}");
                return false;
            }

            var waiter = new MsgWaiter();
            _waitDic.Add(uniId, waiter);

            try
            {
                using var cts = new CancellationTokenSource(timeoutMs);
                var result = await waiter.Tcs.Task.AttachExternalCancellation(cts.Token);
                return result;
            }
            catch (OperationCanceledException)
            {
                Log.Error($"等待消息超时: {uniId}");
                return false;
            }
            finally
            {
                _waitDic.Remove(uniId);
            }
        }

        /// <summary>
        /// 结束等待指定消息
        /// </summary>
        /// <param name="uniId">消息唯一ID</param>
        /// <param name="result">是否成功</param>
        public static void EndWait(int uniId, bool result = true)
        {
            Log.Debug($"结束等待消息: {uniId}");
            if (!result)
            {
                Log.Error($"消息处理失败：{uniId}");
            }

            if (_waitDic.TryGetValue(uniId, out var waiter))
            {
                _waitDic.Remove(uniId);

                // 如果所有等待的消息都完成了，通知WaitAllBack
                if (_waitDic.Count == 0 && _allTcs != null)
                {
                    _allTcs?.TrySetResult(true);
                    _allTcs = null;
                }
            }
            else
            {
                if (uniId > 0)
                {
                    Log.Error($"没有等待中的消息, 但是收到了[{uniId}]的消息, 当前等待消息数量：{_waitDic.Count}");
                }
            }

            waiter?.Complete(result);
        }

        /// <summary>
        /// UniTask完成源
        /// </summary>
        public UniTaskCompletionSource<bool> Tcs { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MsgWaiter()
        {
            Tcs = new UniTaskCompletionSource<bool>();
        }

        /// <summary>
        /// 完成等待
        /// </summary>
        /// <param name="result">结果</param>
        public void Complete(bool result)
        {
            Tcs?.TrySetResult(result);
        }

        /// <summary>
        /// 取消等待
        /// </summary>
        public void Cancel()
        {
            Tcs?.TrySetCanceled();
        }

    }
}
