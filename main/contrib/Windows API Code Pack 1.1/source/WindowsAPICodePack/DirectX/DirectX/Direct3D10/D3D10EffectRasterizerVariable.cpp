// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectRasterizerVariable.h"

#include "D3D10RasterizerState.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

RasterizerDescription EffectRasterizerVariable::GetBackingStore(UInt32 index)
{
    D3D10_RASTERIZER_DESC desc;

    Validate::VerifyResult(CastInterface<ID3D10EffectRasterizerVariable>()->GetBackingStore(
        index, &desc));
    
    return RasterizerDescription(desc);
}

RasterizerState^ EffectRasterizerVariable::GetRasterizerState(UInt32 index)
{
    ID3D10RasterizerState*  tempoutRasterizerState = NULL;
    Validate::VerifyResult(CastInterface<ID3D10EffectRasterizerVariable>()->GetRasterizerState(static_cast<UINT>(index), &tempoutRasterizerState));
    return tempoutRasterizerState == NULL ? nullptr : gcnew RasterizerState(tempoutRasterizerState);
}

