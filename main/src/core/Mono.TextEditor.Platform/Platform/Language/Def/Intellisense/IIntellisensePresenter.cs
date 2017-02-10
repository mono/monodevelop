////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a presenter of IntelliSense information.
    /// </summary>
    public interface IIntellisensePresenter
    {
        /// <summary>
        /// Gets the session that this presenter is rendering.
        /// </summary>
        IIntellisenseSession Session { get; }
    }
}
