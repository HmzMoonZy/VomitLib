using Cysharp.Threading.Tasks;

namespace Twenty2.VomitLib.Tools
{
    public interface IState
    {
        public virtual bool Condition()
        {
            return true;
        }
        
        public UniTask Enter(IState context);

        public virtual void Update()
        {
            
        }

        public virtual void FixedUpdate()
        {
            
        }
        
        public UniTask Exit();
    }
}