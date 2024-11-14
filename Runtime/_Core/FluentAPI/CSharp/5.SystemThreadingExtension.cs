using System.Threading;

namespace FluentAPI
{
    public static class SystemThreadingExtension 
    {
        public static void CancelAndDispose(this CancellationTokenSource cts)
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                cts.Dispose();    
            }
        }


        public static bool IsNullOrCanceled(this CancellationTokenSource cts)
        {
            return cts == null || cts.IsCancellationRequested;
        }
    }
}