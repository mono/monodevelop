//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// An input-layout interface accesses the input data for the input-assembler stage.
    /// <para>(Also see DirectX SDK: ID3D11InputLayout)</para>
    /// </summary>
    public ref class InputLayout :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::DeviceChild
    {
    internal:

        InputLayout(void)
        { }

		InputLayout(ID3D11InputLayout* pNativeID3D11InputLayout) : DeviceChild(pNativeID3D11InputLayout)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11InputLayout)); }
        }
    };
} } } }
