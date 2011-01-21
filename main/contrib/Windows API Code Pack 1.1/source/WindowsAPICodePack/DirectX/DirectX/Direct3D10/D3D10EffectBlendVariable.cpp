// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectBlendVariable.h"

#include "D3D10BlendState.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

BlendDescription EffectBlendVariable::GetBackingStore(UInt32 index)
{
    D3D10_BLEND_DESC tempoutBlendDesc = {0};

    Validate::VerifyResult(CastInterface<ID3D10EffectBlendVariable>()->GetBackingStore(
        index, &tempoutBlendDesc));

    return BlendDescription(tempoutBlendDesc);
}

BlendState^ EffectBlendVariable::GetBlendState(UInt32 index)
{
    ID3D10BlendState* tempoutBlendState = NULL;
    
    Validate::VerifyResult(
        CastInterface<ID3D10EffectBlendVariable>()->GetBlendState(static_cast<UINT>(index), &tempoutBlendState));
    
    return tempoutBlendState == NULL ? nullptr : gcnew BlendState(tempoutBlendState);
}

