using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Twenty2.VomitLib.View
{
    public interface IViewLoader
    {
        public UniTask<GameObject> LoadView(string viewName);

        public UniTask<GameObject> LoadComp(string compName);
        
        public void ReleaseView(GameObject view);
        
        public void ReleaseComp(GameObject comp);
    }
}

