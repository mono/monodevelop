//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A vertex-shader interface manages an executable program (a vertex shader) that controls the vertex-shader stage.
    /// <para>(Also see DirectX SDK: ID3D10VertexShader)</para>
    /// </summary>
    public ref class VertexShader :
        public DeviceChild
    {
    public: 

    internal:
        VertexShader()
        { }

        VertexShader(ID3D10VertexShader* pNativeID3D10VertexShader) : DeviceChild(pNativeID3D10VertexShader)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10VertexShader)); }
        }
    };
} } } }
