// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Windows;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Class that captures the state of some result displayed in Peek.
    /// </summary>
    public interface IPeekResultScrollState : IDisposable
    {
        /// <summary>
        /// Restore the presentation to the captured state.
        /// </summary>
        /// <param name="presentation">Result Presentation to scroll.</param>
        /// <remarks>
        /// <paramref name="presentation"/> will always be the presentation
        /// that created this via presentation.CaptureScrollState().</remarks>
        void RestoreScrollState(IPeekResultPresentation presentation);
    }
}
