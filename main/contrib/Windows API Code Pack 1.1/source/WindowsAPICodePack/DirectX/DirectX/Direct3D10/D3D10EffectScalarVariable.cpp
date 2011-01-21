// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectScalarVariable.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

Boolean EffectScalarVariable::AsBoolean::get()
{
    BOOL tempoutValue;
    Validate::VerifyResult(CastInterface<ID3D10EffectScalarVariable>()->GetBool(&tempoutValue));
    
    return tempoutValue != 0;
}

array<Boolean>^ EffectScalarVariable::GetBooleanArray(UInt32 count)
{
    array<Boolean>^ arr = gcnew array<Boolean>(count);
    pin_ptr<Boolean> pArr = &arr[0];
    
    Validate::VerifyResult(CastInterface<ID3D10EffectScalarVariable>()->GetBoolArray((BOOL*)pArr, 0, count));
    
    return arr;
}

Single EffectScalarVariable::AsSingle::get()
{
    FLOAT tempoutValue;
    Validate::VerifyResult(CastInterface<ID3D10EffectScalarVariable>()->GetFloat(&tempoutValue));
    
    return tempoutValue;
}

array<Single>^ EffectScalarVariable::GetSingleArray(UInt32 count)
{
    array<Single>^ arr = gcnew array<Single>(count);
    pin_ptr<FLOAT> pArr = &arr[0];

    Validate::VerifyResult(CastInterface<ID3D10EffectScalarVariable>()->GetFloatArray(pArr, 0, count));
    
    return arr;
}

Int32 EffectScalarVariable::AsInt32::get()
{
    int tempoutValue;
    Validate::VerifyResult(CastInterface<ID3D10EffectScalarVariable>()->GetInt(&tempoutValue));

    return tempoutValue;
}

array<int>^ EffectScalarVariable::GetInt32Array(UInt32 count)
{
    array<Int32>^ arr = gcnew array<Int32>(count);
    pin_ptr<int> pArr = &arr[0];

    Validate::VerifyResult(CastInterface<ID3D10EffectScalarVariable>()->GetIntArray(pArr, 0, count));
    
    return arr;
}

void EffectScalarVariable::AsBoolean::set(Boolean value)
{
    Validate::VerifyResult(CastInterface<ID3D10EffectScalarVariable>()->SetBool(value != 0));
}

void EffectScalarVariable::SetBooleanArray(array<Boolean>^ data)
{
    UINT count = data->Length;
    pin_ptr<Boolean> pArr = &data[0];

    Validate::VerifyResult(CastInterface<ID3D10EffectScalarVariable>()->SetBoolArray((BOOL*) pArr, 0, count));
}

void EffectScalarVariable::AsSingle::set(Single value)
{
    Validate::VerifyResult(CastInterface<ID3D10EffectScalarVariable>()->SetFloat(value));
}

void EffectScalarVariable::SetSingleArray(array<Single>^ data)
{
    UINT count = data->Length;
    pin_ptr<Single> pArr = &data[0];

    Validate::VerifyResult(CastInterface<ID3D10EffectScalarVariable>()->SetFloatArray(pArr, 0, count));
}

void EffectScalarVariable::AsInt32::set(int Value)
{
    Validate::VerifyResult(CastInterface<ID3D10EffectScalarVariable>()->SetInt(Value));
}

void EffectScalarVariable::SetInt32Array(array<Int32>^ data)
{
    UINT count = data->Length;
    pin_ptr<int> pArr = &data[0];

    Validate::VerifyResult(CastInterface<ID3D10EffectScalarVariable>()->SetIntArray(pArr, 0, count));
}