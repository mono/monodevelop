// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectSamplerVariable.h"

#include "D3D10SamplerState.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

SamplerDescription EffectSamplerVariable::GetBackingStore(UInt32 index)
{
    D3D10_SAMPLER_DESC desc;
    Validate::VerifyResult(CastInterface<ID3D10EffectSamplerVariable>()->GetBackingStore(
        index, 
        &desc));

    return SamplerDescription(desc);
}

SamplerState^ EffectSamplerVariable::GetSampler(UInt32 index)
{
    ID3D10SamplerState* tempoutSampler = NULL;
    
    Validate::VerifyResult(CastInterface<ID3D10EffectSamplerVariable>()->GetSampler(static_cast<UINT>(index), &tempoutSampler));

    return tempoutSampler == NULL ? nullptr : gcnew SamplerState(tempoutSampler);
}

