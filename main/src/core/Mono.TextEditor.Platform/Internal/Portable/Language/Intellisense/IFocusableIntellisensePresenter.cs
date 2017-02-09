// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a focusable presenter of IntelliSense information.
    /// </summary>
    public interface IFocusableIntellisensePresenter : ICustomIntellisensePresenter
    {
        /// <summary>
        /// Sets focus within the presentation that this presenter is rendering.
        /// </summary>
        /// <returns><c>true</c> if focus set successfully, <c>false</c> otherwise.</returns>
        bool Focus();
    }
}
