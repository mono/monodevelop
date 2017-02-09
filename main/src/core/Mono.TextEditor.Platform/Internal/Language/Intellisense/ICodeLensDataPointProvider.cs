//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a factory which can create <see cref="ICodeLensDataPoint"/> instances from an
    /// <see cref="ICodeLensDescriptor"/>.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attributes:
    /// [Export(typeof(ICodeLensDataPointProvider))]
    /// [Name]
    /// 
    /// The [Order] attribute is optional, and can be used to order visualizations of this provider's
    /// data points relative to other providers.
    /// </remarks>
    public interface ICodeLensDataPointProvider
    {
        /// <summary>
        /// Determines if this provider supports the descriptor given
        /// </summary>
        /// <param name="descriptor">The descriptor to check</param>
        /// <returns>True if a data point can be created from this descriptor, false otherwise</returns>
        bool CanCreateDataPoint(ICodeLensDescriptor descriptor);

        /// <summary>
        /// Creates a data point from a given descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor to use</param>
        /// <returns>A data point created from the descriptor</returns>
        ICodeLensDataPoint CreateDataPoint(ICodeLensDescriptor descriptor);
    }
}