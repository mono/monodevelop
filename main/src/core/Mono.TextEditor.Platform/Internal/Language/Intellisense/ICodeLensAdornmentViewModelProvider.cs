//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides view model for the adornments
    /// </summary>
    public interface ICodeLensAdornmentViewModelProvider
    {
        Task<ICodeLensAdornmentViewModel> CreateAdornmentViewModelAsync(ICodeLensDescriptor descriptor);
    }
}
