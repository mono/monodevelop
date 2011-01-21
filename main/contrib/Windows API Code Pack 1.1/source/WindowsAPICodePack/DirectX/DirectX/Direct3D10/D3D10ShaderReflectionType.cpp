// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10ShaderReflectionType.h"
#include "D3D10ShaderReflectionType.h"

using namespace msclr::interop;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

ShaderTypeDescription ShaderReflectionType::Description::get()
{
    ShaderTypeDescription desc;
    pin_ptr<ShaderTypeDescription> ptr = &desc;
    
    Validate::VerifyResult(CastInterface<ID3D10ShaderReflectionType>()->GetDesc((D3D10_SHADER_TYPE_DESC*)ptr));
    return desc;
}

ShaderReflectionType^ ShaderReflectionType::GetMemberTypeByIndex(UInt32 index)
{
    ID3D10ShaderReflectionType* returnValue = CastInterface<ID3D10ShaderReflectionType>()->GetMemberTypeByIndex(static_cast<UINT>(index));
    return returnValue == NULL ? nullptr : gcnew ShaderReflectionType(returnValue);
}

ShaderReflectionType^ ShaderReflectionType::GetMemberTypeByName(String^ name)
{
    IntPtr ptr = Marshal::StringToHGlobalAnsi(name);

    try
    {
        ID3D10ShaderReflectionType * returnValue =  CastInterface<ID3D10ShaderReflectionType>()->GetMemberTypeByName(static_cast<char*>(ptr.ToPointer()));

        return returnValue ? gcnew ShaderReflectionType(returnValue) : nullptr;
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

String^ ShaderReflectionType::GetMemberTypeName(UInt32 index)
{
    LPCSTR returnValue = CastInterface<ID3D10ShaderReflectionType>()->GetMemberTypeName(static_cast<UINT>(index));
    return returnValue == NULL ? nullptr : gcnew String(returnValue);
}

