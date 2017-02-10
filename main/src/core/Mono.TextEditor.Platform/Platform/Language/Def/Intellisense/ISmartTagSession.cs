////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation. All rights reserved
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a smart tag session, which encapsulates all the information about a particular invocation of the smart tag system.
    /// </summary>
    [Obsolete(SmartTag.DeprecationMessage)]
    public interface ISmartTagSession : IIntellisenseSession
    {
        /// <summary>
        /// Gets or sets the span to which this session is applicable in the text buffer. This is used to position any popups that are rendered by smart tag
        /// presenters.
        /// </summary>
        /// <remarks>
        /// If, during smart tag session calculation, no smart tag source sets this property then the session will be dismissed.
        /// </remarks>
        ITrackingSpan ApplicableToSpan { get; set; }

        /// <summary>
        /// Occurs when the <see cref="ApplicableToSpan"/> property changes.
        /// </summary>
        event EventHandler ApplicableToSpanChanged;

        /// <summary>
        /// The span over which the tag should be rendered
        /// </summary>
        ITrackingSpan TagSpan { get; set; }

        /// <summary>
        /// Raised when the TagSpan property changes.
        /// </summary>
        event EventHandler TagSpanChanged;

        /// <summary>
        /// Gets the collection of actions that this session displays.
        /// </summary>
        ReadOnlyObservableCollection<SmartTagActionSet> ActionSets { get; }

        /// <summary>
        /// Gets or sets the text to be displayed with the tag.  
        /// </summary>
        /// <remarks>
        /// This text is independent of any individual action. The default presenter
        /// displays this text as a tooltip alongside the tag in its intermediate state.
        /// </remarks>
        string TagText { get; set; }

        /// <summary>
        /// The type of this smart tag session.
        /// </summary>
        SmartTagType Type { get; }

        /// <summary>
        /// Gets or sets the current state of this session. Collapsed sessions are rendered as a small colored rectangle by the
        /// default presenter. Expanded sessions are rendered as a menu containing all of the valid actions.
        /// </summary>
        SmartTagState State { get; set; }

        /// <summary>
        /// Fired when the state of this session changes
        /// </summary>
        event EventHandler StateChanged;

        /// <summary>
        /// Gets/Sets an icon that could be used in the display of this session.  The default presenter renders this icon in the
        /// smart tag button which appears when hovering over the tag.
        /// </summary>
        ImageSource IconSource { get; set; }

        /// <summary>
        /// Fired when the session's icon changes.
        /// </summary>
        event EventHandler IconSourceChanged;
    }
}
