//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11Asynchronous.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// This class encapsulates methods for measuring GPU performance.
    /// <para>(Also see DirectX SDK: ID3D11Counter)</para>
    /// </summary>
    public ref class D3DCounter :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::Asynchronous
    {
    public: 
        /// <summary>
        /// Get the counter description.
        /// <para>(Also see DirectX SDK: ID3D11Counter::GetDesc)</para>
        /// </summary>
        property CounterDescription Description
        {
            CounterDescription get();
        }

    internal:

        D3DCounter(void)
        { }

		D3DCounter(ID3D11Counter* pNativeID3D11Counter) : Asynchronous(pNativeID3D11Counter)
        { }
    };
} } } }
