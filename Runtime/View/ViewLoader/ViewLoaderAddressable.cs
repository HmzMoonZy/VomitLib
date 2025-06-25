using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

namespace Twenty2.VomitLib.View
{
    public class ViewLoaderAddressable : IViewLoader
    {
        private Func<string, string> _getViewAddress;   // 根据viewName获取地址
        
        public ViewLoaderAddressable(Func<string, string> getViewAddress)
        {
            _getViewAddress = getViewAddress;
        }

        public async UniTask<GameObject> CreateView(string viewName, Transform parent)
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