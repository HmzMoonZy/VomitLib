namespace Twenty2.VomitLib.View
{
    public interface IViewLocker
    {
        public void Lock(ViewLogic view);

        public void UnLock(ViewLogic view);
    }
}