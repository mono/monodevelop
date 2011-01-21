// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11Resource.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

UInt32 D3DResource::EvictionPriority::get()
{
    return CastInterface<ID3D11Resource>()->GetEvictionPriority();
}

void D3DResource::EvictionPriority::set(UInt32 value)
{
    CastInterface<ID3D11Resource>()->SetEvictionPriority(static_cast<UINT>(value));
}

ResourceDimension D3DResource::ResourceDimension::get()
{
    D3D11_RESOURCE_DIMENSION tempoutrType = D3D11_RESOURCE_DIMENSION_UNKNOWN;

    CastInterface<ID3D11Resource>()->GetType(&tempoutrType);

    return safe_cast<Direct3D11::ResourceDimension>(tempoutrType);
}

Surface^ D3DResource::GraphicsSurface::get(void)
{
    IDXGISurface *surface = NULL;

    Validate::VerifyResult ( CastInterface<ID3D11Resource>()->QueryInterface(
            __uuidof(IDXGISurface),
            (void**)&surface));

    return surface != NULL ? gcnew Surface(surface) : nullptr;
}

/// <summary>
/// Get associated Graphics.Surface1.
/// </summary>
Surface1^ D3DResource::GraphicsSurface1::get(void)
{
    IDXGISurface1 *surface = NULL;

    Validate::VerifyResult ( CastInterface<ID3D11Resource>()->QueryInterface(
            __uuidof(IDXGISurface1),
            (void**)&surface));

    return surface != NULL ? gcnew Surface1(surface) : nullptr;
}