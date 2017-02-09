//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Text.Tagging;
using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Justification = "Interface is empty")]
    public interface ICodeLensTag : ITag
    {
        ICodeLensDescriptor Descriptor { get; }

        /// <summary>
        /// Event raised when this tag has been disconnected and is no longer used as part of the editor.
        /// </summary>
        event EventHandler Disconnected;
    }
}
