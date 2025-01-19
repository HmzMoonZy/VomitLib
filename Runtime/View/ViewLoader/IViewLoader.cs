using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;

namespace Twenty2.VomitLib.View
{
    public interface IViewLoader
    {
        public UniTask<GameObject> LoadView(string viewName);

        public UniTask<GameObject> LoadComp(string compName);

        public UniTask<SpriteAtlas> LoadAtlas(string atlasName);
        
        public void ReleaseView(GameObject view);
        
        public void ReleaseComp(GameObject comp);
    }
}

