//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// CodeLens data provider aggregator.
    /// </summary>
    public interface ICodeLensIndicatorService
    {
        /// <summary>
        /// Get the data for a particular CodeLens descriptor from all of the providers that support that descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor describing what particular interesting document specific information is being asked for.</param>
        /// <returns>An enumerable of indicators, each of which contains unique a single data point</returns>
        Task<ICodeLensIndicatorCollection> CreateIndicatorCollectionAsync(ICodeLensDescriptor descriptor);

        /// <summary>
        /// Gets the index of the given data point provider within the ordered list of
        /// all providers.
        /// </summary>
        /// <param name="dataPointProviderName">The identifier of the data point provider
        /// to find the index of.</param>
        /// <returns>The index of the provider within the ordered list of providers,
        /// or -1 if the provider name does not match any known provider.</returns>
        int GetDataProviderIndex(string dataPointProviderName);

        /// <summary>
        /// Gets a bindable source for access keys for the given data point provider.
        /// </summary>
        /// <param name="dataPointProviderName">The identifier of the data point provider
        /// to find the access key source for.</param>
        /// <returns>The access key source for the data point provider, or null if the provider
        /// requested does not exist.</returns>
        ICodeLensAccessKeySource GetAccessKeySource(string dataPointProviderName);

        /// <summary>
        /// Get a particular CodeLensIndicator given the descriptor and the provider name
        /// </summary>
        /// <param name="descriptor">The descriptor describing what particular interesting document specific information is being asked for.</param>
        /// <param name="dataPointProviderName">The specific data point provider name</param>
        /// <param name="localizedName">The localized name of the provider</param>
        /// <returns>The indicator corresponding descriptor and the data point provider</returns>
        ICodeLensIndicator CreateIndicator(ICodeLensDescriptor descriptor, string dataPointProviderName, out string localizedName);
    }
}
