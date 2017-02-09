// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.Generic;
    
    /// <summary>
    /// An extension of the ITextView that exposes some internal hooks.
    /// </summary>
    public interface ITextView2 : ITextView
    {
        /// <summary>
        /// The MaxTextRightCoordinate of the view based only on the text contained in the view.
        /// </summary>
        double RawMaxTextRightCoordinate
        {
            get;
        }

        /// <summary>
        /// The minimum value for the view's MaxTextRightCoordinate.
        /// </summary>
        /// <remarks>
        /// If setting this value changes the view's MaxTextRightCoordinate, the view will raise a layout changed event.
        /// </remarks>
        double MinMaxTextRightCoordinate
        {
            get;
            set;
        }

        /// <summary>
        /// Raised whenever the view's MaxTextRightCoordinate is changed.
        /// </summary>
        /// <remarks>
        /// This event will only be rasied if the MaxTextRightCoordinate is changed by changing the MinMaxTextRightCoordinate property
        /// (it will not be raised as a side-effect of a layout even if the layout does change the MaxTextRightCoordinate).
        /// </remarks>
        event EventHandler MaxTextRightCoordinateChanged;
    }
}
