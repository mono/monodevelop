using System;
using System.Threading;

namespace Microsoft.VisualStudio.Language.Intellisense.Utilities
{
    public enum WaitIndicatorResult
    {
        Completed,
        Canceled,
    }

    public interface IWaitContext : IDisposable
    {
        CancellationToken CancellationToken { get; }

        bool AllowCancel { get; set; }
        string Message { get; set; }

        void UpdateProgress();
    }
}
