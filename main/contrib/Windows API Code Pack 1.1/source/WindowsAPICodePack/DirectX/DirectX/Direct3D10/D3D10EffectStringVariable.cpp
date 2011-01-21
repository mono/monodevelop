// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectStringVariable.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

String^ EffectStringVariable::StringValue::get()
{
    LPCSTR tempoutString = NULL;
    Validate::VerifyResult(CastInterface<ID3D10EffectStringVariable>()->GetString(&tempoutString));

    return tempoutString == NULL ? nullptr : gcnew String(tempoutString);
}

array<String^>^ EffectStringVariable::GetStringArray(UInt32 offset, UInt32 count)
{
    vector<LPCSTR> strVector(count);
    array<String^>^ arr = gcnew array<String^>(count);

    Validate::VerifyResult(CastInterface<ID3D10EffectStringVariable>()->GetStringArray(&strVector[0], offset, count));

    for (UINT i = 0, s =  count; i < s; i++)
    {
        arr[i] = strVector[i] == NULL ? nullptr : gcnew String(strVector[i]);
    }

    return arr;
}

