using Cysharp.Threading.Tasks;

namespace Twenty2.VomitLib.View
{
    public static class ViewLogicExtension
    {
        public static UniTask<TLogic> WithEvent<TLogic, TEvent>(this UniTask<TLogic> task, TEvent eventParam)
            where TLogic : ViewLogic 
            where TEvent : struct
        {
            Vomit.Interface.SendEvent(eventParam);
            return task;
        }
    }
}