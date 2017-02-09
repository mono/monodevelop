////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation. All rights reserved
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents the central broker responsible for statement completion.
    /// </summary>
    public interface ICompletionBroker
    {
        /// <summary>
        /// Starts the process of statement completion, assuming the caret position to be the position at which completions should
        /// be inserted.  
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to trigger statement completion.</param>
        /// <returns>A valid statement completion session. May be null if no session could be created.</returns>
        /// <remarks>When the caret leaves the
        /// applicability span of all the completions in this session, the session will be automatically dismissed.</remarks>
        ICompletionSession TriggerCompletion(ITextView textView);

        /// <summary>
        /// Starts the process of statement completion at a particular position. When called with a specific trigger point, caret
        /// movements will be ignored and the broker will not be responsible for dismissing the session.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to trigger statement completion.</param>
        /// <param name="triggerPoint">The point in the text buffer at which statement completion is requested.</param>
        /// <param name="trackCaret">
        /// <c>true</c> if this session should track the caret, <c>false</c> otherwise. When the caret is tracked, only completion items whose
        /// applicability span contains the caret will be displayed.
        /// </param>
        /// <returns>A valid statement completion session.  May be null if no session could be created.</returns>
        ICompletionSession TriggerCompletion(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret);

        /// <summary>
        /// Creates a completion session, but does not start it.  
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to create a completion session.</param>
        /// <param name="triggerPoint">The point in the text buffer at which statement completion is requested.</param>
        /// <param name="trackCaret">
        /// <c>true</c> if this session should track the caret, <c>false</c> otherwise. When the caret is tracked, only completion items whose
        /// applicability span contains the caret will be displayed.
        /// </param>
        /// <returns>A valid statement completion session.  May be null if no session could be created.</returns>
        /// <remarks>This method is useful if you want to set some properties on the session
        /// before starting it.</remarks>
        ICompletionSession CreateCompletionSession(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret);

        /// <summary>
        /// Dismisses all active statement completion sessions.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to dismiss all sessions.</param>
        void DismissAllSessions(ITextView textView);

        /// <summary>
        /// Determines whether or not statement completion is active over the specified <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> for which to determine if statement completion is active.</param>
        /// <remarks>This property is <c>true</c> when Sessions.Count > 0 and <c>false</c>
        /// otherwise.</remarks>
        bool IsCompletionActive(ITextView textView);

        /// <summary>
        /// Gets the list of active statement completion sessions.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> for which to get completions.</param>
        ReadOnlyCollection<ICompletionSession> GetSessions(ITextView textView);
    }
}
