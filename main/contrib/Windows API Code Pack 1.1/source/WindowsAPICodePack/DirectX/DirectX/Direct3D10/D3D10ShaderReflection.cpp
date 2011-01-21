// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10ShaderReflection.h"
#include "D3D10ShaderReflectionConstantBuffer.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;
using namespace msclr::interop;

ShaderReflectionConstantBuffer^ ShaderReflection::GetConstantBufferByIndex(UInt32 index)
{
    ID3D10ShaderReflectionConstantBuffer* returnValue = CastInterface<ID3D10ShaderReflection>()->GetConstantBufferByIndex(static_cast<UINT>(index));
    return returnValue == NULL ? nullptr : gcnew ShaderReflectionConstantBuffer(returnValue);
}

ShaderReflectionConstantBuffer^ ShaderReflection::GetConstantBufferByName(String^ name)
{
    IntPtr ptr = Marshal::StringToHGlobalAnsi(name);

    try
    {
        ID3D10ShaderReflectionConstantBuffer * returnValue = CastInterface<ID3D10ShaderReflection>()->GetConstantBufferByName(static_cast<char*>(ptr.ToPointer()));

        return returnValue ? gcnew ShaderReflectionConstantBuffer(returnValue) : nullptr;
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

ShaderDescription ShaderReflection::Description::get()
{
    D3D10_SHADER_DESC desc = {0};
    Validate::VerifyResult(CastInterface<ID3D10ShaderReflection>()->GetDesc(&desc));
    return ShaderDescription(desc);
}

SignatureParameterDescription ShaderReflection::GetInputParameterDescription(UInt32 parameterIndex)
{
    D3D10_SIGNATURE_PARAMETER_DESC desc = {0};

    Validate::VerifyResult(CastInterface<ID3D10ShaderReflection>()->GetInputParameterDesc(parameterIndex, &desc));

    return SignatureParameterDescription(desc);
}

SignatureParameterDescription ShaderReflection::GetOutputParameterDescription(UInt32 parameterIndex)
{
    D3D10_SIGNATURE_PARAMETER_DESC desc = {0};
    
    Validate::VerifyResult(CastInterface<ID3D10ShaderReflection>()->GetOutputParameterDesc(parameterIndex, &desc));
    
    return SignatureParameterDescription(desc);
}

ShaderInputBindDescription ShaderReflection::GetResourceBindingDescription(UInt32 resourceIndex)
{
    D3D10_SHADER_INPUT_BIND_DESC desc = {0};
    Validate::VerifyResult(CastInterface<ID3D10ShaderReflection>()->GetResourceBindingDesc(
        resourceIndex, &desc));

    return ShaderInputBindDescription(desc);
}

