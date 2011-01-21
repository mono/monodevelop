//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A rasterizer-state interface accesses rasterizer state for the rasterizer stage.
    /// <para>(Also see DirectX SDK: ID3D10RasterizerState)</para>
    /// </summary>
    public ref class RasterizerState :
        public DeviceChild
    {
    public: 
        /// <summary>
        /// Get the properties of a rasterizer-state object.
        /// <para>(Also see DirectX SDK: ID3D10RasterizerState::GetDesc)</para>
        /// </summary>
        property RasterizerDescription Description
        {
            RasterizerDescription get();
        }

    internal:
        RasterizerState()
        { }

        RasterizerState(ID3D10RasterizerState* pNativeID3D10RasterizerState) : DeviceChild(pNativeID3D10RasterizerState)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10RasterizerState)); }
        }
    };
} } } }
