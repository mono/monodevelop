//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// A pixel-shader class manages an executable program (a pixel shader) that controls the pixel-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11PixelShader)</para>
    /// </summary>
    public ref class PixelShader :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::DeviceChild
    {
    internal:

        PixelShader(void)
        { }

		PixelShader(ID3D11PixelShader* pNativeID3D11PixelShader) : DeviceChild(pNativeID3D11PixelShader)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11PixelShader)); }
        }
    };
} } } }
