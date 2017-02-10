////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Describes types of UIElements to be provided via <see cref="IUIElementProvider&lt;T,V&gt;"/>.
    /// </summary>
    public enum UIElementType
    {
        /// <summary>
        /// Small UIElement representing the object in question.
        /// </summary>
        /// <remarks>Small UIElements will most likely be placed in a list alongside other small UIElements.</remarks>
        Small,

        /// <summary>
        /// Large UIElement representing the object in question.
        /// </summary>
        /// <remarks>
        /// Large UIElements will most likely be displayed on their own and should present detailed information about the object in
        /// question.
        /// </remarks>
        Large,

        /// <summary>
        /// UIElement to be hosted in a tooltip representing the object in question.
        /// </summary>
        Tooltip
    }
}
