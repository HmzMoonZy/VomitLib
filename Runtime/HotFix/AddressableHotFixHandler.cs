using System;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Twenty2.VomitLib.HotFix
{
    public class AddressableHotFixHandler : IHotFixHandler
    {
        private string _forceCheckUrl;          // 强制更新检查地址

        public AddressableHotFixHandler(string forceCheckUrl)
        {
            _forceCheckUrl = forceCheckUrl;
        }
        
        public void CheckForceUpdate(Action<bool> onSuc, Action onFail, Func<string, bool> onCheck)
        {
            if (string.IsNullOrEmpty(_forceCheckUrl))
            {
                onSuc?.Invoke(false);
                return;
            }
            
            UniTask.Create(async () =>
            {
                using var uwr = UnityWebRequest.Get(_forceCheckUrl);
                var result = await uwr.SendWebRequest().ToUniTask();
                if (result == null || result.error != null || uwr.result != UnityWebRequest.Result.Success)
                {
                    onFail?.Invoke();
                    return;
                }

                var checkRet = onCheck.Invoke(uwr.downloadHandler.text);
                onSuc?.Invoke(checkRet);
            });
        }
    }
}