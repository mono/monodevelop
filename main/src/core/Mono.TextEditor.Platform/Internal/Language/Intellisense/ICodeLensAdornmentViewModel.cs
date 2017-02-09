//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// View model for the adornment
    /// </summary>
    public interface ICodeLensAdornmentViewModel
    {
        /// <summary>
        /// Gets the collection of indicators presented in this adornment.
        /// </summary>
        ICodeLensIndicatorCollection Indicators { get; }

        /// <summary>
        /// Gets whether or not the adornment is currently connected to
        /// live data.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Disconnects the adornment, if it's currently connected.
        /// Disconnecting the adornment will disconnect each indicator, created
        /// cached data.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Connects the adornment, if it's currently disconnected.  Connecting
        /// the adornment will hook each indicator up to a live view model.
        /// </summary>
        Task ConnectAsync();
    }
}
