//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11View.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// A shader-resource-view interface specifies the subresources a shader can access during rendering. Examples of shader resources include a constant buffer, a texture buffer, a texture or a sampler.
    /// <para>(Also see DirectX SDK: ID3D11ShaderResourceView)</para>
    /// </summary>
    public ref class ShaderResourceView :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::View
    {
    public: 
        /// <summary>
        /// Get the shader resource view's description.
        /// <para>(Also see DirectX SDK: ID3D11ShaderResourceView::GetDesc)</para>
        /// </summary>
        property ShaderResourceViewDescription Description
        {
            ShaderResourceViewDescription get();
        }

    internal:

        ShaderResourceView(void)
        { }

		ShaderResourceView(ID3D11ShaderResourceView* pNativeID3D11ShaderResourceView) : View(pNativeID3D11ShaderResourceView)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11ShaderResourceView)); }
        }
    };
} } } }
