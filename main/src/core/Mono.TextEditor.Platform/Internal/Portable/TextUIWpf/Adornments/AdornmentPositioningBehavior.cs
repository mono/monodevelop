// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Defines the positioning of adornments.
    /// </summary>
    public enum AdornmentPositioningBehavior
    {
        /// <summary>
        /// The adornment is not moved automatically.
        /// </summary>
        OwnerControlled,    

        /// <summary>
        /// The adornment is positioned relative to the top left corner of the view.
        /// </summary>
        ViewportRelative,   

        /// <summary>
        /// The adornment is positioned relative to the text in the view.
        /// </summary>
        TextRelative        
    }
}
