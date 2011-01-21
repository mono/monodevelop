// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Texture3D.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

Texture3DDescription Texture3D::Description::get()
{
    Texture3DDescription desc;
    pin_ptr<Texture3DDescription> ptr = &desc;

    CastInterface<ID3D10Texture3D>()->GetDesc((D3D10_TEXTURE3D_DESC*)ptr);
    return desc;
}

MappedTexture3D Texture3D::Map(UInt32 subresourceIndex, Direct3D10::Map type, Direct3D10::MapOptions options)
{
    D3D10_MAPPED_TEXTURE3D texture = {0};
    Validate::VerifyResult(CastInterface<ID3D10Texture3D>()->Map(
        subresourceIndex, 
        static_cast<D3D10_MAP>(type), 
        static_cast<UINT>(options), 
        &texture));

    return MappedTexture3D(texture);
}

void Texture3D::Unmap(UInt32 subresourceIndex)
{
    CastInterface<ID3D10Texture3D>()->Unmap(static_cast<UINT>(subresourceIndex));
}
