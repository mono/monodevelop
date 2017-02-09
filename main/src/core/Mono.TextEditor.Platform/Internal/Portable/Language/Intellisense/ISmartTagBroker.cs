////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Microsoft.VisualStudio.Text;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text.Editor;
using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a smart tag broker, which is responsible for triggering smart tags. Components call methods on the
    /// broker in order to trigger smart tags.
    /// </summary>
    /// <remarks>
    /// To create a smart tag session, use <see cref="ISmartTagBroker.CreateSmartTagSession"/>,
    /// add some context data into the session's property bag, and call <see cref="IIntellisenseSession.Start"/>.
    /// During the <see cref="IIntellisenseSession.Start"/> call, the session is calculated
    /// for the first time, and in <see cref="ISmartTagSource.AugmentSmartTagSession"/> the smart tag source
    /// can return actions that will be added to the <see cref="ISmartTagSession.ActionSets"/>.  <see cref="ISmartTagSource"/>s
    /// should also set the <see cref="ISmartTagSession.ApplicableToSpan"/> property based on the context data that was earlier added
    /// to the session's property bag. If, during any smart tag session calculation,
    /// the session doesn't get any actions or an applicability span, then the session will be immediately dismissed.
    /// </remarks>
    [Obsolete(SmartTag.DeprecationMessage)]
    public interface ISmartTagBroker
    {
        /// <summary>
        /// Creates a smart tag session for smart tags of the specified type at the specified location.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to create a smart tag session.</param>
        /// <param name="type">The type of smart tag that should be created.</param>
        /// <param name="triggerPoint">The location in the buffer where the smart tag session should be created.</param>
        /// <param name="state">The initial state of the smart tag session.</param>
        /// <returns>A valid smart tag session or null.</returns>
        ISmartTagSession CreateSmartTagSession(ITextView textView, SmartTagType type, ITrackingPoint triggerPoint, SmartTagState state);

        /// <summary>
        /// Gets the list of currently-active smart tag sessions for the textview and subject buffer over which the broker is active
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to retrieve a list of smart tag sessions.</param>
        /// <returns>A list of smart tag sessions for the specified <see cref="ITextView"/>.</returns>
        ReadOnlyCollection<ISmartTagSession> GetSessions(ITextView textView);

        /// <summary>
        /// Determines whether a smart tag is active.
        /// </summary>
        /// <param name="textView">
        /// The <see cref="ITextView"/> over which to determine if there are any active smart tag sessions.
        /// </param>
        /// <returns>
        /// <c>true</c> if there is at least one smart tag session for the specified <see cref="ITextView"/>, <c>false</c>otherwise.
        /// </returns>
        bool IsSmartTagActive(ITextView textView);
    }
}
