using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public interface ICompletionSession2 : ICompletionSession
    {
        /// <summary>
        /// Raised following a call to <see cref="IIntellisenseSession.Match"/>.
        /// </summary>
        event EventHandler Matched;

        /// <summary>
        /// Raised after <see cref="ICompletionSession.Filter()"/> was executed.
        /// </summary>
        event EventHandler Filtered;
    }
}