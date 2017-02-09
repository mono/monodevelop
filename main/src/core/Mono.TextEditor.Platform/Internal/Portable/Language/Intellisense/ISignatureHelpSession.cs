////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines an IntelliSense session used for displaying signature help.
    /// </summary>
    public interface ISignatureHelpSession : IIntellisenseSession
    {
        /// <summary>
        /// Gets the set of valid signatures for this session.
        /// </summary>
        ReadOnlyObservableCollection<ISignature> Signatures { get; }

        /// <summary>
        /// Gets the signature from among the set of valid signatures that is currently selected.
        /// </summary>
        ISignature SelectedSignature { get; set; }

        /// <summary>
        /// Occurs when the SelectedSignature property changes.
        /// </summary>
        event EventHandler<SelectedSignatureChangedEventArgs> SelectedSignatureChanged;
    }
}
