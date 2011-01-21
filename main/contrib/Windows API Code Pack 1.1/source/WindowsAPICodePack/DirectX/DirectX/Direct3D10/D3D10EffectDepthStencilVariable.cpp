// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectDepthStencilVariable.h"

#include "D3D10DepthStencilState.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

DepthStencilDescription EffectDepthStencilVariable::GetBackingStore(UInt32 index)
{
    D3D10_DEPTH_STENCIL_DESC desc = {0};

    Validate::VerifyResult(CastInterface<ID3D10EffectDepthStencilVariable>()->GetBackingStore(
        index, &desc));

    return DepthStencilDescription(desc);
}

DepthStencilState^  EffectDepthStencilVariable::GetDepthStencilState(UInt32 index)
{
    ID3D10DepthStencilState* tempoutDepthStencilState = NULL;
    Validate::VerifyResult(CastInterface<ID3D10EffectDepthStencilVariable>()->GetDepthStencilState(static_cast<UINT>(index), &tempoutDepthStencilState));
    
    return tempoutDepthStencilState == NULL ? nullptr : gcnew DepthStencilState(tempoutDepthStencilState);
}

