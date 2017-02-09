//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// View model for data points that wraps an integer value
    /// </summary>
    public interface ICodeLensIntegerDataPointViewModel : ICodeLensDataPointViewModel
    {
        /// <summary>
        /// The value of the data point
        /// </summary>
        int? Value { get; }
    }
}
