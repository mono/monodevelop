// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11Texture2D.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

Texture2DDescription Texture2D::Description::get()
{
    Texture2DDescription desc;
    pin_ptr<Texture2DDescription> ptr = &desc;

    CastInterface<ID3D11Texture2D>()->GetDesc((D3D11_TEXTURE2D_DESC*)ptr);
    return desc;
}

