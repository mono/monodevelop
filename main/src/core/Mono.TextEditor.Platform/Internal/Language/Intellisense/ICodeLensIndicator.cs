//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a single indicator in the UI.  The indicator
    /// can either be connected to a view model (in which case its data is
    /// kept up-to-date) or disconnected (in which case its data is cached).
    /// </summary>
    public interface ICodeLensIndicator : ICodeLensDataPointViewModelBase
    {
        /// <summary>
        /// Gets the descriptor used to create this indicator.
        /// </summary>
        ICodeLensDescriptor CodeLensDescriptor { get; }

        /// <summary>
        /// Gets the name of the data point provider used to
        /// create this indicator.
        /// </summary>
        string DataPointProviderName { get; }

        /// <summary>
        /// Gets the type of view model associated with this indicator.
        /// </summary>
        Type ViewModelType { get; }

        /// <summary>
        /// Gets the view model currently associated with this indicator,
        /// if the indicator is connected.
        /// </summary>
        ICodeLensDataPointViewModel ViewModel { get; }

        /// <summary>
        /// Gets whether or not this indicator is currently connected to a view model.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Connects this indicator to a view model if it's currently disconnected.
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnects this indicator from its view model if it's currently connnected.
        /// </summary>
        void Disconnect();
    }
}
