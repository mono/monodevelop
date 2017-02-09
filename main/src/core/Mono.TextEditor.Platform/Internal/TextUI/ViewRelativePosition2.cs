// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Defines the meaning of the verticalOffset parameter in the <see cref="ITextView"/>.DisplayTextLineContaining(...).
    /// </summary>
    /// <remarks>
    /// This enum adds a couple of modes to the ViewRelativePosition that we need to support for the InterLineAdornments
    /// but don't want to expose. The WpfTextView accepts the new values but doesn't pass them on to anyone else.
    /// </remarks>
    public enum ViewRelativePosition2
    {
        /// <summary>
        /// The offset with respect to the top of the view to the top of the line.
        /// </summary>
        /// <remarks>
        /// Must match ViewRelativePosition.Top.
        /// </remarks>
        Top = ViewRelativePosition.Top,

        /// <summary>
        /// The offset with respect to the bottom of the view to the bottom of the line.
        /// </summary>
        /// <remarks>
        /// Must match ViewRelativePosition.Bottom.
        /// </remarks>
        Bottom = ViewRelativePosition.Bottom,

        /// <summary>
        /// The offset with respect to the top of the view to the top of the text on the line.
        /// </summary>
        TextTop,

        /// <summary>
        /// The offset with respect to the bottom of the view to the bottom of the text on the line.
        /// </summary>
        TextBottom
    }
}