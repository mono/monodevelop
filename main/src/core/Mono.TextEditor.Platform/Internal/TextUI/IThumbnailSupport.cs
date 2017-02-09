// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;

    public interface IThumbnailSupport
    {
        /// <summary>
        /// Controls whether or note the view's visuals are removed when the view is hidden.
        /// </summary>
        /// <remarks>
        /// Defaults to true and is should to false when generating thumbnails of a hidden view (then restored afterwards).
        /// </remarks>
        bool RemoveVisualsWhenHidden { get; set; }
    }
}
