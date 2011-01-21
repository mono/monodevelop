//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectUnknown.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

    /// <summary>
    /// This class is used to return arbitrary length data.
    /// <para>(Also see DirectX SDK: ID3D10Blob)</para>
    /// </summary>
    public ref class Blob :
        public DirectUnknown
    {
    public: 
        /// <summary>
        /// Get the data.
        /// <para>(Also see DirectX SDK: ID3D10Blob::GetBufferPointer)</para>
        /// </summary>
        property IntPtr BufferPointer
        {
            IntPtr get(void);
        }

        /// <summary>
        /// Get the size.
        /// <para>(Also see DirectX SDK: ID3D10Blob::GetBufferSize)</para>
        /// </summary>
        property UInt32 BufferSize
        {
            UInt32 get(void);
        }

		/// <summary>
		/// Creates a Blob instance of the given size.
		/// </summary>
		/// <remarks>
		/// </remarks>
		static Blob^ Create(Int64 size);

    internal:
        Blob()
        { }

        Blob(ID3D10Blob* pNativeID3D10Blob) : 
            DirectUnknown(pNativeID3D10Blob)
        { }

    };
} } } }
