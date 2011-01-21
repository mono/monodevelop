//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10Asynchronous.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A query interface queries information from the GPU.
    /// <para>(Also see DirectX SDK: ID3D10Query)</para>
    /// </summary>
    public ref class D3DQuery :
        public Asynchronous
    {
    public: 
        /// <summary>
        /// Get a query description.
        /// <para>(Also see DirectX SDK: ID3D10Query::GetDesc)</para>
        /// </summary>
        property QueryDescription Description
        {
            QueryDescription get();
        }

    internal:
        D3DQuery()
        { }

        D3DQuery(ID3D10Query* pNativeID3D10Query) : Asynchronous(pNativeID3D10Query)
        { }

    };
} } } }
