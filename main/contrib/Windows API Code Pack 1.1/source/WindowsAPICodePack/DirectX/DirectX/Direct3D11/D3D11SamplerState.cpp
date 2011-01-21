// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11SamplerState.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

SamplerDescription SamplerState::Description::get()
{
    D3D11_SAMPLER_DESC desc;
    CastInterface<ID3D11SamplerState>()->GetDesc(&desc);

    return SamplerDescription(desc);
}

