// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Texture2D.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

Texture2DDescription Texture2D::Description::get()
{
    D3D10_TEXTURE2D_DESC desc = {0};

    CastInterface<ID3D10Texture2D>()->GetDesc(&desc);
    return Texture2DDescription(desc);
}

MappedTexture2D Texture2D::Map(UInt32 subresourceIndex, Direct3D10::Map type, Direct3D10::MapOptions options)
{
    D3D10_MAPPED_TEXTURE2D texture = {0};
    
    Validate::VerifyResult(CastInterface<ID3D10Texture2D>()->Map(
        subresourceIndex, static_cast<D3D10_MAP>(type), 
        static_cast<UINT>(options), &texture));
    
    return MappedTexture2D(texture);
}

void Texture2D::Unmap(UInt32 subresourceIndex)
{
    CastInterface<ID3D10Texture2D>()->Unmap(static_cast<UINT>(subresourceIndex));
}
