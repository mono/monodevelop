// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11Predicate.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

DeviceContext^ D3DPredicate::Context::get(void)
{
    return context;
}

void D3DPredicate::Context::set(DeviceContext^ value)
{
    if (!whenTrue.HasValue)
    {
        throw gcnew InvalidOperationException("The device context for this predicate cannot be set until the WhenTrue property has been set");
    }

    if (context != value)
    {
        if (context != nullptr)
        {
            context->CastInterface<ID3D11DeviceContext>()->SetPredication(NULL, FALSE);
        }

        context = value;

        context->CastInterface<ID3D11DeviceContext>()->SetPredication(
            CastInterface<ID3D11Predicate>(), whenTrue.Value ? TRUE : FALSE);
    }
}

bool D3DPredicate::WhenTrue::get(void)
{
    if (!whenTrue.HasValue)
    {
        ID3D11Predicate* tempPredicate = NULL;
        BOOL tempPredicateValue = FALSE;

        if (context != nullptr)
        {

            context->CastInterface<ID3D11DeviceContext>()->GetPredication(
                &tempPredicate, &tempPredicateValue);
        }

#ifdef _DEBUG
        // Either way of creating a D3DPredicate should set the underlying native
        // interface by this point. If for some reason, that happens to not be true,
        // the WhenTrue property will be initialized to FALSE, per the initialized
        // value for the local variable.
        System::Diagnostics::Debug::Assert(CastInterface<ID3D11Predicate>() != NULL);
#endif // _DEBUG

        if (tempPredicate != CastInterface<ID3D11Predicate>())
        {
            throw gcnew InvalidOperationException("This predicate is not the current predicate for any device context and its WhenTrue value hasn't been set yet");
        }

        whenTrue = Nullable<bool>(tempPredicateValue != FALSE);
    }

    return whenTrue.Value;
}

void D3DPredicate::WhenTrue::set(bool value)
{
    whenTrue = Nullable<bool>(value);

    if (context != nullptr)
    {
        ID3D11Predicate* tempPredicate = NULL;
        BOOL tempPredicateValue, newValue = value ? TRUE : FALSE;

        context->CastInterface<ID3D11DeviceContext>()->GetPredication(&tempPredicate, &tempPredicateValue);

        if (tempPredicate == CastInterface<ID3D11Predicate>() &&
            tempPredicateValue != newValue)
        {
            context->CastInterface<ID3D11DeviceContext>()->SetPredication(tempPredicate, newValue);
        }
    }
}