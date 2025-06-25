namespace Twenty2.VomitLib.View
{
    public interface EvtView
    {
        public struct Create
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