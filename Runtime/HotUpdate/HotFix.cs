namespace Twenty2.VomitLib.HotFix
{
    public static class HotFix
    {
        private static IHotUpdateHandler _handler = null;
        
        public static void SetHandler(IHotUpdateHandler handler)
        {
            _handler = handler;
        }
    }
}