using Cysharp.Threading.Tasks;

namespace Twenty2.VomitLib.View
{
    public partial interface EView
    {
        public struct Open
        {
            public System.Type LogicType;

            public ViewLogic ViewLogic;
            
            public UniTaskCompletionSource OpenTask;
        }
    }
}