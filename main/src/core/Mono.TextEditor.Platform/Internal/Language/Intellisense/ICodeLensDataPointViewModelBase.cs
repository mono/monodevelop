//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.ComponentModel;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public interface ICodeLensDataPointViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// The description of the data point
        /// </summary>
        string Descriptor { get; }

        /// <summary>
        /// Indicates if the datapoint has details, so the indicator should appear as a link.  
        /// This does not indicate that detailed information is immediately available, only that details can be obtained.
        /// </summary>
        bool HasDetails { get; }

        /// <summary>
        /// indicates if data is available for the datapoint
        /// </summary>
        /// <returns>Null indicates that the value has not yet been set.
        /// True indicates that valid data exists.
        /// False indicates that data has finished loading but that there is no data to return.</returns>
        bool? HasData { get; }

        /// <summary>
        /// indicates if text should be displayed for the datapoint
        /// </summary>
        bool IsVisible { get; }
    }
}
