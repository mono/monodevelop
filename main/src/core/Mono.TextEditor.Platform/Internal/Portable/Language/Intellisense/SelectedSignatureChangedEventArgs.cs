////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides information about selected signature changes in signature help IntelliSense sessions.
    /// </summary>
    public class SelectedSignatureChangedEventArgs : EventArgs
    {
        private ISignature _prevSelectedSignature;
        private ISignature _newSelectedSignature;

        /// <summary>
        /// Initializes a new instance of <see cref="SelectedSignatureChangedEventArgs"/>.
        /// </summary>
        /// <param name="previousSelectedSignature">The signature that was previously selected.</param>
        /// <param name="newSelectedSignature">The signature that is currently selected.</param>
        public SelectedSignatureChangedEventArgs(ISignature previousSelectedSignature, ISignature newSelectedSignature)
        {
            _prevSelectedSignature = previousSelectedSignature;
            _newSelectedSignature = newSelectedSignature;
        }

        /// <summary>
        /// Gets the signature that was previously selected.
        /// </summary>
        public ISignature PreviousSelectedSignature
        {
            get { return _prevSelectedSignature; }
        }

        /// <summary>
        /// Gets the signature that is currently selected.
        /// </summary>
        public ISignature NewSelectedSignature
        {
            get { return _newSelectedSignature; }
        }
    }
}
