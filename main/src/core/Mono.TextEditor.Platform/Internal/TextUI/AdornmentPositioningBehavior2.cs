// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Defines the positioning of adornments.
    /// </summary>
    /// <remarks>
    /// This enum adds a mode to the AdornmentPositioningBehavior needed for diff but we don't want to expose. 
    /// </remarks>
    public enum AdornmentPositioningBehavior2
    {
        /// <summary>
        /// The adornment is not moved automatically.
        /// </summary>
        OwnerControlled = AdornmentPositioningBehavior.OwnerControlled,

        /// <summary>
        /// The adornment is positioned relative to the top left corner of the view.
        /// </summary>
        ViewportRelative = AdornmentPositioningBehavior.ViewportRelative,

        /// <summary>
        /// The adornment is positioned relative to the text in the view.
        /// </summary>
        TextRelative = AdornmentPositioningBehavior.TextRelative,

        /// <summary>
        /// Behaves like a AdornmentPositioningBehavior.TextRelative adornment but only scrolls vertically.
        /// </summary>
        TextRelativeVerticalOnly
    }
}