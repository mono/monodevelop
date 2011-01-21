//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A pixel-shader interface manages an executable program (a pixel shader) that controls the pixel-shader stage.
    /// <para>(Also see DirectX SDK: ID3D10PixelShader)</para>
    /// </summary>
    public ref class PixelShader :
        public DeviceChild
    {
    public: 

    internal:
        PixelShader()
        { }

        PixelShader(ID3D10PixelShader* pNativeID3D10PixelShader) : DeviceChild(pNativeID3D10PixelShader)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10PixelShader)); }
        }
    };
} } } }
