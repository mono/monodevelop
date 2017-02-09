// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a Light Bulb session, which encapsulates all the information about a particular Light Bulb instance.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public interface ILightBulbSession : IIntellisenseSession
    {
        /// <summary>
        /// Determines whether the session is in the collapsed state.
        /// </summary>
        bool IsCollapsed { get; }

        /// <summary>
        /// Determines whether the session is in the expanded state.
        /// </summary>
        bool IsExpanded { get; }

        /// <summary>
        /// Tries to get the list of <see cref="ISuggestedAction"/>s (grouped into <see cref="SuggestedActionSet"/>s)
        /// this session displays.
        /// <param name="actionSets">Resulting list of <see cref="SuggestedActionSet"/>s.</param>
        /// <returns><see cref="QuerySuggestedActionCompletionStatus"/> indicating whether the operation completed successfully
        /// or was canceled.</returns>
        /// </summary>
        /// <remarks>Note that this method is intended to be called in response to a user action (such as Light Bulb expansion)
        /// and will show a wait dialog if it takes too long to complete.</remarks>
        QuerySuggestedActionCompletionStatus TryGetSuggestedActionSets(out IEnumerable<SuggestedActionSet> actionSets);

        /// <summary>
        /// Expands the session.
        /// </summary>
        void Expand();

        /// <summary>
        /// Resets the session content.
        /// </summary>
        void Reset();

        /// <summary>
        /// Fires when the session is collapsed.
        /// </summary>
        event EventHandler Collapsed;

        /// <summary>
        /// Fires when the session is expanded.
        /// </summary>
        event EventHandler Expanded;

        /// <summary>
        /// Determines whether this session tracks the mouse.
        /// </summary>
        bool TrackMouse { get; }

        /// <summary>
        /// Gets the visual span to which this session is applicable in the text buffer. This is used to position the Light Bulb presentation
        /// that is rendered by Light Bulb presenters.
        /// </summary>
        /// <returns></returns>
        SnapshotSpan ApplicableToSpan { get; }

        /// <summary>
        /// Gets a set of suggested action categories this session was requested to provide.
        /// </summary>
        ISuggestedActionCategorySet ActionCategories { get; }
    }
}
