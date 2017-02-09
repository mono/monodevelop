// Copyright (c) Microsoft Corporation
// All rights reserved

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a Peek broker, which is globally responsible for managing <see cref="IPeekSession"/>s.
    /// </summary>
    /// <remarks>This is a MEF component, and should be imported as follows:
    /// [Import]
    /// IPeekBroker peekBroker = null;
    /// </remarks>
    public interface IPeekBroker
    {
        /// <summary>
        /// Starts a Peek session, assuming the caret position to be the position of a peekable symbol
        /// on which a Peek session is requested.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to trigger a Peek session.</param>
        /// <param name="relationshipName">The name of the requested relationship to be explored by a Peek session.</param>
        /// <returns>A valid Peek session. May be null if no session could be created at the caret position 
        /// for the given relationship.</returns>
        IPeekSession TriggerPeekSession(ITextView textView, string relationshipName);

        /// <summary>
        /// Starts a Peek session at a particular position, which is assumed to be the position of a peekable symbol
        /// on which a Peek session is requested.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to trigger a Peek session.</param>
        /// <param name="triggerPoint">The point in the text buffer at which a Peek session is requested.</param>
        /// <param name="relationshipName">The name of the requested relationship to be explored by a Peek session.</param>
        /// <returns>A valid Peek session. May be null if no session could be created at the trigger point
        /// for the given relationship.</returns>
        IPeekSession TriggerPeekSession(ITextView textView, ITrackingPoint triggerPoint, string relationshipName);

        /// <summary>
        /// Starts a Peek session with the specified options.
        /// </summary>
        /// <param name="options">The options needed to create a Peek session.</param>
        /// <returns>
        /// A valid Peek session. May be null if no session could be created for the
        /// <see cref="PeekSessionCreationOptions.TriggerPoint"/> for the given <see cref="PeekSessionCreationOptions.RelationshipName"/>.
        /// </returns>
        IPeekSession TriggerPeekSession(PeekSessionCreationOptions options);

        /// <summary>
        /// Starts a nested Peek session, assuming the caret position to be the position of a peekable symbol
        /// on which a nested Peek session is requested. A Peek session is considered to be nested when it's
        /// started from a text view that represents an <see cref="IPeekResult"/> of a containing Peek session.
        /// This method doesn't create a new Peek session though, instead it adds another <see cref="IPeekableItem"/> to
        /// the containing session.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to trigger a nested Peek session.</param>
        /// <param name="relationshipName">The name of the requested relationship to be explored by a nested Peek session.</param>
        /// <param name="containingSession">The containing Peek session.</param>
        void TriggerNestedPeekSession(ITextView textView, string relationshipName, IPeekSession containingSession);

        /// <summary>
        /// Starts a nested Peek session at a particular position, which is assumed to be the position of a peekable symbol
        /// on which a nested Peek session is requested. A Peek session is considered to be nested when it's
        /// started from a text view that represents an <see cref="IPeekResult"/> of a containing Peek session.
        /// This method doesn't create a new Peek session though, instead it adds another <see cref="IPeekableItem"/> to
        /// the containing session.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to trigger a Peek session.</param>
        /// <param name="triggerPoint">The point in the text buffer at which a Peek session is requested.</param>
        /// <param name="relationshipName">The name of the requested relationship to be explored by a Peek session.</param>
        /// <param name="containingSession">The containing Peek session.</param>
        void TriggerNestedPeekSession(ITextView textView, ITrackingPoint triggerPoint, string relationshipName, IPeekSession containingSession);

        /// <summary>
        /// Starts a nested Peek session, assuming the options specify a peekable symbol
        /// on which a nested Peek session is requested. A Peek session is considered to be nested when it's
        /// started from a text view that represents an <see cref="IPeekResult"/> of a containing Peek session.
        /// This method doesn't create a new Peek session though, instead it adds another <see cref="IPeekableItem"/> to
        /// the containing session.
        /// </summary>
        /// <param name="options">The options needed to create a Peek session.</param>
        /// <param name="containingSession">The containing Peek session.</param>
        void TriggerNestedPeekSession(PeekSessionCreationOptions options, IPeekSession containingSession);

        /// <summary>
        /// Determines whether a Peek session can be triggered at the caret position, without actually triggering it. 
        /// Note, that an ability to trigger a Peek session doesn't mean that when triggered the session will necessarily 
        /// provide results.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to check if a Peek session can be triggered.</param>
        /// <param name="relationshipName">The name of the requested relationship to be explored by a Peek session.</param>
        /// <param name="isStandaloneFilePredicate">A predicate used to determine whether given file is a standalone (not part of a project) file.</param>
        /// <returns><c>true</c> if a Peek session can be triggered at the caret position, <c>false</c> otherwise.</returns>
        bool CanTriggerPeekSession(ITextView textView, string relationshipName, Predicate<string> isStandaloneFilePredicate);

        /// <summary>
        /// Determines whether a Peek session can be triggered at a particular position, without actually triggering it. 
        /// Note, that an ability to trigger a Peek session doesn't mean that when triggered the session will necessarily 
        /// provide results.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to check if a Peek session can be triggered.</param>
        /// <param name="triggerPoint">The point in the text buffer at which a Peek session is requested.</param>
        /// <param name="relationshipName">The name of the requested relationship to be explored by a Peek session.</param>
        /// <param name="isStandaloneFilePredicate">A predicate used to determine whether given file is a standalone (not part of a project) file.</param>
        /// <returns><c>true</c> if a Peek session can be triggered at the position, <c>false</c> otherwise.</returns>
        bool CanTriggerPeekSession(ITextView textView, ITrackingPoint triggerPoint, string relationshipName, Predicate<string> isStandaloneFilePredicate);

        /// <summary>
        /// Determines whether a Peek session can be triggered with the specified options, without actually triggering it. 
        /// Note, that an ability to trigger a Peek session doesn't mean that when triggered the session will necessarily 
        /// provide results.
        /// </summary>
        /// <param name="options">The options needed to create a Peek session.</param>
        /// <param name="isStandaloneFilePredicate">A predicate used to determine whether given file is a standalone (not part of a project) file.</param>
        /// <returns><c>true</c> if a Peek session can be triggered at the position, <c>false</c> otherwise.</returns>
        bool CanTriggerPeekSession(PeekSessionCreationOptions options, Predicate<string> isStandaloneFilePredicate);

        /// <summary>
        /// Creates, but does not start a Peek session at a particular position, which is assumed to be 
        /// the position of a peekable symbol on which a Peek session is requested.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to trigger a Peek session.</param>
        /// <param name="triggerPoint">The point in the text buffer at which a Peek session is requested.</param>
        /// <param name="relationshipName">The name of the requested relationship to be explored by a Peek session.</param>
        /// <returns>A valid Peek session. May be null if no session could be created at the trigger point 
        /// for the given relationship.</returns>
        IPeekSession CreatePeekSession(ITextView textView, ITrackingPoint triggerPoint, string relationshipName);

        /// <summary>
        /// Creates, but does not start a Peek session with the specified options.
        /// </summary>
        /// <param name="options">The options needed to create a Peek session.</param>
        /// <returns>
        /// A valid Peek session. May be null if no session could be created for the
        /// <see cref="PeekSessionCreationOptions.TriggerPoint"/> for the given <see cref="PeekSessionCreationOptions.RelationshipName"/>.
        /// </returns>
        IPeekSession CreatePeekSession(PeekSessionCreationOptions options);

        /// <summary>
        /// Dismisses an active Peek session for a particular <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to dismiss an active Peek session (if any).</param>
        void DismissPeekSession(ITextView textView);

        /// <summary>
        /// Determines whether or not a Peek session is active over the specified <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> for which to determine if Peek session is active.</param>
        /// <returns>true if an active Peek session exists for the given <see cref="ITextView"/>, false otherwise.</returns>
        bool IsPeekSessionActive(ITextView textView);

        /// <summary>
        /// Gets the active Peek session.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> for which to get a Peek session.</param>
        /// <returns>The valid active Peek session for the given <see cref="ITextView"/> or null if it doesn't exist.</returns>
        IPeekSession GetPeekSession(ITextView textView);
    }
}
