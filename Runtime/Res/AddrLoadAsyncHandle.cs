using System;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Twenty2.VomitLib.Addr
{
    public struct AddrLoadAsyncHandle
    {
        private AsyncOperationHandle _handle;
        
        public object Result => _handle.Result;
        
        public AddrLoadAsyncHandle(AsyncOperationHandle handle)
        {
            _handle = handle;
        }

        public void OnComplete<T>(Action<T> onComplete)
        {
            _handle.Completed += (handle => onComplete.Invoke((T)handle.Result));
        }

        public UniTask ToUniTask()
        {
            return _handle.ToUniTask();
        }
        
    }
}