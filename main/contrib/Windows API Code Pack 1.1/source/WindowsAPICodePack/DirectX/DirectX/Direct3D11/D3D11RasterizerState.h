//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// A rasterizer-state interface accesses rasterizer state for the rasterizer stage.
    /// <para>(Also see DirectX SDK: ID3D11RasterizerState)</para>
    /// </summary>
    public ref class RasterizerState :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::DeviceChild
    {
    public: 
        /// <summary>
        /// Get the properties of a rasterizer-state object.
        /// <para>(Also see DirectX SDK: ID3D11RasterizerState::GetDesc)</para>
        /// </summary>
        property RasterizerDescription Description
        {
            RasterizerDescription get();
        }

    internal:

        RasterizerState(void)
        { }

		RasterizerState(ID3D11RasterizerState* pNativeID3D11RasterizerState) : DeviceChild(pNativeID3D11RasterizerState)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11RasterizerState)); }
        }
    };
} } } }
