//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10Asynchronous.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// This class encapsulates methods for measuring GPU performance.
    /// <para>(Also see DirectX SDK: ID3D10Counter)</para>
    /// </summary>
    public ref class D3DCounter :
        public Asynchronous
    {
    public: 
        /// <summary>
        /// Get a counter description.
        /// <para>(Also see DirectX SDK: ID3D10Counter::GetDesc)</para>
        /// </summary>
        property CounterDescription Description
        {
            CounterDescription get();
        }

    internal:
        D3DCounter()
        { }

        D3DCounter(ID3D10Counter* pNativeID3D10Counter) : Asynchronous(pNativeID3D10Counter)
        { }
    };
} } } }
