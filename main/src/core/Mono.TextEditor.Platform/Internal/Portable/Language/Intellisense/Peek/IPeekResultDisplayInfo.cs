// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Windows;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines elements of <see cref="IPeekResult"/> display information.
    /// </summary>
    public interface IPeekResultDisplayInfo : IDisposable
    {
        /// <summary>
        /// Defines the localized label used for displaying this result to the user.
        /// This value will be used to represent <see cref="IPeekResult"/> in the Peek control's result list.
        /// </summary>
        string Label { get; }

        /// <summary>
        /// Defines the localized label tooltip used for displaying this result to the user.
        /// </summary>
        /// <remarks>
        /// Supported content types are strings and <see cref="UIElement" /> instances.
        /// </remarks>
        object LabelTooltip { get; }

        /// <summary>
        /// Defines the localized title used for displaying this result to the user.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Defines the localized title tooltip used for displaying this result to the user.
        /// </summary>
        /// // <remarks>
        /// Supported content types are strings and <see cref="UIElement" /> instances.
        /// </remarks>
        object TitleTooltip { get; }
    }
}
