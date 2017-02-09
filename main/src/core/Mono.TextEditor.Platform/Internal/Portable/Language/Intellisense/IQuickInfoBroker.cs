////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation. All rights reserved
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a Quick Info broker.  
    /// </summary>
    /// <remarks>
    /// The broker is responsible for triggering Quick Info sessions
    /// </remarks>
    public interface IQuickInfoBroker
    {
        /// <summary>
        /// Determines whether there is at least one active Quick Info session in the specified <see cref="ITextView" />.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView" /> for which Quick Info session status is to be determined.</param>
        /// <returns>
        /// <c>true</c> if there is at least one active Quick Info session over the specified <see cref="ITextView" />, <c>false</c>
        /// otherwise.
        /// </returns>
        bool IsQuickInfoActive(ITextView textView);

        /// <summary>
        /// Triggers Quick Info at the position of the caret in the specified <see cref="ITextView" />.  
        /// </summary>
        /// <param name="textView">The <see cref="ITextView" /> for which Quick Info is to be triggered.</param>
        /// <returns>A valid Quick Info session, or null if none could be created.</returns>
        /// <remarks>
        /// Quick Info is triggered in the <see cref="ITextView" /> to which this
        /// broker is attached.
        /// </remarks>
        IQuickInfoSession TriggerQuickInfo(ITextView textView);

        /// <summary>
        /// Triggers Quick Info at the specified position in the buffer, either tracking or not tracking the mouse.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView" /> for which Quick Info is to be triggered.</param>
        /// <param name="triggerPoint">
        /// The <see cref="ITrackingPoint" /> in the text buffer at which Quick Info should be triggered.
        /// </param>
        /// <param name="trackMouse">
        /// <c>true</c> if the session should be dismissed when the mouse leaves the applicability span of the session,
        /// <c>false</c> otherwise.
        /// </param>
        /// <returns>A valid Quick Info session, or null if none could be created.</returns>
        IQuickInfoSession TriggerQuickInfo(ITextView textView, ITrackingPoint triggerPoint, bool trackMouse);

        /// <summary>
        /// Creates but does not start a Quick Info session at the specified location in the <see cref="ITextBuffer" />.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView" /> for which a Quick Info should be created.</param>
        /// <param name="triggerPoint">
        /// The <see cref="ITrackingPoint" /> in the text buffer at which Quick Info should be triggered.
        /// </param>
        /// <param name="trackMouse">
        /// <c>true</c> if the session should be auto-dismissed when the mouse leaves the applicability span of the session,
        /// otherwise <c>false</c>.
        /// </param>
        /// <returns>A valid Quick Info session, or null if none could be created.</returns>
        IQuickInfoSession CreateQuickInfoSession(ITextView textView, ITrackingPoint triggerPoint, bool trackMouse);

        /// <summary>
        /// Gets the set of active Quick Info sessions for the <see cref="ITextView" /> in which this broker operates.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView" /> for which Quick Info sessions should be returned.</param>
        /// <returns>The list of valid Quick Info sessions active over the specified <see cref="ITextView" />.</returns>
        ReadOnlyCollection<IQuickInfoSession> GetSessions(ITextView textView);
    }
}
