// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Texture1D.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

Texture1DDescription Texture1D::Description::get()
{
    Texture1DDescription desc;
    pin_ptr<Texture1DDescription> ptr = &desc;

    CastInterface<ID3D10Texture1D>()->GetDesc((D3D10_TEXTURE1D_DESC*)ptr);
    return desc;
}

IntPtr Texture1D::Map(UInt32 subresourceIndex, Direct3D10::Map type, Direct3D10::MapOptions options)
{
    void* tempoutData = NULL;
    Validate::VerifyResult(CastInterface<ID3D10Texture1D>()->Map(static_cast<UINT>(subresourceIndex), static_cast<D3D10_MAP>(type), static_cast<UINT>(options), &tempoutData));
    return IntPtr(tempoutData);
}

void Texture1D::Unmap(UInt32 subresourceIndex)
{
    CastInterface<ID3D10Texture1D>()->Unmap(static_cast<UINT>(subresourceIndex));
}

