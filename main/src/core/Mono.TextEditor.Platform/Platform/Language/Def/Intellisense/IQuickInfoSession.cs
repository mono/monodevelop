////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation. All rights reserved
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.VisualStudio.Text;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines an IntelliSense session used to display Quick Info information.
    /// </summary>
    public interface IQuickInfoSession : IIntellisenseSession
    {
        /// <summary>
        /// Gets the content that will be displayed by this session.  
        /// </summary>
        /// <remarks>
        /// Several types of content are supported, including strings,
        /// <see cref="ITextBuffer" /> instances, and <see cref="UIElement" /> instances.
        /// </remarks>
        BulkObservableCollection<object> QuickInfoContent { get; }

        /// <summary>
        /// Gets the applicability span for this session.  
        /// </summary>
        /// <remarks>
        /// The applicability span is the span of text in the <see cref="ITextBuffer" /> to which this
        /// session pertains. The default Quick Info presenter renders a popup near this location. If this session tracks the
        /// mouse, the session will be dismissed when the mouse leaves this <see cref="ITrackingSpan" />.
        /// </remarks>
        ITrackingSpan ApplicableToSpan { get; }

        /// <summary>
        /// Occurs when the ApplicableToSpan property on this session changes.
        /// </summary>
        event EventHandler ApplicableToSpanChanged;

        /// <summary>
        /// Determines whether this session tracks the mouse.  
        /// </summary>
        /// <remarks>
        /// When the session tracks the mouse, it will be dismissed
        /// when the mouse pointer leaves the applicability span for this session.
        /// </remarks>
        bool TrackMouse { get; }
    }
}
