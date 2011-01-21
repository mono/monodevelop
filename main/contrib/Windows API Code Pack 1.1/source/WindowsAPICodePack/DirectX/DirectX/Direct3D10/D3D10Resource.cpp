// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Resource.h"
#include "DXGI/DXGISurface.h"
#include "DXGI/DXGISurface1.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

UInt32 D3DResource::EvictionPriority::get()
{
    return safe_cast<UInt32>(CastInterface<ID3D10Resource>()->GetEvictionPriority());
}

ResourceDimension D3DResource::ResourceDimension::get()
{
    D3D10_RESOURCE_DIMENSION tempoutrType;
    CastInterface<ID3D10Resource>()->GetType(&tempoutrType);
    return safe_cast<Direct3D10::ResourceDimension>(tempoutrType);
}

void D3DResource::EvictionPriority::set(UInt32 value)
{
    CastInterface<ID3D10Resource>()->SetEvictionPriority(static_cast<UINT>(value));
}

// Get associated Graphics surface. If none is associated - throw an InvalidCastException.
Surface^ D3DResource::GraphicsSurface::get(void)
{
    IDXGISurface* surface = NULL;

    Validate::VerifyResult(CastInterface<ID3D10Resource>()->QueryInterface(
            __uuidof(IDXGISurface), (void**)&surface));

    return surface != NULL ? gcnew Surface(surface) : nullptr;
}

Surface1^ D3DResource::GraphicsSurface1::get(void)
{
    IDXGISurface1* surface = NULL;

    Validate::VerifyResult(CastInterface<ID3D10Resource>()->QueryInterface(
            __uuidof(IDXGISurface1), (void**)&surface));

    return surface != NULL ? gcnew Surface1(surface) : nullptr;
}