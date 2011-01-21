//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
namespace Microsoft.WindowsAPICodePack.Net
{
    /// <summary>
    /// Specifies types of network connectivity.
    /// </summary>    
    [Flags]
    public enum ConnectivityStates
    {
        /// <summary>
        /// The underlying network interfaces have no 
        /// connectivity to any network.
        /// </summary>
        None = 0,
        /// <summary>
        /// There is connectivity to the Internet 
        /// using the IPv4 protocol.
        /// </summary>        
        IPv4Internet = 0x40,
        /// <summary>
        /// There is connectivity to a routed network
        /// using the IPv4 protocol.
        /// </summary>        
        IPv4LocalNetwork = 0x20,
        /// <summary>
        /// There is connectivity to a network, but 
        /// the service cannot detect any IPv4 
        /// network traffic.
        /// </summary>
        IPv4NoTraffic = 1,
        /// <summary>
        /// There is connectivity to the local 
        /// subnet using the IPv4 protocol.
        /// </summary>
        IPv4Subnet = 0x10,
        /// <summary>
        /// There is connectivity to the Internet 
        /// using the IPv4 protocol.
        /// </summary>
        IPv6Internet = 0x400,
        /// <summary>
        /// There is connectivity to a local 
        /// network using the IPv6 protocol.
        /// </summary>
        IPv6LocalNetwork = 0x200,
        /// <summary>
        /// There is connectivity to a network, 
        /// but the service cannot detect any 
        /// IPv6 network traffic
        /// </summary>
        IPv6NoTraffic = 2,
        /// <summary>
        /// There is connectivity to the local 
        /// subnet using the IPv6 protocol.
        /// </summary>
        IPv6Subnet = 0x100
    }

    /// <summary>
    /// Specifies the domain type of a network.
    /// </summary>
    public enum DomainType
    {
        /// <summary>
        /// The network is not an Active Directory network.
        /// </summary>
        NonDomainNetwork = 0,
        /// <summary>
        /// The network is an Active Directory network, but this machine is not authenticated against it.
        /// </summary>
        DomainNetwork = 1,
        /// <summary>
        /// The network is an Active Directory network, and this machine is authenticated against it.
        /// </summary>
        DomainAuthenticated = 2,
    }

    /// <summary>
    /// Specifies the trust level for a 
    /// network.
    /// </summary>
    public enum NetworkCategory
    {
        /// <summary>
        /// The network is a public (untrusted) network. 
        /// </summary>
        Public,
        /// <summary>
        /// The network is a private (trusted) network. 
        /// </summary>
        Private,
        /// <summary>
        /// The network is authenticated against an Active Directory domain.
        /// </summary>
        Authenticated
    }

    /// <summary>
    /// Specifies the level of connectivity for 
    /// networks returned by the 
    /// <see cref="NetworkListManager"/> 
    /// class.
    /// </summary>
    [Flags]
    public enum NetworkConnectivityLevels
    {
        /// <summary>
        /// Networks that the machine is connected to.
        /// </summary>
        Connected = 1,
        /// <summary>
        /// Networks that the machine is not connected to.
        /// </summary>
        Disconnected = 2,
        /// <summary>
        /// All networks.
        /// </summary>
        All = 3,
    }


}
