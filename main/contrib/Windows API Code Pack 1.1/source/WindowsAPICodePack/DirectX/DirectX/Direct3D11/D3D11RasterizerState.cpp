// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11RasterizerState.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

RasterizerDescription RasterizerState::Description::get()
{
    D3D11_RASTERIZER_DESC desc;

    CastInterface<ID3D11RasterizerState>()->GetDesc(&desc);
    
    return RasterizerDescription(desc);
}

