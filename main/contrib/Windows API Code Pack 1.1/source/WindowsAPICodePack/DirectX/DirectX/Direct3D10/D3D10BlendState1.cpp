// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10BlendState1.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

BlendDescription1 BlendState1::Description1::get()
{
    D3D10_BLEND_DESC1 tempoutDescription = {0};
    CastInterface<ID3D10BlendState1>()->GetDesc1(&tempoutDescription);
    
    return BlendDescription1(tempoutDescription);
}

