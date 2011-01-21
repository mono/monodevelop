//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// This class encapsulates methods for retrieving data from the GPU asynchronously.
    /// <para>(Also see DirectX SDK: ID3D10Asynchronous)</para>
    /// </summary>
    public ref class Asynchronous :
        public DeviceChild
    {
    public: 
        /// <summary>
        /// Starts the collection of GPU data.
        /// <para>(Also see DirectX SDK: ID3D10Asynchronous::Begin)</para>
        /// </summary>
        void Begin();

        /// <summary>
        /// Ends the collection of GPU data.
        /// <para>(Also see DirectX SDK: ID3D10Asynchronous::End)</para>
        /// </summary>
        void End();

        /// <summary>
        /// Get data from the GPU asynchronously.
        /// <para>(Also see DirectX SDK: ID3D10Asynchronous::GetData)</para>
        /// </summary>
        /// <param name="data">Address of memory that will receive the data. If NULL, GetData will be used only to check status. The type of data output depends on the type of asynchronous object. See Remarks.</param>
        /// <param name="dataSize">Size of the data to retrieve or 0. This value can be obtained with Asynchronous.GetDataSize. Must be 0 when pData is NULL.</param>
        /// <param name="options">Optional flags. Can be 0 or any combination of the flags enumerated by AsyncGetDataOptions.</param>
        void GetData(IntPtr data, UInt32 dataSize, AsyncGetDataOptions options);
        /// <summary>
        /// Get the size of the data (in bytes) that is output when calling Asynchronous.GetData.
        /// <para>(Also see DirectX SDK: ID3D10Asynchronous::GetDataSize)</para>
        /// </summary>
        property UInt32 DataSize
        {
            UInt32 get(void);
        }

    internal:
        Asynchronous()
        { }

        Asynchronous(ID3D10Asynchronous* pNativeID3D10Asynchronous) : 
            DeviceChild(pNativeID3D10Asynchronous)
        { }
    };
} } } }
