//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.Net
{
    [ComImport, ClassInterface((short)0), Guid("DCB00C01-570F-4A9B-8D69-199FDBA5723B")]
    [ComSourceInterfaces("Microsoft.Windows.NetworkList.Internal.INetworkEvents\0Microsoft.Windows.NetworkList.Internal.INetworkConnectionEvents\0Microsoft.Windows.NetworkList.Internal.INetworkListManagerEvents\0"), TypeLibType((short)2)]
    internal class NetworkListManagerClass : INetworkListManager
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(7)]
        public virtual extern ConnectivityStates GetConnectivity();

        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)]
        public virtual extern INetwork GetNetwork([In] Guid gdNetworkId);

        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)]
        public virtual extern INetworkConnection GetNetworkConnection([In] Guid gdNetworkConnectionId);

        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)]
        public virtual extern IEnumerable GetNetworkConnections();

        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)]
        public virtual extern IEnumerable GetNetworks([In] NetworkConnectivityLevels Flags);

        [DispId(6)]
        public virtual extern bool IsConnected
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(6)]
            get;
        }

        [DispId(5)]
        public virtual extern bool IsConnectedToInternet
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)]
            get;
        }
    }
}