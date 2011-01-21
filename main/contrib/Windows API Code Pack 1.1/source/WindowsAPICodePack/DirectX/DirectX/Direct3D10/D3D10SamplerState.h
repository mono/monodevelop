//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A sampler-state interface accesses sampler state for a texture.
    /// <para>(Also see DirectX SDK: ID3D10SamplerState)</para>
    /// </summary>
    public ref class SamplerState :
        public DeviceChild
    {
    public: 
        /// <summary>
        /// Get the sampler state.
        /// <para>(Also see DirectX SDK: ID3D10SamplerState::GetDesc)</para>
        /// </summary>
        property SamplerDescription Description
        {
            SamplerDescription get();
        }

    internal:

        SamplerState(void)
        { }

        SamplerState(ID3D10SamplerState* pNativeID3D10SamplerState) : DeviceChild(pNativeID3D10SamplerState)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10SamplerState)); }
        }
    };
} } } }
