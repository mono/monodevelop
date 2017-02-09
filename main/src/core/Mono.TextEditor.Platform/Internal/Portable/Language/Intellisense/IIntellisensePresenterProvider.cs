////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Creates IntelliSense presenters over a given IntelliSense session.  
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IIntellisensePresenterProvider))]
    /// Component exporters must add the Order attribute to define the order of the presenter in the presenter chain.
    /// </remarks>
    public interface IIntellisensePresenterProvider
    {
        /// <summary>
        /// Attempts to create an IntelliSense presenter for a given IntelliSense session.
        /// </summary>
        /// <param name="session">The session for which a presenter should be created.</param>
        /// <returns>A valid IntelliSense presenter, or null if none could be created.</returns>
        IIntellisensePresenter TryCreateIntellisensePresenter ( IIntellisenseSession session );
    }
}
