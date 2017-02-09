//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.ComponentModel;
using System.Windows.Input;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// A simple ViewModel for datapoints that provides a descriptor
    /// </summary>
    public interface ICodeLensDataPointViewModel : ICodeLensDataPointViewModelBase
    {
        /// <summary>
        /// Gets the ICodeLensDataPoint used to create this view model.
        /// </summary>
        ICodeLensDataPoint DataPoint { get; }

        /// <summary>
        /// indicates if the datapoint is actively loading detailed data
        /// </summary>
        bool IsLoadingDetails { get; }

        /// <summary>
        /// contains any failure information if details were attempted to be loaded but failed.
        /// </summary>
        string DetailsFailureInfo { get; }

        /// <summary>
        /// a Command that can be used to refresh this viewmodel
        /// </summary>
        ICommand RefreshCommand { get; }

        /// <summary>
        /// Gets the additional information to be shown about the data point (for example, in a tooltip).
        /// </summary>
        string AdditionalInformation { get; }
    }

    /// <summary>
    /// Interface for DataPointViewModel provider
    /// </summary>
    /// <remarks>This is not intended to be used externally</remarks>
    public interface ICodeLensDataPointViewModelProvider
    {
        /// <summary>
        /// Gets view model for given data point
        /// </summary>
        /// <param name="dataPoint">Data point provided</param>
        /// <returns>Appropriate view model for the data point type</returns>
        ICodeLensDataPointViewModel GetViewModel(ICodeLensDataPoint dataPoint);
    }
}