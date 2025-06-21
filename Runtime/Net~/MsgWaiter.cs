using System;
using System.Collections.Generic;
using System.Threading;
using FluentAPI;
using Cysharp.Threading.Tasks;


namespace Twenty2.VomitLib.Net
{
    public class MsgWaiter
    {
        private UniTaskCompletionSource<bool> _tcs { get; set; }

        private CancellationTokenSource  _timeoutCancellation { get; set; }
        
        public UniTaskCompletionSource<bool> Start(int millisecondsDelay)
        {
            _tcs = new UniTaskCompletionSource<bool>();

            _timeoutCancellation = new CancellationTokenSource();
            UniTask.Create(async () =>
            {
                var isCancel = await UniTask.Delay(
                        millisecondsDelay, 
                        false, 
                        PlayerLoopTiming.Update, 
                        _timeoutCancellation.Token).SuppressCancellationThrow();
                if (isCancel)
                {
                    return;
                }
                Done(false);
                UnityEngine.Debug.LogError("等待消息超时");
            });

            return _tcs;
        }

        public void Done(bool result)
        {
            _timeoutCancellation?.CancelAndDispose();
            _timeoutCancellation = null;
            _tcs?.TrySetResult(result);
            _tcs = null;
        }

        public void Dispose()
        {
            _timeoutCancellation?.CancelAndDispose();
        }
    }
}
