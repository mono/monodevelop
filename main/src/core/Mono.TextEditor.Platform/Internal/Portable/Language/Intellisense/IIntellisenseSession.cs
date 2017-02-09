////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an IntelliSense session, or a single instance of the IntelliSense process.  
    /// </summary>
    /// <remarks>
    /// A session is returned by each
    /// IntelliSense triggering operation, and can be used to control the process of IntelliSense operations. IntelliSense sessions
    /// are aggregated into a stack, managed by an <see cref="IIntellisenseSessionStack"/> instance.
    /// </remarks>
    public interface IIntellisenseSession : IPropertyOwner
    {
        /// <summary>
        /// Gets the <see cref="ITrackingPoint"/> at which this IntelliSense session was triggered in terms of the specified
        /// <see cref="ITextBuffer"/>.
        /// </summary>
        /// <remarks>
        /// Callers should take care to pass only <see cref="ITextBuffer"/>s that are part of the session.TextView.BufferGraph
        /// </remarks>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> for which a trigger point should be returned.</param>
        /// <returns>
        /// The trigger point of the session as a <see cref="ITrackingSpan"/> in terms of the specified <see cref="ITextBuffer"/>.
        /// </returns>
        ITrackingPoint GetTriggerPoint(ITextBuffer textBuffer);

        /// <summary>
        /// Gets the <see cref="SnapshotPoint"/> at which this IntelliSense session was triggered in terms of the specified
        /// <see cref="ITextSnapshot"/>.
        /// </summary>
        /// <remarks>
        /// Callers should take care to pass only <see cref="ITextSnapshot"/>s that are part of the session.TextView.BufferGraph
        /// </remarks>
        /// <param name="textSnapshot">The <see cref="ITextSnapshot"/> for which a trigger point should be returned.</param>
        /// <returns>
        /// The trigger point of the session as a <see cref="SnapshotPoint"/> in terms of the specified <see cref="ITextSnapshot"/>.
        /// </returns>
        SnapshotPoint? GetTriggerPoint(ITextSnapshot textSnapshot);

        /// <summary>
        /// Gets the <see cref="ITextView"/> inside of which this IntelliSense session was triggered.
        /// </summary>
        ITextView TextView { get; }

        /// <summary>
        /// Gets the <see cref="IIntellisensePresenter"/> that is used to render IntelliSense for this session.  
        /// </summary>
        /// <remarks>This property can
        /// change due to session updates.</remarks>
        IIntellisensePresenter Presenter { get; }

        /// <summary>
        /// Occurs when the IntelliSense presenter for this session changes.  
        /// </summary>
        /// <remarks>
        /// Any consumers of the presenter should re-render the presenter at this time.
        /// </remarks>
        event EventHandler PresenterChanged;

        /// <summary>
        /// Starts the session.  
        /// </summary>
        /// <remarks>
        /// Before this method is called, the session is in an initialization state. It begins processing only when Start()
        /// is called.
        /// </remarks>
        void Start();

        /// <summary>
        /// Dismisses the session, causing the presenter to be destroyed and the session to be removed from the session stack.
        /// </summary>
        void Dismiss();

        /// <summary>
        /// Occurs when the session is dismissed.
        /// </summary>
        event EventHandler Dismissed;

        /// <summary>
        /// Determines whether the session is dismissed.
        /// </summary>
        bool IsDismissed { get; }

        /// <summary>
        /// Recalculates the underlying IntelliSense items pertaining to this session, using the same trigger point.
        /// </summary>
        void Recalculate();

        /// <summary>
        /// Occurs when the session is recalculated.
        /// </summary>
        event EventHandler Recalculated;

        /// <summary>
        /// Determines the best matching item in the session and sets the selection to this item.  
        /// </summary>
        /// <remarks>
        /// The best match is determined by
        /// querying the highest-priority provider for the buffer over which this session is running.
        /// </remarks>
        bool Match();

        /// <summary>
        /// Collapses the session to an unobtrusive state in which it doesn't get in the way of the user.  If the session has no
        /// such state, the session will be dismissed.
        /// </summary>
        /// <remarks>
        /// <see cref="ISmartTagSession"/>s are the only default <see cref="IIntellisenseSession"/>s that have a collapsed state.
        /// All other default sessions (<see cref="ICompletionSession"/>s, <see cref="ISignatureHelpSession"/>s, and
        /// <see cref="IQuickInfoSession"/>s) will be dismissed when collapsed.
        /// </remarks>
        void Collapse();
    }
}
