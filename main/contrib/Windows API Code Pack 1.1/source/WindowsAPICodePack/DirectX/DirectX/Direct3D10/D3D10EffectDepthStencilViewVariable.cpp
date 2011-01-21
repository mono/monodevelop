// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectDepthStencilViewVariable.h"

#include "D3D10DepthStencilView.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

DepthStencilView^ EffectDepthStencilViewVariable::DepthStencil::get(void)
{
    ID3D10DepthStencilView* tempoutResource = NULL;
    Validate::VerifyResult(CastInterface<ID3D10EffectDepthStencilViewVariable>()->GetDepthStencil(&tempoutResource));
    
    return tempoutResource == NULL ? nullptr : gcnew DepthStencilView(tempoutResource);
}

ReadOnlyCollection<DepthStencilView^>^ EffectDepthStencilViewVariable::GetDepthStencilArray(UInt32 offset, UInt32 count)
{
    vector<ID3D10DepthStencilView*> tempVector(count);
    Validate::VerifyResult(
        CastInterface<ID3D10EffectDepthStencilViewVariable>()->GetDepthStencilArray(&tempVector[0], static_cast<UINT>(offset), static_cast<UINT>(count)));

    return Utilities::Convert::GetCollection<DepthStencilView, ID3D10DepthStencilView>(count, tempVector);
}

void EffectDepthStencilViewVariable::DepthStencil::set(DepthStencilView^ resource)
{

    Validate::CheckNull(resource,"resource");

    Validate::VerifyResult(CastInterface<ID3D10EffectDepthStencilViewVariable>()->SetDepthStencil(resource->CastInterface<ID3D10DepthStencilView>()));
}

void EffectDepthStencilViewVariable::SetDepthStencilArray(IEnumerable<DepthStencilView^>^ resources, UInt32 offset)
{
    vector<ID3D10DepthStencilView*> itemsVector;

    UINT count = Utilities::Convert::FillIUnknownsVector<DepthStencilView, ID3D10DepthStencilView>(resources, itemsVector);

    Validate::VerifyResult(
        CastInterface<ID3D10EffectDepthStencilViewVariable>()->SetDepthStencilArray(
        count == 0 ? NULL : &(itemsVector[0]), static_cast<UINT>(offset), static_cast<UINT>(count)));
}

