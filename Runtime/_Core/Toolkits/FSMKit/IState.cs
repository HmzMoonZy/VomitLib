using Cysharp.Threading.Tasks;

namespace Twenty2.VomitLib.Tools
{
    public interface IState
    {
        public bool Condition();
        public UniTask Enter(IState context);
        public void Update();
        public void FixedUpdate();
        public UniTask Exit();
    }
}