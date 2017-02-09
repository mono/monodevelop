//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Base class for data template providers.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [ContentProperty("IndicatorTemplate")]
    public abstract class CodeLensIndicatorTemplateProvider : ICodeLensIndicatorTemplateProvider
    {
        public DataTemplate IndicatorTemplate { get; protected set; }
    }
}
