namespace Twenty2.VomitLib.HotFix
{
    public class HotFix
    {
        private IHotFixHandler _handler;
        
        public void Init(IHotFixHandler handler)
        {
            _handler = handler;
        }

        public void CheckUpdate()
        {
            if (_handler == null)
            {
                Log.Error("热更新处理器未初始化");
                return;
            }
        }
    }
}