// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10RasterizerState.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

RasterizerDescription RasterizerState::Description::get()
{
    D3D10_RASTERIZER_DESC desc;

    CastInterface<ID3D10RasterizerState>()->GetDesc(&desc);
    
    return RasterizerDescription(desc);
}

