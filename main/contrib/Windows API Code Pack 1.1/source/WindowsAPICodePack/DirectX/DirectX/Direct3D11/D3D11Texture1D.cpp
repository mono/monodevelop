// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11Texture1D.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

Texture1DDescription Texture1D::Description::get()
{
    Texture1DDescription desc;
    pin_ptr<Texture1DDescription> ptr = &desc;

    CastInterface<ID3D11Texture1D>()->GetDesc((D3D11_TEXTURE1D_DESC*)ptr);
    return desc;
}

