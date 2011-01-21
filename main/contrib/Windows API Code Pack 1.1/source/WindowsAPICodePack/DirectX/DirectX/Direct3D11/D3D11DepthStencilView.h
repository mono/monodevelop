//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11View.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// A depth-stencil-view interface accesses a texture resource during depth-stencil testing.
    /// <para>(Also see DirectX SDK: ID3D11DepthStencilView)</para>
    /// </summary>
    public ref class DepthStencilView :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::View
    {
    public: 
        /// <summary>
        /// Get the depth-stencil view.
        /// <para>(Also see DirectX SDK: ID3D11DepthStencilView::GetDesc)</para>
        /// </summary>
        property DepthStencilViewDescription Description
        {
            DepthStencilViewDescription get();
        }

    internal:

        DepthStencilView(void)
        { }

		DepthStencilView(ID3D11DepthStencilView* pNativeID3D11DepthStencilView) : View(pNativeID3D11DepthStencilView)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11DepthStencilView)); }
        }
    };
} } } }
