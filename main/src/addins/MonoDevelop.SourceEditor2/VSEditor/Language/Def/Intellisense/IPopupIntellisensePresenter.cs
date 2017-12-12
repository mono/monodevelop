////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Windows;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using MonoDevelop.Components;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines an IntelliSense presenter that is rendered as a popup within an
    /// <see cref="Microsoft.VisualStudio.Text.Editor.ITextView"/>.
    /// </summary>
    public interface IPopupIntellisensePresenter : IIntellisensePresenter
    {
		/// <summary>
		/// Gets the WPF <see cref="UIElement"/> that the presenter wants to be displayed inside a
		/// <see cref="Microsoft.VisualStudio.Text.Editor.ITextView"/> popup.
		/// </summary>
		Xwt.Widget SurfaceElement { get; }

        /// <summary>
        /// Occurs when the WPF SurfaceElement is changed.
        /// </summary>
        event EventHandler SurfaceElementChanged;

        /// <summary>
        /// Gets the <see cref="ITrackingSpan"/> to which this presenter is related.  
        /// </summary>
        /// <remarks>
        /// This property is used to determine where to
        /// place the <see cref="Microsoft.VisualStudio.Text.Editor.ITextView"/> popup inside of which the presenter's
        /// SurfaceElement is hosted.
        /// </remarks>
        ITrackingSpan PresentationSpan { get; }

        /// <summary>
        /// Occurs when the PresentationSpan property changes.  
        /// </summary>
        /// <remarks>
        /// This is the way popup presenters signal that they should be moved.
        /// </remarks>
        event EventHandler PresentationSpanChanged;

        /// <summary>
        /// Gets a set of flags that determine the popup style.
        /// </summary>
        PopupStyles PopupStyles { get; }

        /// <summary>
        /// Occurs when the PopupStyles property changes.
        /// </summary>
        event EventHandler<ValueChangedEventArgs<PopupStyles>> PopupStylesChanged;

        /// <summary>
        /// Gets the name of the space reservation manager that should be used to create popups for this presenter.  
        /// </summary>
        /// <remarks>
        /// Space reservation
        /// managers can be ordered, thus ensuring predictable popup placement.
        /// </remarks>
        string SpaceReservationManagerName { get; }

        /// <summary>
        /// Gets or sets the opacity of this popup presenter.  
        /// </summary>
        /// <remarks>
        /// The presenter should use this property to set the
        /// opacity of its surface element and of any other text-obscuring UI elements it has provided.
        /// </remarks>
        double Opacity { get; set; }
    }
}
