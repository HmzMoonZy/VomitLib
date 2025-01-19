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
        private Dictionary<string, GameObject> _viewCache = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> _compCache = new Dictionary<string, GameObject>();
        private Dictionary<string, SpriteAtlas> _atlasCache = new Dictionary<string, SpriteAtlas>();

        private Func<string, string> _getViewAddress; 
        private Func<string, string> _getCompAddress; 
        private Func<string, string> _getAtlasAddress; 
        
        public ViewLoaderAddressable(Func<string, string> getViewAddress, Func<string, string> getCompAddress, Func<string, string> getAtlasAddress)
        {
            _getViewAddress = getViewAddress;
            _getCompAddress = getCompAddress;
            _getAtlasAddress = getAtlasAddress;
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

        public async UniTask<SpriteAtlas> LoadAtlas(string atlasName)
        {
            if (_atlasCache.TryGetValue(atlasName, out var atlas))
            {
                return atlas;
            }
            
            var handle = Addressables.LoadAssetAsync<SpriteAtlas>(_getAtlasAddress(atlasName));
            await handle.ToUniTask();
            atlas = handle.Result;
            _atlasCache.Add(atlasName, atlas);
            return atlas;
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