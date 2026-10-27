using System.Threading;
using System.Threading.Tasks;

namespace ROOT.Shared.Utils
{
    /// <summary>
    /// Copied from https://www.meziantou.net/waiting-for-a-manualreseteventslim-to-be-set-asynchronously.htm
    /// </summary>
    public static class WaitHandleExtensions
    {
        public static Task WaitAsync(this ManualResetEventSlim manualResetEvent, CancellationToken cancellationToken = default)
            => WaitAsync(manualResetEvent.WaitHandle, cancellationToken);

        public static Task WaitAsync(this WaitHandle waitHandle, CancellationToken cancellationToken = default)
        {
            CancellationTokenRegistration cancellationRegistration = default;

            var tcs = new TaskCompletionSource();
            var handle = ThreadPool.RegisterWaitForSingleObject(
                waitObject: waitHandle,
                callBack: (o, timeout) =>
                {
                    cancellationRegistration.Unregister();
                    tcs.TrySetResult();
                },
                state: null,
                timeout: Timeout.InfiniteTimeSpan,
                executeOnlyOnce: true);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.Register(() =>
                {
                    handle.Unregister(waitHandle);
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            return tcs.Task;
        }
    }
}
