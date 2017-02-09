// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a LightBulb broker, which is globally responsible for managing <see cref="ILightBulbSession"/>s.
    /// </summary>
    /// <remarks>This is a MEF component, and should be imported as follows:
    /// [Import]
    /// ILightBulbBroker lightBulbBroker = null;
    /// </remarks>
    [CLSCompliant(false)]
    public interface ILightBulbBroker
    {
        /// <summary>
        /// Determines whether or not an <see cref="ILightBulbSession"/> is active over the specified <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> for which to determine if an <see cref="ILightBulbSession"/> is active.</param>
        /// <returns><c>true</c> if an active <see cref="ILightBulbSession"/> exists for the given <see cref="ITextView"/>, <c>false</c> otherwise.</returns>
        bool IsLightBulbSessionActive(ITextView textView);

        /// <summary>
        /// Gets the active <see cref="ILightBulbSession"/> for the specified <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> for which to get an <see cref="ILightBulbSession"/>.</param>
        /// <returns>The valid active <see cref="ILightBulbSession"/> for the given <see cref="ITextView"/> or null if it doesn't exist.</returns>
        ILightBulbSession GetSession(ITextView textView);

        /// <summary>
        /// Asynchronously determines whether any <see cref="ISuggestedAction"/>s are associated with the current caret 
        /// position in a given <see cref="ITextView"/>.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="textView">The <see cref="ITextView"/> over which to determine whether any <see cref="ISuggestedAction"/>s 
        /// are associated with the current caret position.</param>
        /// <param name="cancellationToken">Cancellation token to cancel this asynchronous operation.</param>
        /// <returns>A task that returns <c>true</c> if any <see cref="ISuggestedAction"/>s are associated with the current caret 
        /// position in a given <see cref="ITextView"/>, <c>false</c> otherwise.</returns>
        Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, ITextView textView, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously determines whether any <see cref="ISuggestedAction"/>s are associated with a given trigger point
        /// position and span in a given <see cref="ITextView"/>.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="textView">The <see cref="ITextView"/> over which to determine whether any <see cref="ISuggestedAction"/>s are associated \
        /// with a given trigger point position and span.</param>
        /// <param name="triggerPoint">The <see cref="ITrackingPoint" /> in the text buffer at which to determine whether any <see cref="ISuggestedAction"/>s are
        /// associated with a given point position and span in a given <see cref="ITextView"/>.</param>
        /// <param name="triggerSpan">The <see cref="ITrackingSpan" /> in the text buffer for which to determine whether any <see cref="ISuggestedAction"/>s are 
        /// associated with a given trigger point position and span in a given <see cref="ITextView"/>.</param>
        /// <returns>A task that returns <c>true</c> if any <see cref="ISuggestedAction"/>s are associated with the current caret 
        /// position in a given <see cref="ITextView"/>, <c>false</c> otherwise.</returns>
        Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, ITextView textView, ITrackingPoint triggerPoint, 
            ITrackingSpan triggerSpan, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether a <see cref="ILightBulbSession"/> can be created for a given <see cref="ITextView"/>
        /// with current caret position as a trigger point.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="textView">The <see cref="ITextView"/> over which to determine if an <see cref="ILightBulbSession"/> can be created.</param>
        /// <returns><c>true</c> if a session can be created, <c>false</c> otherwise.</returns>
        bool CanCreateSession(ISuggestedActionCategorySet requestedActionCategories, ITextView textView);

        /// <summary>
        /// Determines whether a <see cref="ILightBulbSession"/> can be created for a given <see cref="ITextView"/>
        /// at given trigger point.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="textView">The <see cref="ITextView"/> over which to determine if an <see cref="ILightBulbSession"/> can be created.</param>
        /// <param name="triggerPoint">The <see cref="ITrackingPoint" /> in the text buffer at which to determine if an <see cref="ILightBulbSession"/> can be created.</param>
        /// <returns><c>true</c> if a session can be created, <c>false</c> otherwise.</returns>
        bool CanCreateSession(ISuggestedActionCategorySet requestedActionCategories, ITextView textView, ITrackingPoint triggerPoint);

        /// <summary>
        /// Creates, but doesn't expand a <see cref="ILightBulbSession"/> for a given <see cref="ITextView"/> with current caret position
        /// as a trigger point.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="textView">The <see cref="ITextView"/> over which to create an <see cref="ILightBulbSession"/>.</param>
        /// <returns>A valid instance of <see cref="ILightBulbSession"/> or null if no <see cref="ILightBulbSession"/> can be created for
        /// given text view and caret position.</returns>
        ILightBulbSession CreateSession(ISuggestedActionCategorySet requestedActionCategories, ITextView textView);

        /// <summary>
        /// Creates, but doesn't expand a <see cref="ILightBulbSession"/> for a given <see cref="ITextView"/> with current caret position
        /// as a trigger point.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="textView">The <see cref="ITextView"/> over which to create an <see cref="ILightBulbSession"/>.</param>
        /// <param name="triggerPoint">The <see cref="ITrackingPoint"/> in the text buffer at which to create an <see cref="ILightBulbSession"/>.</param>
        /// <param name="triggerSpan">The <see cref="ITrackingSpan"/> in the text buffer for which to create an <see cref="ILightBulbSession"/>.</param>
        /// <param name="trackMouse">Determines whether the session should track the mouse.</param>
        /// <returns>A valid instance of <see cref="ILightBulbSession"/> or null if no <see cref="ILightBulbSession"/> can be created for
        /// given text view and caret position.</returns>
        ILightBulbSession CreateSession(ISuggestedActionCategorySet requestedActionCategories, ITextView textView, 
            ITrackingPoint triggerPoint, ITrackingSpan triggerSpan, bool trackMouse);

        /// <summary>
        /// Tries to create and expand <see cref="ILightBulbSession"/> for the specified <see cref="ITextView"/>.
        /// If the session already exists, this method expands it.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="textView">The <see cref="ITextView"/> for which to create and expand an <see cref="ILightBulbSession"/>.</param>
        /// <returns><c>true</c> if <see cref="ILightBulbSession"/> was successfully created and expanded, <c>false</c> otherwise.</returns>
        bool TryExpandSession(ISuggestedActionCategorySet requestedActionCategories, ITextView textView);

        /// <summary>
        /// Tries to create and expand <see cref="ILightBulbSession"/> for the specified <see cref="ITextView"/>.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="textView">The <see cref="ITextView"/> for which to create and expand an <see cref="ILightBulbSession"/>.</param>
        /// <param name="triggerPoint">The <see cref="ITrackingPoint"/> in the text buffer at which to create an <see cref="ILightBulbSession"/>.</param>
        /// <param name="triggerSpan">The <see cref="ITrackingSpan"/> in the text buffer for which to create an <see cref="ILightBulbSession"/>.</param>
        /// <param name="trackMouse">Determines whether the session should track the mouse.</param>
        /// <returns><c>true</c> if <see cref="ILightBulbSession"/> was successfully created and expanded, <c>false</c> otherwise.</returns>
        bool TryExpandSession(ISuggestedActionCategorySet requestedActionCategories, ITextView textView, 
            ITrackingPoint triggerPoint, ITrackingSpan triggerSpan, bool trackMouse);

        // <summary>
        /// Dismisses an active <see cref="ILightBulbSession"/> for a particular <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> over which to dismiss an active <see cref="ILightBulbSession"/> (if any).</param>
        void DismissSession(ITextView textView);

        /// <summary>
        /// Determines whether there is at least one <see cref="ISuggestedActionsSourceProvider"/> supporting
        /// given content type.
        /// </summary>
        /// <param name="contentType">The content type to check if there is at least one <see cref="ISuggestedActionsSourceProvider"/> supporting it.</param>
        /// <returns><c>true</c> if there is at least one <see cref="ISuggestedActionsSourceProvider"/> supporting given content type, <c>false</c> otherwise.</returns>
        bool IsSupportedContentType(IContentType contentType);

        /// <summary>
        /// Gets a list of <see cref="ISuggestedActionsSource"/>s for given <see cref="ITextView"/> and <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> for which to get the list of <see cref="ISuggestedActionsSource"/>s.</param>
        /// <param name="buffer">The <see cref="ITextBuffer"/> for which to get the list of <see cref="ISuggestedActionsSource"/>s.</param>
        /// <returns>A list of <see cref="ISuggestedActionsSource"/>s for given <see cref="ITextView"/> and <see cref="ITextBuffer"/>
        /// or null if no <see cref="ISuggestedActionsSource"/>s support given <see cref="ITextView"/> and <see cref="ITextBuffer"/>.</returns>
        IEnumerable<ISuggestedActionsSource> GetSuggestedActionsSources(ITextView textView, ITextBuffer buffer);
    }
}
