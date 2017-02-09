//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a disposable reference to an ICodeLensDataPointViewModel.  It's possible
    /// that the view model is shared between multiple sources, and only disposing all references
    /// will release the underlying view model.
    /// </summary>
    public interface ICodeLensDataPointViewModelReference : IDisposable
    {
        /// <summary>
        /// Gets the view model referenced by this object.  This view model
        /// is kept alive as long as this object is not disposed.
        /// </summary>
        ICodeLensDataPointViewModel ViewModel { get; }
    }
}
