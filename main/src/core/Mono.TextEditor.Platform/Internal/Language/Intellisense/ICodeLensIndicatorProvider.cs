//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public interface ICodeLensIndicatorProvider
    {
        ICodeLensIndicator CreateIndicator(ICodeLensDescriptor descriptor, string dataPointProviderName, ICodeLensDataPointViewModelReference dataPointViewModelReference, Func<ICodeLensDataPointViewModelReference> viewModelReferenceFactory);
    }
}
