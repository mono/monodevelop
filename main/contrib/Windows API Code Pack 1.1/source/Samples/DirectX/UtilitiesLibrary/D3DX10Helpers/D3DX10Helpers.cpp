// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "D3DX10Helpers.h"

using namespace msclr::interop;

using namespace System;
using namespace System::Runtime::InteropServices;

using namespace Microsoft::WindowsAPICodePack::DirectX;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3DX10;


ShaderResourceView^ D3D10XHelpers::CreateShaderResourceViewFromFile(
        D3DDevice^ pDevice,
        String^ fileName)
{
    ID3D10ShaderResourceView* view = NULL;

    pin_ptr<const wchar_t> name = PtrToStringChars( fileName );    
    
    HRESULT hr = 
        D3DX10CreateShaderResourceViewFromFile( 
        reinterpret_cast<ID3D10Device*>(pDevice->NativeInterface.ToPointer()), 
        name, NULL, NULL, &view, NULL );

    if (!SUCCEEDED(hr))
    {
        throw DirectHelpers::GetExceptionForHresult(static_cast<int>(hr));
    }

    return view ? DirectHelpers::CreateIUnknownWrapper<ShaderResourceView^>(IntPtr(view)) : nullptr;
}

// Build a lookat matrix. (left-handed)
array<Single>^ D3D10XHelpers::MatrixLookAtLH
    ( array<Single>^ pEye, array<Single>^ pAt, array<Single>^ pUp )
{
    pin_ptr<FLOAT> eye = &pEye[0];
    pin_ptr<FLOAT> at = &pAt[0];
    pin_ptr<FLOAT> up = &pUp[0];

    array<Single>^ outMatrix = gcnew array<Single>(16);
    pin_ptr<FLOAT> m = &outMatrix[0];
    D3DXMatrixLookAtLH((D3DXMATRIX*) m, (D3DXVECTOR3 *)eye, (D3DXVECTOR3 *)at, (D3DXVECTOR3 *)up);
    return outMatrix;
}

array<Single>^ D3D10XHelpers::MatrixPerspectiveFovLH
        (Single fovy, Single Aspect, Single zn, Single zf )
{
    array<Single>^ outMatrix = gcnew array<Single>(16);
    pin_ptr<FLOAT> m = &outMatrix[0];
    D3DXMatrixPerspectiveFovLH((D3DXMATRIX*) m, safe_cast<FLOAT>(fovy), safe_cast<FLOAT>(Aspect), safe_cast<FLOAT>(zn), safe_cast<FLOAT>(zf));
    return outMatrix;
}

array<Single>^  D3D10XHelpers::MatrixRotationX(Single value)
{
    array<Single>^ outMatrix = gcnew array<Single>(16);
    pin_ptr<FLOAT> m = &outMatrix[0];
    D3DXMatrixRotationX((D3DXMATRIX*) m, safe_cast<FLOAT>(value));
    return outMatrix;
}
array<Single>^  D3D10XHelpers::MatrixRotationY(Single value)
{
    array<Single>^ outMatrix = gcnew array<Single>(16);
    pin_ptr<FLOAT> m = &outMatrix[0];
    D3DXMatrixRotationY((D3DXMATRIX*) m, safe_cast<FLOAT>(value));
    return outMatrix;
}
array<Single>^  D3D10XHelpers::MatrixRotationZ(Single value)
{
    array<Single>^ outMatrix = gcnew array<Single>(16);
    pin_ptr<FLOAT> m = &outMatrix[0];
    D3DXMatrixRotationZ((D3DXMATRIX*) m, safe_cast<FLOAT>(value));
    return outMatrix;
}

array<Single>^ D3D10XHelpers::MatrixMultiply(array<Single>^ m1, array<Single>^ m2)
{
    array<Single>^ outMatrix = gcnew array<Single>(16);
    pin_ptr<FLOAT> m = &outMatrix[0];

    pin_ptr<FLOAT> m1_in = &m1[0];
    pin_ptr<FLOAT> m2_in = &m2[0];

    D3DXMatrixMultiply((D3DXMATRIX*) m, (D3DXMATRIX*) m1_in,  (D3DXMATRIX*) m2_in);

    return outMatrix;
}
