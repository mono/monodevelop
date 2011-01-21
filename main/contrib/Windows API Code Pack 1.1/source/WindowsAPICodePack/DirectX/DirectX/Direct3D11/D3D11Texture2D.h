//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11Resource.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// A 2D texture interface manages texel data, which is structured memory.
    /// <para>(Also see DirectX SDK: ID3D11Texture2D)</para>
    /// </summary>
    public ref class Texture2D :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::D3DResource
    {
    public: 
        /// <summary>
        /// Get the properties of the texture resource.
        /// <para>(Also see DirectX SDK: ID3D11Texture2D::GetDesc)</para>
        /// </summary>
        property Texture2DDescription Description
        {
            Texture2DDescription get();
        }


    internal:

        Texture2D(void)
        { }

		Texture2D(ID3D11Texture2D* pNativeID3D11Texture2D) : D3DResource(pNativeID3D11Texture2D)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11Texture2D)); }
        }
    };
} } } }
