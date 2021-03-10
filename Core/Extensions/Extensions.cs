using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dofus.Retro.Supertools.Core.Object;

namespace Dofus.Retro.Supertools.Core.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<T> WaitOrCancel<T>(this Task<T> task, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await Task.WhenAny(task, token.WhenCanceled());
            token.ThrowIfCancellationRequested();

            return await task;
        }

        public static Task WhenCanceled(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        public static OcrString FindOcrString(this string name)
        {
            return Static.Datacenter.OcrStrings.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
