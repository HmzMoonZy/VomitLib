using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;

namespace Twenty2.VomitLib.View
{
    public interface IViewLoader
    {
        public UniTask<GameObject> CreateView(string viewName, Transform parent);
        
        public void ReleaseView(GameObject view);
    }
}

