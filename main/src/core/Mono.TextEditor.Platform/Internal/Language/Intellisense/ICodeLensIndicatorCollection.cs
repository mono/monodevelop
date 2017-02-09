//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an observable collection of indicators.
    /// </summary>
    public interface ICodeLensIndicatorCollection : IReadOnlyList<ICodeLensIndicator>, INotifyCollectionChanged
    {
        /// <summary>
        /// Gets the descriptor associated with this collection of indicators.
        /// </summary>
        ICodeLensDescriptor Descriptor { get; }

        /// <summary>
        /// Gets whether or not this indicator collection is connected to live data.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Disconnects all of the indicators in this collection if they're currently connected.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Connects all of the indicators in this collection if they're currently disconnected.
        /// </summary>
        Task ConnectAsync();
    }
}
