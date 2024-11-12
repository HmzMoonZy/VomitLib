using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Twenty2.VomitLib.View
{
    public class ViewLoaderAddressable : IViewLoader
    {
        private Dictionary<string, GameObject> _viewCache = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> _compCache = new Dictionary<string, GameObject>();

        private Func<string, string> _getViewAddress; 
        private Func<string, string> _getCompAddress; 
        
        public ViewLoaderAddressable(Func<string, string> getViewAddress, Func<string, string> getCompAddress)
        {
            _getViewAddress = getViewAddress;
            _getCompAddress = getCompAddress;
        }

        public async UniTask<GameObject> LoadView(string viewName)
        {
            // 如果在缓存中则直接返回
            if (_viewCache.TryGetValue(viewName, out var view))
            {
                return view;
            }
            
            var handle = Addressables.LoadAssetAsync<GameObject>(_getViewAddress(viewName));
            await handle.ToUniTask();
            view = handle.Result;
            _viewCache.Add(viewName, view);
            
            return view;
        }

        public async UniTask<GameObject> LoadComp(string compName)
        {
            if (_compCache.TryGetValue(compName, out var comp))
            {
                return comp;
            }
            
            var handle = Addressables.LoadAssetAsync<GameObject>(_getCompAddress(compName));
            await handle.ToUniTask();
            comp = handle.Result;
            _compCache.Add(compName, comp);
            
            return comp;
        }

        public void ReleaseView(GameObject view)
        {
            // ignore
        }

        public void ReleaseComp(GameObject comp)
        {
            // ignore
        }
    }
}