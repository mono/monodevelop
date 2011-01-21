// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11Texture3D.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

Texture3DDescription Texture3D::Description::get()
{
    Texture3DDescription desc;
    pin_ptr<Texture3DDescription> ptr = &desc;

    CastInterface<ID3D11Texture3D>()->GetDesc((D3D11_TEXTURE3D_DESC*)ptr);
    return desc;
}

