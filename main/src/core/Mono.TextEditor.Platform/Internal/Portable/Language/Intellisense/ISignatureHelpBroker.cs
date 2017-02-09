////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines the signature help broker, which is the primary component of the signature help process.  
    /// </summary>
    /// <remarks>
    /// The broker is responsible for
    /// handling each signature help session from beginning to end. IntelliSense controllers
    /// request this broker to trigger or dismiss signature help. The broker can also be used by other components to determine the
    /// state of signature help or to trigger the process.
    /// </remarks>
    public interface ISignatureHelpBroker
    {
        /// <summary>
        /// Begins the process of signature help at the position of the caret.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to trigger signature help.</param>
        /// <returns>A valid signature help session. May be null if no session could be created.</returns>
        ISignatureHelpSession TriggerSignatureHelp(ITextView textView);

        /// <summary>
        /// Starts the process of signature help at the specified point.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to trigger signature help.</param>
        /// <param name="triggerPoint">The point in the text buffer at which signature help is requested.</param>
        /// <param name="trackCaret">
        /// <c>true</c> if this session should track the caret, <c>false</c> otherwise. When the caret is tracked,
        /// the only items to be displayed are those whose applicability
        /// span contains the caret.
        /// </param>
        /// <returns>A valid signature help session. May be null if no session could be created.</returns>
        ISignatureHelpSession TriggerSignatureHelp(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret);

        /// <summary>
        /// Creates a signature help session without starting it.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which signature help is requested.</param>
        /// <param name="triggerPoint">The point in the text buffer at which signature help is requested.</param>
        /// <param name="trackCaret">
        /// <c>true</c> if this session should track the caret, <c>false</c> otherwise. When the caret is tracked,
        /// the only items to be displayed are those whose applicability
        /// span contains the caret.
        /// </param>
        /// <returns>A valid, unstarted signature help session. May be null if no session could be created.</returns>
        ISignatureHelpSession CreateSignatureHelpSession(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret);

        /// <summary>
        /// Dismisses any active signature help sessions in this broker's <see cref="Microsoft.VisualStudio.Text.Editor.ITextView"/>. 
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which all signature help sessions should be dismissed.</param>
        /// <remarks>
        /// This method is valid only when called while signature help is active.
        /// </remarks>
        void DismissAllSessions(ITextView textView);

        /// <summary>
        /// Determines whether signature help is active.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over signature help status should be determined.</param>
        /// <returns>
        /// <c>true</c> if there is at least one signature help session over the specified <see cref="ITextView"/>, <c>false</c>
        /// otherwise.
        /// </returns>
        bool IsSignatureHelpActive(ITextView textView);

        /// <summary>
        /// Gets the list of all signature help sessions for this broker's <see cref="Microsoft.VisualStudio.Text.Editor.ITextView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to retrieve signature help sessions.</param>
        /// <returns>A <see cref="ReadOnlyCollection&lt;ISignatureHelpSession&gt;"/> of signature help sessions.</returns>
        ReadOnlyCollection<ISignatureHelpSession> GetSessions(ITextView textView);
    }
}
