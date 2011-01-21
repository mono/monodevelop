//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10View.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A depth-stencil-view interface accesses a texture resource during depth-stencil testing.
    /// <para>(Also see DirectX SDK: ID3D10DepthStencilView)</para>
    /// </summary>
    public ref class DepthStencilView :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D10::View
    {
    public: 
        /// <summary>
        /// Get the depth-stencil view description.
        /// <para>(Also see DirectX SDK: ID3D10DepthStencilView::GetDesc)</para>
        /// </summary>
        property DepthStencilViewDescription Description
        {
            DepthStencilViewDescription get();
        }

    internal:

        DepthStencilView(void)
        { }

		DepthStencilView(ID3D10DepthStencilView* pNativeID3D10DepthStencilView) : View(pNativeID3D10DepthStencilView)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10DepthStencilView)); }
        }
    };
} } } }
