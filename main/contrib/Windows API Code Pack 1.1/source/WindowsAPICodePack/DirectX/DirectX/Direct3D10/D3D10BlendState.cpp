// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10BlendState.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

BlendDescription BlendState::Description::get()
{
    D3D10_BLEND_DESC tempoutDescription = {0};
    CastInterface<ID3D10BlendState>()->GetDesc(&tempoutDescription);
    
    return BlendDescription(tempoutDescription);
}

