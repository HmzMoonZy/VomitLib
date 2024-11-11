using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Twenty2.VomitLib.View
{
    public interface IViewLoader
    {
        public GameObject LoadView(string viewName);
        
        public void ReleaseView(GameObject view);

        public bool IsViewLoaded(string viewName);
    }
}

