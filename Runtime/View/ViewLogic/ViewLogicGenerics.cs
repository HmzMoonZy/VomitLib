using Cysharp.Threading.Tasks;

namespace Twenty2.VomitLib.View
{
    /// <summary>
    /// 一个呼出且需要监听事件的ViewLogic
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public abstract class ViewLogic<TEvent> : ViewLogic where TEvent : struct
    {
        private bool _openFinishToken;
        
        public override UniTask OnOpened()
        {
            _openFinishToken = false;
            
            this.RegisterViewEvent<TEvent>(OnOpen);

            return UniTask.WaitUntil(() => _openFinishToken);
        }


        protected virtual void OnOpen(TEvent e)
        {
            OpenDone();
        }

        protected void OpenDone()
        {
            _openFinishToken = true;
        }
    }
}