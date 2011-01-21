//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10ShaderResourceView.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A shader-resource-view interface specifies the subresources a shader can access during rendering. Examples of shader resources include a constant buffer, a texture buffer, a texture or a sampler.
    /// <para>(Also see DirectX SDK: ID3D10ShaderResourceView1)</para>
    /// </summary>
    public ref class ShaderResourceView1 :
        public ShaderResourceView
    {
    public: 
        /// <summary>
        /// Get the shader resource view's description.
        /// <para>(Also see DirectX SDK: ID3D10ShaderResourceView1::GetDesc1)</para>
        /// </summary>
        property ShaderResourceViewDescription1 Description1
        {
            ShaderResourceViewDescription1 get();
        }
    internal:
        ShaderResourceView1()
        { }

        ShaderResourceView1(ID3D10ShaderResourceView1* pNativeID3D10ShaderResourceView1) : ShaderResourceView(pNativeID3D10ShaderResourceView1)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10ShaderResourceView1)); }
        }
    };
} } } }
