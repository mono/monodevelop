using System;
using System.Threading;

namespace Microsoft.VisualStudio.Language.Intellisense.Utilities
{
    public static class WaitHelper
    {
        public static IWaitContext Wait(IWaitIndicator waitIndicator, string title, string message)
        {
            if (waitIndicator == null)
            {
                return new WaitContext();
            }
            else
            {
                return waitIndicator.StartWait(title, message, allowCancel: true);
            }
        }

        private class WaitContext : IWaitContext
        {
            public CancellationToken CancellationToken
            {
                get
                {
                    return CancellationToken.None;
                }
            }

            public bool AllowCancel { get; set; }
            public string Message { get; set; }

            public void UpdateProgress()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
