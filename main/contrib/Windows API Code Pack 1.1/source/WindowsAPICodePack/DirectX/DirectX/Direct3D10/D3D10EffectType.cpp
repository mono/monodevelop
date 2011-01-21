// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectType.h"

#include "D3D10EffectType.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;
using namespace msclr::interop;


EffectTypeDescription EffectType::Description::get()
{
    D3D10_EFFECT_TYPE_DESC desc = {0};
    Validate::VerifyResult(CastInterface<ID3D10EffectType>()->GetDesc(&desc));

    return EffectTypeDescription(desc);
}

String^ EffectType::GetMemberName(UInt32 index)
{
    LPCSTR returnValue = CastInterface<ID3D10EffectType>()->GetMemberName(static_cast<UINT>(index));
    return returnValue ? gcnew String(returnValue) : nullptr;
}

String^ EffectType::GetMemberSemantic(UInt32 index)
{
    LPCSTR returnValue = CastInterface<ID3D10EffectType>()->GetMemberSemantic(static_cast<UINT>(index));
    return returnValue ? gcnew String(returnValue) : nullptr;
}

EffectType^ EffectType::GetMemberTypeByIndex(UInt32 index)
{
    ID3D10EffectType* returnValue = CastInterface<ID3D10EffectType>()->GetMemberTypeByIndex(static_cast<UINT>(index));
    return returnValue ? gcnew EffectType(returnValue) : nullptr;
}

EffectType^ EffectType::GetMemberTypeByName(String^ name)
{
    IntPtr ptr = Marshal::StringToHGlobalAnsi(name);

    try
    {
        ID3D10EffectType * returnValue = CastInterface<ID3D10EffectType>()->GetMemberTypeByName(static_cast<char*>(ptr.ToPointer()));

        return returnValue ? gcnew EffectType(returnValue) : nullptr;
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

EffectType^ EffectType::GetMemberTypeBySemantic(String^ semantic)
{

    IntPtr ptr = Marshal::StringToHGlobalAnsi(semantic);

    try
    {
        ID3D10EffectType * returnValue = CastInterface<ID3D10EffectType>()->GetMemberTypeBySemantic(static_cast<char*>(ptr.ToPointer()));

        return returnValue ? gcnew EffectType(returnValue) : nullptr;
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

Boolean EffectType::IsValid::get()
{
    return CastInterface<ID3D10EffectType>()->IsValid() != 0;
}

