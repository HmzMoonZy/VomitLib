using System.Threading;

namespace Twenty2.VomitLib
{
    public static class CancellationSourceExtension
    {
        public static void CancelAndDispose(this CancellationTokenSource cts)
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}