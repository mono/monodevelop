// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Net;
using Xunit;
using Xunit.Extensions;

namespace Tests
{
    public class NetworkListManagerTests
    {
        [Fact]
        public void NetworkCollectionContainsAllNetworkConnections()
        {
            bool isConnected = NetworkListManager.IsConnected;
            ConnectivityStates connectivity = NetworkListManager.Connectivity;
            bool isConnectedToInternet = NetworkListManager.IsConnectedToInternet;

            NetworkCollection networks = NetworkListManager.GetNetworks(NetworkConnectivityLevels.All);
            NetworkConnectionCollection connections = NetworkListManager.GetNetworkConnections();

            // BUG: Both GetNetworks and GetNetworkConnections create new network objects, so 
            // you can't do a reference comparison.
            // By inspection, the connections are contained in the NetworkCollection, just a different instance.
            foreach (NetworkConnection c in connections)
            {
                Assert.Contains(c.Network, networks);
            }
        }

        [Fact]
        public void IsConnectedIsConsistentWithGetNetworkConnections()
        {
            bool isConnected = NetworkListManager.IsConnected;
            bool moreThanZeroConnections = false;

            foreach (NetworkConnection c in NetworkListManager.GetNetworkConnections())
            {
                moreThanZeroConnections = true;
                break;
            }

            Assert.Equal(isConnected, moreThanZeroConnections);
        }
    }
}
