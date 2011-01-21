//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace  Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {
    ref class D3DDevice;
    ref class Effect;
    ref class ShaderResourceView;
}}}}

namespace  Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3DX10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

/// <summary>
/// Helper APIs for Direct3D10X
/// </summary>
public ref class D3D10XHelpers
{
protected:
    D3D10XHelpers(void) {}
public:

    static ShaderResourceView^ CreateShaderResourceViewFromFile(
        D3DDevice^ pDevice,
        String^ fileName);

    // Build a lookat matrix. (left-handed)
    static array<Single>^ MatrixLookAtLH
        ( array<Single>^ pEye, array<Single>^ pAt, array<Single>^ pUp );

    static array<Single>^ MatrixPerspectiveFovLH
        (Single fovy, Single Aspect, Single zn, Single zf );

    static array<Single>^ MatrixRotationX(Single value);
    static array<Single>^ MatrixRotationY(Single value);
    static array<Single>^ MatrixRotationZ(Single value);

	static array<Single>^ MatrixMultiply(array<Single>^ m1, array<Single>^ m2);

};
}}}}