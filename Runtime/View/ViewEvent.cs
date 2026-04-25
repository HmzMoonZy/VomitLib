namespace Twenty2.VomitLib.View
{
    public static class ViewEvent
    {
        public struct Created
        {
            public ViewLogic ViewLogic;
        }

        public struct Open
        {
            public ViewLogic ViewLogic;
        }

        public struct Close
        {
            public string ViewName;

            public bool IsCache;
        }
    }
}