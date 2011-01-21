//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11Asynchronous.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// A query interface queries information from the GPU.
    /// <para>(Also see DirectX SDK: ID3D11Query)</para>
    /// </summary>
    public ref class D3DQuery :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::Asynchronous
    {
    public: 
        /// <summary>
        /// Get a query description.
        /// <para>(Also see DirectX SDK: ID3D11Query::GetDesc)</para>
        /// </summary>
        property QueryDescription Description
        {
            QueryDescription get();
        }

    internal:

        D3DQuery(void)
        { }

		D3DQuery(ID3D11Query* pNativeID3D11Query) : Asynchronous(pNativeID3D11Query)
        { }
    };
} } } }
