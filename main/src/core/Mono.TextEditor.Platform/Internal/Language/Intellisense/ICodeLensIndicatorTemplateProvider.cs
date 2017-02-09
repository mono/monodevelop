//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Windows;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides DataTemplates for a specific type of DataPoint
    /// </summary>
    public interface ICodeLensIndicatorTemplateProvider
    {
        /// <summary>
        /// Gets the data point template
        /// </summary>
        DataTemplate IndicatorTemplate { get; }
    }
}
