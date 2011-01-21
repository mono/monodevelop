//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// This blend-state interface accesses blending state for a Direct3D 10.0 device for the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D10BlendState)</para>
    /// </summary>
    public ref class BlendState :
        public DeviceChild
    {
    public: 
        /// <summary>
        /// Get the blend state description.
        /// <para>(Also see DirectX SDK: ID3D10BlendState::GetDesc)</para>
        /// </summary>
        property BlendDescription Description
        {
            BlendDescription get();
        }

    internal:
        BlendState()
        { }

        BlendState(ID3D10BlendState* pNativeID3D10BlendState) : 
            DeviceChild(pNativeID3D10BlendState)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10BlendState)); }
        }
    };
} } } }
