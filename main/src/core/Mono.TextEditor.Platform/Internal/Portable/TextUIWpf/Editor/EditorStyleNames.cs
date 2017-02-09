// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System.Windows;

    /// <summary>
    /// The names of various editor components where the component's style can be defined by the program hosting the editor.
    /// </summary>
    /// <remarks>
    /// Custom styles need to be defined and placed in the application's ResourceDictionary.
    /// </remarks>
    public static class EditorStyleNames
    {
        /// <summary>
        /// The name of the WPF style used for the collapsed adornment tooltip.
        /// </summary>
        public const string CollapsedAdornmentToolTipStyleName = "CollapsedAdornmentToolTipStyle";
    }
}