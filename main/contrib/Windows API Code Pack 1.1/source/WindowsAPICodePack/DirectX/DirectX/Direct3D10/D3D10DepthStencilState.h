//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A depth-stencil-state interface accesses depth-stencil state which sets up the depth-stencil test for the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D10DepthStencilState)</para>
    /// </summary>
    public ref class DepthStencilState :
        public DeviceChild
    {
    public: 
        /// <summary>
        /// Get the depth-stencil state.
        /// <para>(Also see DirectX SDK: ID3D10DepthStencilState::GetDesc)</para>
        /// </summary>
        property DepthStencilDescription Description
        {
            DepthStencilDescription get();
        }

    internal:
        DepthStencilState()
        { }

        DepthStencilState(ID3D10DepthStencilState* pNativeID3D10DepthStencilState) : DeviceChild(pNativeID3D10DepthStencilState)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10DepthStencilState)); }
        }
    };
} } } }
