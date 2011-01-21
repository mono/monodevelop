// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectVectorVariable.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

Vector4B EffectVectorVariable::BoolVector::get()
{
    BOOL pArr[4];

    Validate::VerifyResult(CastInterface<ID3D10EffectVectorVariable>()->GetBoolVector((BOOL*)pArr));

    return Vector4B(pArr[0] != FALSE, pArr[1] != FALSE, pArr[2] != FALSE, pArr[3] != FALSE);
}

array<Vector4B>^ EffectVectorVariable::GetBoolVectorArray(UInt32 count)
{
    if (count == 0)
        return nullptr;

    // REVIEW: the compiler allows this, but Vector4B has no structure layout
    // control. Without that, it doesn't seem like this would be safe:

    array<Vector4B>^ arr = gcnew array<Vector4B>(count);
    pin_ptr<Vector4B> pArr = &arr[0];

    Validate::VerifyResult(CastInterface<ID3D10EffectVectorVariable>()->GetBoolVectorArray((BOOL*) pArr, 0, count));

    return arr;
}

Vector4F EffectVectorVariable::FloatVector::get()
{
    Single pArr[4];

    Validate::VerifyResult(CastInterface<ID3D10EffectVectorVariable>()->GetFloatVector(pArr));

    return Vector4F(pArr[0], pArr[1], pArr[2], pArr[3]);
}

array<Vector4F>^  EffectVectorVariable::GetFloatVectorArray(UInt32 count)
{
    if (count == 0)
        return nullptr;

    array<Vector4F>^ arr = gcnew array<Vector4F>(count);
    pin_ptr<Vector4F> pArr = &arr[0];

    Validate::VerifyResult(CastInterface<ID3D10EffectVectorVariable>()->GetFloatVectorArray((FLOAT*)pArr, 0, count));

    return arr;
}

Vector4I EffectVectorVariable::IntVector::get()
{
    int pArr[4];

    Validate::VerifyResult(CastInterface<ID3D10EffectVectorVariable>()->GetIntVector(pArr));

    return Vector4I(pArr[0], pArr[1], pArr[2], pArr[3]);
}

array<Vector4I>^ EffectVectorVariable::GetIntVectorArray(UInt32 count)
{
    if (count == 0)
        return nullptr;

    array<Vector4I>^ arr = gcnew array<Vector4I>(count);
    pin_ptr<Vector4I> pArr = &arr[0];

    Validate::VerifyResult(CastInterface<ID3D10EffectVectorVariable>()->GetIntVectorArray((int*)pArr, 0, count));

    return arr;
}

void EffectVectorVariable::BoolVector::set(Vector4B data)
{
    BOOL pArr[] = { data.X, data.Y, data.Z, data.W };

    Validate::VerifyResult(CastInterface<ID3D10EffectVectorVariable>()->SetBoolVector(pArr));
}

void EffectVectorVariable::SetBoolVectorArray(array<Vector4B>^ data)
{
    if (data == nullptr)
    {
        throw gcnew ArgumentNullException("data", "Must be a valid array.");
    }

    pin_ptr<Vector4B> pArr = &data[0];
    Validate::VerifyResult(CastInterface<ID3D10EffectVectorVariable>()->SetBoolVectorArray((BOOL*)pArr, 0, data->Length));
}

void EffectVectorVariable::FloatVector::set(Vector4F data)
{
    pin_ptr<FLOAT> dataArray = &data.dangerousFirstField;
    Validate::VerifyResult(CastInterface<ID3D10EffectVectorVariable>()->SetFloatVector(dataArray));
}

void EffectVectorVariable::SetFloatVectorArray(array<Vector4F>^ data)
{
    if (data == nullptr)
    {
        throw gcnew ArgumentNullException("data", "Must be a valid array.");
    }

    pin_ptr<float> pArr = &data[0].dangerousFirstField;
    Validate::VerifyResult(CastInterface<ID3D10EffectVectorVariable>()->SetFloatVectorArray(pArr, 0, data->Length));
}

void EffectVectorVariable::IntVector::set(Vector4I data)
{
    int pArr[] = { data.X, data.Y, data.Z, data.W };

    Validate::VerifyResult(CastInterface<ID3D10EffectVectorVariable>()->SetIntVector(pArr));
}

void EffectVectorVariable::SetIntVectorArray(array<Vector4I>^ data)
{
    if (data == nullptr)
    {
        throw gcnew ArgumentNullException("data", "Must be a valid array.");
    }

    pin_ptr<Vector4I> pArr = &data[0];

    Validate::VerifyResult(CastInterface<ID3D10EffectVectorVariable>()->SetIntVectorArray((int*)pArr, 0, data->Length));
}

