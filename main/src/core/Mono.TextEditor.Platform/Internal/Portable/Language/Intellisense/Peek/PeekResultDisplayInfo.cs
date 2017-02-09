// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Windows;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines elements of <see cref="IPeekResult"/> display information.
    /// </summary>
    public class PeekResultDisplayInfo : IPeekResultDisplayInfo
    {
        /// <summary>
        /// Defines the localized label used for displaying this result to the user.
        /// This value will be used to represent <see cref="IPeekResult"/> in the Peek control's result list.
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Defines the localized label tooltip used for displaying this result to the user.
        /// </summary>
        /// <remarks>
        /// Supported content types are strings and <see cref="UIElement" /> instances.
        /// </remarks>
        public object LabelTooltip { get; private set; }

        /// <summary>
        /// Defines the localized title used for displaying this result to the user.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Defines the localized title tooltip used for displaying this result to the user.
        /// </summary>
        /// // <remarks>
        /// Supported content types are strings and <see cref="UIElement" /> instances.
        /// </remarks>
        public object TitleTooltip { get; private set; }

        /// <summary>
        /// Creates new instance of the <see cref="PeekResultDisplayInfo"/> class.
        /// </summary>
        public PeekResultDisplayInfo(string label, object labelTooltip, string title, string titleTooltip)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("label");
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("title");
            }

            Label = label;
            LabelTooltip = labelTooltip;
            Title = title;
            TitleTooltip = titleTooltip;
        }

        /// <summary>
        /// Disposes the <see cref="PeekResultDisplayInfo"/> instance.
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose.
        }
    }
}
