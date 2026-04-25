namespace Twenty2.VomitLib.View
{
    public interface IViewLocker
    {
        public void Freeze(ViewLogic view);

        public void UnFreeze(ViewLogic view);
    }
}