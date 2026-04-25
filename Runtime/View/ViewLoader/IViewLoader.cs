using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Twenty2.VomitLib.View
{
    public interface IViewLoader
    {
        public UniTask<GameObject> CreateViewAsync(string viewName, Transform parent);

        public void ReleaseView(GameObject view);
    }
}

