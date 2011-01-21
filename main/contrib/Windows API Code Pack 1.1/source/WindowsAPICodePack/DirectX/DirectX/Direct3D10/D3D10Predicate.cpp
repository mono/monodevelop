// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Predicate.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

bool D3DPredicate::WhenTrue::get(void)
{
    if (!whenTrue.HasValue)
    {
        ID3D10Predicate* tempPredicate = NULL;
        BOOL tempPredicateValue;

        Device->CastInterface<ID3D10Device>()->GetPredication(&tempPredicate, &tempPredicateValue);

        if (tempPredicate != CastInterface<ID3D10Predicate>())
        {
            throw gcnew InvalidOperationException("This predicate is not the current predicate for its parent device and its WhenTrue value hasn't been set yet");
        }

        whenTrue = Nullable<bool>(tempPredicateValue != FALSE);
    }

    return whenTrue.Value;
}

void D3DPredicate::WhenTrue::set(bool newWhenTrue)
{
    whenTrue = Nullable<bool>(newWhenTrue);

    ID3D10Predicate* tempPredicate = NULL;
    BOOL tempPredicateValue, newValue = newWhenTrue ? TRUE : FALSE;

    Device->CastInterface<ID3D10Device>()->GetPredication(&tempPredicate, &tempPredicateValue);

    if (tempPredicate == CastInterface<ID3D10Predicate>() &&
        tempPredicateValue != newValue)
    {
        Device->CastInterface<ID3D10Device>()->SetPredication(tempPredicate, newValue);
    }
}