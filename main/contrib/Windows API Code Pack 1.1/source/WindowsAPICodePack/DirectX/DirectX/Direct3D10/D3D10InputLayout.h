//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// An input-layout interface accesses the input data for the input-assembler stage.
    /// <para>(Also see DirectX SDK: ID3D10InputLayout)</para>
    /// </summary>
    public ref class InputLayout :
        public DeviceChild
    {

    internal:
        InputLayout()
        { }

        InputLayout(ID3D10InputLayout* pNativeID3D10InputLayout) : DeviceChild(pNativeID3D10InputLayout)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10InputLayout)); }
        }
    };
} } } }
