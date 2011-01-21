// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11BlendState.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

BlendDescription BlendState::Description::get()
{
    D3D11_BLEND_DESC desc = {0};

    CastInterface<ID3D11BlendState>()->GetDesc(&desc);

    return BlendDescription(desc);
}

