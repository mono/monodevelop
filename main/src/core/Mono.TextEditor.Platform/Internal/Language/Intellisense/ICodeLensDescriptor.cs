//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public interface ICodeLensDescriptor
    {
        /// <summary>
        /// A short description of the element for this descriptor.
        /// </summary>
        string ElementDescription { get; }
    }
}
