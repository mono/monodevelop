// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectRenderTargetViewVariable.h"

#include "D3D10RenderTargetView.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

RenderTargetView^ EffectRenderTargetViewVariable::RenderTarget::get(void)
{
    ID3D10RenderTargetView* resource = NULL;
    Validate::VerifyResult(CastInterface<ID3D10EffectRenderTargetViewVariable>()->GetRenderTarget(&resource));
 
    return resource == NULL ? nullptr : gcnew RenderTargetView(resource);
}

ReadOnlyCollection<RenderTargetView^>^ EffectRenderTargetViewVariable::GetRenderTargetCollection(UInt32 offset, UInt32 count)
{
    vector<ID3D10RenderTargetView*> resourcesVector(count);
    Validate::VerifyResult(CastInterface<ID3D10EffectRenderTargetViewVariable>()->GetRenderTargetArray(&resourcesVector[0], static_cast<UINT>(offset), static_cast<UINT>(count)));

    return Utilities::Convert::GetCollection<RenderTargetView, ID3D10RenderTargetView>(count, resourcesVector);    
}

void EffectRenderTargetViewVariable::RenderTarget::set(RenderTargetView^ resource)
{
    Validate::VerifyResult(CastInterface<ID3D10EffectRenderTargetViewVariable>()->SetRenderTarget(
        resource == nullptr ? NULL : resource->CastInterface<ID3D10RenderTargetView>()));
}

void EffectRenderTargetViewVariable::SetRenderTargetCollection(IEnumerable<RenderTargetView^>^ resources, UInt32 offset)
{
    vector<ID3D10RenderTargetView*> resourcesVector;

    UINT count = Utilities::Convert::FillIUnknownsVector<RenderTargetView, ID3D10RenderTargetView>(resources, resourcesVector);

    Validate::VerifyResult(
        CastInterface<ID3D10EffectRenderTargetViewVariable>()->SetRenderTargetArray(
        count == 0 ? NULL : &resourcesVector[0], static_cast<UINT>(offset), count));    
}

