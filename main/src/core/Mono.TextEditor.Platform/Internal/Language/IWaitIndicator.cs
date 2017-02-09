using System;

namespace Microsoft.VisualStudio.Language.Intellisense.Utilities
{
    public interface IWaitIndicator
    {
        /// <summary>
        /// Schedule the action on the caller's thread and wait for the task to complete.
        /// </summary>
        WaitIndicatorResult Wait(string title, string message, bool allowCancel, Action<IWaitContext> action);
        IWaitContext StartWait(string title, string message, bool allowCancel);
    }
}