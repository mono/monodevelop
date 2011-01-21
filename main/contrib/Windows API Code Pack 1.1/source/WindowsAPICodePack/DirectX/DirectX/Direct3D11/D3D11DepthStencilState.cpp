// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11DepthStencilState.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

DepthStencilDescription DepthStencilState::Description::get()
{
    D3D11_DEPTH_STENCIL_DESC desc = {0};

    CastInterface<ID3D11DepthStencilState>()->GetDesc(&desc);

    return DepthStencilDescription(desc);
}

