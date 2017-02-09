// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System.Windows;

    /// <summary>
    /// Support for theming intra-text adornments that are provided via <see cref="IntraTextAdornmentTag"/>s.
    /// </summary>
    public static class IntraTextAdornment
    {
        /// <summary>
        /// Represents the IsSelected property of these adornments.
        /// </summary>
        public static readonly DependencyProperty IsSelected = DependencyProperty.RegisterAttached("IsSelected", typeof(bool), typeof(IntraTextAdornment));
        
        /// <summary>
        /// Sets the IsSelected value on the specified <see cref="UIElement"/>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="isSelected">The IsSelected value.</param>
        public static void SetIsSelected(UIElement element, bool isSelected)
        {
            element.SetValue(IsSelected, isSelected);
        }

        /// <summary>
        /// Gets the IsSelected value on the specified <see cref="UIElement"/>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns><c>true</c> if the element is selected, otherwise <c>false</c>.</returns>
        public static bool GetIsSelected(UIElement element)
        {
            return true.Equals(element.GetValue(IsSelected));
        }
    }
}
