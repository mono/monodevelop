// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10SamplerState.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

SamplerDescription SamplerState::Description::get()
{
    D3D10_SAMPLER_DESC tempoutDescription;
    CastInterface<ID3D10SamplerState>()->GetDesc(&tempoutDescription);
    
    return SamplerDescription(tempoutDescription);
}

