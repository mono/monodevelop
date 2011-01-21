// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10DepthStencilState.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

DepthStencilDescription DepthStencilState::Description::get()
{
    D3D10_DEPTH_STENCIL_DESC desc = {0};

    CastInterface<ID3D10DepthStencilState>()->GetDesc(&desc);

    return DepthStencilDescription(desc);
}

