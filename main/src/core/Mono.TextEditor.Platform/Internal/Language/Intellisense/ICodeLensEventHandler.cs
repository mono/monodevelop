//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides a generic event listener for tracking CodeLens actions.
    /// </summary>
    public interface ICodeLensEventHandler
    {
        /// <summary>
        /// Report a CodeLens indicator click.
        /// </summary>
        /// <param name="indicatorName">The name of the clicked indicator.</param>
        /// <param name="keyboardUsed">True if the keyboard was used to invoke the indicator.</param>
        void OnIndicatorInvoked(string indicatorName, bool keyboardUsed);

        /// <summary>
        /// Report a change to the CodeLens options.
        /// </summary>
        /// <remarks>Called initially with the current settings.</remarks>
        /// <param name="globalOption">Global CodeLens setting</param>
        /// <param name="enabledProviders">An array of all enabled provider names.</param>
        /// <param name="disabledProviders">An array of all disabled provider names.</param>
        void OnCodeLensOptionsChanged(bool globalOption, string[] enabledProviders, string[] disabledProviders);

        /// <summary>
        /// Report a CodeLens indicator was pinned.
        /// </summary>
        /// <param name="indicatorName">The name of the clicked indicator.</param>
        /// <param name="keyboardUsed">True if the keyboard was used to pin the indicator.</param>
        /// <param name="args">Arguments to recreate details popup content</param>
        void OnPinInvoked(string indicatorName, bool keyboardUsed, CodeLensPinEventArgs args);
    }
}
