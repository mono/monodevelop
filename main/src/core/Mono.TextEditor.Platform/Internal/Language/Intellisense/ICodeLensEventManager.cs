//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides a generic event manager for raising events to ICodeLensEventHandlers.
    /// </summary>
    public interface ICodeLensEventManager
    {
        /// <summary>
        /// Report a CodeLens indicator click.
        /// </summary>
        /// <param name="indicatorName">The name of the clicked indicator.</param>
        /// <param name="keyboardUsed">True if the keyboard was used to invoke the indicator.</param>
        void OnIndicatorInvoked(string indicatorName, bool keyboardUsed);

        /// <summary>
        /// Report a CodeLens indicator was pinned.
        /// </summary>
        /// <param name="indicatorName">The name of the clicked indicator.</param>
        /// <param name="keyboardUsed">True if the keyboard was used to pin the indicator.</param>
        /// <param name="args">Arguments to recreate details popup content</param>
        void OnPinInvoked(string indicatorName, bool keyboardUsed, CodeLensPinEventArgs args);
    }
}
