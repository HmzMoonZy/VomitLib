using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Twenty2.VomitLib.View
{
    public class ViewLoaderAddressable : IViewLoader
    {
        private Func<string, string> _getViewAddress;

        public ViewLoaderAddressable(Func<string, string> getViewAddress)
        {
            _getViewAddress = getViewAddress;
        }

        public async UniTask<GameObject> CreateViewAsync(string viewName, Transform parent)
        {
            var handle = Addressables.InstantiateAsync(_getViewAddress(viewName), parent);
            await handle.ToUniTask();
            return handle.Result;
        }

        public void ReleaseView(GameObject view)
        {
            Addressables.ReleaseInstance(view);
        }
    }
}