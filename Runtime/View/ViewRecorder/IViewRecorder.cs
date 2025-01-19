namespace Twenty2.VomitLib.View
{
    public interface IViewRecorder
    {
        public bool IsFirstOpen(string viewName);
        public void RecordOpen(string viewName);
    }
}