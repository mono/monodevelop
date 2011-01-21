// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11StreamOutputPipelineStage.h"

#include "D3D11Buffer.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

ReadOnlyCollection<D3DBuffer^>^ StreamOutputPipelineStage::GetTargets(UInt32 bufferCount)
{
    vector <ID3D11Buffer*> tempoutSOTargets(bufferCount);
    Parent->CastInterface<ID3D11DeviceContext>()->SOGetTargets(static_cast<UINT>(bufferCount), &(tempoutSOTargets[0]));
        
    return Utilities::Convert::GetCollection<D3DBuffer, ID3D11Buffer>(bufferCount, tempoutSOTargets);
}

void StreamOutputPipelineStage::SetTargets(IEnumerable<D3DBuffer^>^ targets, array<UInt32>^ offsets)
{
    vector<ID3D11Buffer*> itemsVector;
    UINT count = Utilities::Convert::FillIUnknownsVector<D3DBuffer, ID3D11Buffer>(targets, itemsVector);

    if (count > 0 && (count != static_cast<UINT>(offsets->Length)))
    {
        throw gcnew InvalidOperationException("Both targets and offsets sizes must be equal.");
    }

    pin_ptr<UINT> offsetsArray = (count > 0 ? offsetsArray = &offsets[0] : nullptr);

    Parent->CastInterface<ID3D11DeviceContext>()->SOSetTargets(
        count, 
        count == 0 ? NULL : &itemsVector[0],
        offsetsArray);
}

