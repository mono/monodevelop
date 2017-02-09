//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a single piece of data in CodeLens
    /// </summary>
    public interface ICodeLensDataPoint
    {
        event EventHandler Invalidated;

        Task<object> GetDataAsync();
    }
}
