// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DXGISurface.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

SurfaceDescription Surface::Description::get()
{
    DXGI_SURFACE_DESC desc;

    Validate::VerifyResult(CastInterface<IDXGISurface>()->GetDesc(&desc));

    return SurfaceDescription(desc);
}

MappedRect Surface::Map(MapOptions options)
{
    DXGI_MAPPED_RECT rect;
    Validate::VerifyResult(CastInterface<IDXGISurface>()->Map(
        &rect, static_cast<UINT>(options)));
    
    return MappedRect(rect);
}

void Surface::Unmap()
{
    Validate::VerifyResult(CastInterface<IDXGISurface>()->Unmap());
}

