//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Windows;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides DetailsTemplates for a specific type of DataPoint
    /// </summary>
    public interface ICodeLensDetailsTemplateProvider
    {
        /// <summary>
        /// Gets the details template
        /// </summary>
        DataTemplate DetailsTemplate { get; }
    }
}
