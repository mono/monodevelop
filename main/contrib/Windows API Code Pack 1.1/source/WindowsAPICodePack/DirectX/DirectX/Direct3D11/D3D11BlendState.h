//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// This blend-state interface accesses blending state for the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D11BlendState)</para>
    /// </summary>
    public ref class BlendState :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::DeviceChild
    {
    public: 
        /// <summary>
        /// Get the blend state description.
        /// <para>(Also see DirectX SDK: ID3D11BlendState::GetDesc)</para>
        /// </summary>
        property BlendDescription Description
        {
            BlendDescription get();
        }

    internal:

        BlendState(void)
        { }

		BlendState(ID3D11BlendState* pNativeID3D11BlendState) : DeviceChild(pNativeID3D11BlendState)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11BlendState)); }
        }
    };
} } } }
