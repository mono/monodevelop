// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System.Windows;

    /// <summary>
    /// Represents the KeyboardFilter ordering name.
    /// </summary>
    public static class WpfTextViewKeyboardFilterName
    {
        /// <summary>
        /// The value of the Name attribute on the IKeyboardFilterProvider production.
        /// </summary>
        /// <remarks>
        /// You can use this name to order other keyboard filters relative to the keyboard 
        /// filter that performs command keybinding dispatching.
        /// </remarks>
        public const string KeyboardFilterOrderingName = "Wpf Text View Keyboard Filter";
    }
}