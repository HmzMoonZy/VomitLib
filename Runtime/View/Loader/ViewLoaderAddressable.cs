using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Twenty2.VomitLib.View
{
    public class ViewLoaderAddressable : IViewLoader
    {
        private Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();

        private Func<string, string> _getAddress; 
        
        public ViewLoaderAddressable(Func<string, string> getAddress)
        {
            _getAddress = getAddress;
        }

        public GameObject LoadView(string viewName)
        {
            // 如果在缓存中则直接返回
            if (_cache.TryGetValue(viewName, out var view))
            {
                return view;
            }

            view = Addressables.LoadAssetAsync<GameObject>(_getAddress(viewName)).WaitForCompletion();
            
            _cache.Add(viewName, view);
            
            return view;
        }

        public void ReleaseView(GameObject view)
        {
            // ignore
        }

        public bool IsViewLoaded(string viewName)
        {
            return _cache.ContainsKey(viewName);
        }
    }
}