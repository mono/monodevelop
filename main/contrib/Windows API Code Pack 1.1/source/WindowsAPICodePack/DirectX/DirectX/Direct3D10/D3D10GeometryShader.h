//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A geometry-shader interface manages an executable program (a geometry shader) that controls the geometry-shader stage.
    /// <para>(Also see DirectX SDK: ID3D10GeometryShader)</para>
    /// </summary>
    public ref class GeometryShader :
        public DeviceChild
    {
    public: 

    internal:
        GeometryShader()
        { }

        GeometryShader(ID3D10GeometryShader* pNativeID3D10GeometryShader) : DeviceChild(pNativeID3D10GeometryShader)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10GeometryShader)); }
        }
    };
} } } }
