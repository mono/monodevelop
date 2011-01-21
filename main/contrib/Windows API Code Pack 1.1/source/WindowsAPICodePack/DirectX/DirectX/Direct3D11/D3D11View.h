//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;
ref class D3DResource;

    /// <summary>
    /// A view specifies the parts of a resource the pipeline can access during rendering.
    /// <para>(Also see DirectX SDK: ID3D11View)</para>
    /// </summary>
    public ref class View :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::DeviceChild
    {
    public: 
        /// <summary>
        /// Get the resource that is accessed through this view.
        /// <para>(Also see DirectX SDK: ID3D11View::GetResource)</para>
        /// </summary>
        property D3DResource^ Resource
        {
            D3DResource^ get(void);
        }

    internal:
        View() : DeviceChild()
        { }

        View(ID3D11View* pNativeID3D11View) : DeviceChild(pNativeID3D11View)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11View)); }
        }
    };
} } } }
