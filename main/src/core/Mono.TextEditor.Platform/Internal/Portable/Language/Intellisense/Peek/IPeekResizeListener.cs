// Copyright (c) Microsoft Corporation
// All rights reserved

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Describes an object to be notified when the user resizes the Peek control.
    /// </summary>
    public interface IPeekResizeListener
    {
        /// <summary>
        /// Gets called after the user has manually resized the Peek control.
        /// </summary>
        void OnResized(object sender, PeekResizeEventArgs e);
    }
}