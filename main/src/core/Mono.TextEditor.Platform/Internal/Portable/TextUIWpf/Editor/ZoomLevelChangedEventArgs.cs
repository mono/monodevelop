// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Windows.Media;

    /// <summary>
    /// Provides information for a ZoomLevelChangedEvent event in the <see cref="IWpfTextView"/>.
    /// </summary>
    public class ZoomLevelChangedEventArgs : EventArgs
    {        
        /// <summary>
        /// Initializes a new instance of a <see cref="ZoomLevelChangedEventArgs"/>.
        /// </summary>
        /// <param name="newZoomLevel">The new zoom level for an <see cref="IWpfTextView"/>.</param>
        /// <param name="transform">The zoom transform used for an <see cref="IWpfTextView"/>.</param>
        public ZoomLevelChangedEventArgs(double newZoomLevel, Transform transform)
        {
            NewZoomLevel = newZoomLevel;
            ZoomTransform = transform;
        }

        /// <summary>
        /// Gets the new zoom level for an <see cref="IWpfTextView"/>.
        /// </summary>
        public double NewZoomLevel
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the zoom tranform to apply 
        /// </summary>
        /// <remarks>Wpf UI elements wishing to be reflect the view's zoom level can set their 
        /// LayoutTransform property to the value of ZoomTransform. 
        /// </remarks>
        public Transform ZoomTransform
        {
            get;
            private set;
        }
    }
}
