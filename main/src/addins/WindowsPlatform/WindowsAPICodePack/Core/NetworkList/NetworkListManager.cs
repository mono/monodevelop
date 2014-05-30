//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Net
{
    /// <summary>
    /// Provides access to objects that represent networks and network connections.
    /// </summary>
    public static class NetworkListManager
    {
        #region Private Fields

        static NetworkListManagerClass manager = new NetworkListManagerClass();

        #endregion // Private Fields

        /// <summary>
        /// Retrieves a collection of <see cref="Network"/> objects that represent the networks defined for this machine.
        /// </summary>
        /// <param name="level">
        /// The <see cref="NetworkConnectivityLevels"/> that specify the connectivity level of the returned <see cref="Network"/> objects.
        /// </param>
        /// <returns>
        /// A <see cref="NetworkCollection"/> of <see cref="Network"/> objects.
        /// </returns>
        public static NetworkCollection GetNetworks(NetworkConnectivityLevels level)
        {
            // Throw PlatformNotSupportedException if the user is not running Vista or beyond
            CoreHelpers.ThrowIfNotVista();

            return new NetworkCollection(manager.GetNetworks(level));
        }

        /// <summary>
        /// Retrieves the <see cref="Network"/> identified by the specified network identifier.
        /// </summary>
        /// <param name="networkId">
        /// A <see cref="System.Guid"/> that specifies the unique identifier for the network.
        /// </param>
        /// <returns>
        /// The <see cref="Network"/> that represents the network identified by the identifier.
        /// </returns>
        public static Network GetNetwork(Guid networkId)
        {
            // Throw PlatformNotSupportedException if the user is not running Vista or beyond
            CoreHelpers.ThrowIfNotVista();

            return new Network(manager.GetNetwork(networkId));
        }

        /// <summary>
        /// Retrieves a collection of <see cref="NetworkConnection"/> objects that represent the connections for this machine.
        /// </summary>
        /// <returns>
        /// A <see cref="NetworkConnectionCollection"/> containing the network connections.
        /// </returns>
        public static NetworkConnectionCollection GetNetworkConnections()
        {
            // Throw PlatformNotSupportedException if the user is not running Vista or beyond
            CoreHelpers.ThrowIfNotVista();

            return new NetworkConnectionCollection(manager.GetNetworkConnections());
        }

        /// <summary>
        /// Retrieves the <see cref="NetworkConnection"/> identified by the specified connection identifier.
        /// </summary>
        /// <param name="networkConnectionId">
        /// A <see cref="System.Guid"/> that specifies the unique identifier for the network connection.
        /// </param>
        /// <returns>
        /// The <see cref="NetworkConnection"/> identified by the specified identifier.
        /// </returns>
        public static NetworkConnection GetNetworkConnection(Guid networkConnectionId)
        {
            // Throw PlatformNotSupportedException if the user is not running Vista or beyond
            CoreHelpers.ThrowIfNotVista();

            return new NetworkConnection(manager.GetNetworkConnection(networkConnectionId));
        }

        /// <summary>
        /// Gets a value that indicates whether this machine 
        /// has Internet connectivity.
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public static bool IsConnectedToInternet
        {
            get
            {
                // Throw PlatformNotSupportedException if the user is not running Vista or beyond
                CoreHelpers.ThrowIfNotVista();

                return manager.IsConnectedToInternet;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether this machine 
        /// has network connectivity.
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public static bool IsConnected
        {
            get
            {
                // Throw PlatformNotSupportedException if the user is not running Vista or beyond
                CoreHelpers.ThrowIfNotVista();

                return manager.IsConnected;
            }
        }

        /// <summary>
        /// Gets the connectivity state of this machine.
        /// </summary>
        /// <value>A <see cref="Connectivity"/> value.</value>
        public static ConnectivityStates Connectivity
        {
            get
            {
                // Throw PlatformNotSupportedException if the user is not running Vista or beyond
                CoreHelpers.ThrowIfNotVista();

                return manager.GetConnectivity();
            }
        }
    }

}