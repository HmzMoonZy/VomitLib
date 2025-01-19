namespace Twenty2.VomitLib.View
{
    public interface IViewMasker
    {
        public void Mask(ViewLogic view);

        public void Unmask(ViewLogic view);
    }
}