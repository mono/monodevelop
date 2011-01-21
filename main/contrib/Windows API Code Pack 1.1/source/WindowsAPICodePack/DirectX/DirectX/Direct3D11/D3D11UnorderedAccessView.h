//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11View.h"

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

    /// <summary>
    /// A view interface specifies the parts of a resource the pipeline can access during rendering.
    /// <para>(Also see DirectX SDK: ID3D11UnorderedAccessView)</para>
    /// </summary>
    public ref class UnorderedAccessView :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::View
    {
    public: 
        /// <summary>
        /// Get a description of the resource.
        /// <para>(Also see DirectX SDK: ID3D11UnorderedAccessView::GetDesc)</para>
        /// </summary>
        property UnorderedAccessViewDescription Description
        {
            UnorderedAccessViewDescription get();
        }

    internal:
        UnorderedAccessView() : View()
        { }

        UnorderedAccessView(ID3D11UnorderedAccessView* pNativeID3D11UnorderedAccessView) : View(pNativeID3D11UnorderedAccessView)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11UnorderedAccessView)); }
        }
    };

} } } }
