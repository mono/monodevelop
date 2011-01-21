// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11ClassLinkage.h"
#include "D3D11ClassInstance.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;
using namespace msclr::interop;

ClassInstance^ ClassLinkage::CreateClassInstance(String^ classTypeName, UInt32 ConstantBufferOffset, UInt32 ConstantVectorOffset, UInt32 TextureOffset, UInt32 SamplerOffset)
{
    if (String::IsNullOrEmpty(classTypeName))
    {
        throw gcnew ArgumentNullException("classTypeName", "Argument cannot be null or empty.");
    }

    marshal_context^ context = gcnew marshal_context();
    ID3D11ClassInstance* tempoutInstance = NULL;
    Validate::VerifyResult(CastInterface<ID3D11ClassLinkage>()->CreateClassInstance(context->marshal_as<const char*>(classTypeName), static_cast<UINT>(ConstantBufferOffset), static_cast<UINT>(ConstantVectorOffset), static_cast<UINT>(TextureOffset), static_cast<UINT>(SamplerOffset), &tempoutInstance));
    return tempoutInstance == NULL ? nullptr : gcnew ClassInstance(tempoutInstance);
}

ClassInstance^ ClassLinkage::GetClassInstance(String^ classInstanceName, UInt32 instanceIndex)
{
    if (String::IsNullOrEmpty(classInstanceName))
    {
        throw gcnew ArgumentNullException("classInstanceName", "Argument cannot be null or empty.");
    }

    ID3D11ClassInstance* tempoutInstance = NULL;
    marshal_context^ context = gcnew marshal_context();
    Validate::VerifyResult(CastInterface<ID3D11ClassLinkage>()->GetClassInstance(context->marshal_as<const char*>(classInstanceName), static_cast<UINT>(instanceIndex), &tempoutInstance));
    return tempoutInstance == NULL ? nullptr : gcnew ClassInstance(tempoutInstance);
}

