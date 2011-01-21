//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11Resource.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// A 1D texture interface accesses texel data, which is structured memory.
    /// <para>(Also see DirectX SDK: ID3D11Texture1D)</para>
    /// </summary>
    public ref class Texture1D :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::D3DResource
    {
    public: 
        /// <summary>
        /// Get the properties of the texture resource.
        /// <para>(Also see DirectX SDK: ID3D11Texture1D::GetDesc)</para>
        /// </summary>
        property Texture1DDescription Description
        {
            Texture1DDescription get();
        }

    internal:

        Texture1D(void)
        { }

		Texture1D(ID3D11Texture1D* pNativeID3D11Texture1D) : D3DResource(pNativeID3D11Texture1D)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11Texture1D)); }
        }
    };
} } } }
