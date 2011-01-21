//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// A depth-stencil-state interface accesses depth-stencil state which sets up the depth-stencil test for the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D11DepthStencilState)</para>
    /// </summary>
    public ref class DepthStencilState : public DeviceChild
    {
    public: 
        /// <summary>
        /// Get the depth-stencil state.
        /// <para>(Also see DirectX SDK: ID3D11DepthStencilState::GetDesc)</para>
        /// </summary>
        property DepthStencilDescription Description
        {
            DepthStencilDescription get();
        }

    internal:
        DepthStencilState() : DeviceChild()
        { }

        DepthStencilState(ID3D11DepthStencilState* pNativeID3D11DepthStencilState) : DeviceChild(pNativeID3D11DepthStencilState)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11DepthStencilState)); }
        }
    };
} } } }
