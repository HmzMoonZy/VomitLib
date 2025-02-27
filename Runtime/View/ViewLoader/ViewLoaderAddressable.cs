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
        private Func<string, string> _getViewAddress; 
        
        public ViewLoaderAddressable(Func<string, string> getViewAddress)
        {
            _getViewAddress = getViewAddress;
        }

        public GameObject CreateView(string viewName, Transform parent)
        {
            return Addressables.InstantiateAsync(_getViewAddress(viewName), parent).WaitForCompletion();
        }

        public void ReleaseView(GameObject view)
        {
            Addressables.ReleaseInstance(view);
        }
    }
}