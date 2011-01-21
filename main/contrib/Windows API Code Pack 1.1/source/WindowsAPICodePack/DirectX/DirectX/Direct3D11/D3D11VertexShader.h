//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// A vertex-shader class manages an executable program (a vertex shader) that controls the vertex-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11VertexShader)</para>
    /// </summary>
    public ref class VertexShader :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::DeviceChild
    {
    public: 

    internal:

        VertexShader(void)
        { }

		VertexShader(ID3D11VertexShader* pNativeID3D11VertexShader) : DeviceChild(pNativeID3D11VertexShader)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11VertexShader)); }
        }
    };
} } } }
