// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System.Windows;

    /// <summary>
    /// Defines the behavior when a <see cref="UIElement"/> is removed from an <see cref="IAdornmentLayer"/>.
    /// </summary>
    /// <param name="tag">The tag associated with <paramref name="element"/>.</param>
    /// <param name="element">The <see cref="UIElement"/> removed from the view.</param>
    public delegate void AdornmentRemovedCallback(object tag, UIElement element);
}
