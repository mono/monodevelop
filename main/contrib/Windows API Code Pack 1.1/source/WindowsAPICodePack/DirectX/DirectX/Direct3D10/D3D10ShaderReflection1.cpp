// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10ShaderReflection1.h"
#include "D3D10ShaderReflectionVariable.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;
using namespace msclr::interop;

UInt32 ShaderReflection1::BitwiseInstructionCount::get()
{
    UINT tempoutCount;
    Validate::VerifyResult(CastInterface<ID3D10ShaderReflection1>()->GetBitwiseInstructionCount(&tempoutCount));
    return safe_cast<UInt32>(tempoutCount);
}

UInt32 ShaderReflection1::ConversionInstructionCount::get()
{
    UINT tempoutCount;
    Validate::VerifyResult(CastInterface<ID3D10ShaderReflection1>()->GetConversionInstructionCount(&tempoutCount));
    return safe_cast<UInt32>(tempoutCount);
}

Primitive ShaderReflection1::GSInputPrimitive::get()
{
    D3D10_PRIMITIVE tempoutPrim;
    Validate::VerifyResult(CastInterface<ID3D10ShaderReflection1>()->GetGSInputPrimitive(&tempoutPrim));
    return safe_cast<Primitive>(tempoutPrim);
}

UInt32 ShaderReflection1::MovcInstructionCount::get()
{
    UINT tempoutCount;
    Validate::VerifyResult(CastInterface<ID3D10ShaderReflection1>()->GetMovcInstructionCount(&tempoutCount));
    return safe_cast<UInt32>(tempoutCount);
}

UInt32 ShaderReflection1::MovInstructionCount::get()
{
    UINT tempoutCount;
    Validate::VerifyResult(CastInterface<ID3D10ShaderReflection1>()->GetMovInstructionCount(&tempoutCount));
    return safe_cast<UInt32>(tempoutCount);
}

ShaderInputBindDescription ShaderReflection1::GetResourceBindingDescriptionByName(String^ name)
{
    D3D10_SHADER_INPUT_BIND_DESC desc = {0};
    IntPtr ptr = Marshal::StringToHGlobalAnsi(name);

    try
    {
        Validate::VerifyResult(CastInterface<ID3D10ShaderReflection1>()->GetResourceBindingDescByName(
            static_cast<char*>(ptr.ToPointer()), 
            &desc));

        return ShaderInputBindDescription(desc);
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

ShaderReflectionVariable^ ShaderReflection1::GetVariableByName(String^ name)
{
    IntPtr ptr = Marshal::StringToHGlobalAnsi(name);

    try
    {
        ID3D10ShaderReflectionVariable * returnValue =  CastInterface<ID3D10ShaderReflection1>()->GetVariableByName(static_cast<char*>(ptr.ToPointer()));

        return returnValue ? gcnew ShaderReflectionVariable(returnValue) : nullptr;
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

Boolean ShaderReflection1::IsLevel9Shader::get()
{
    BOOL tempoutbLevel9Shader;
    Validate::VerifyResult(CastInterface<ID3D10ShaderReflection1>()->IsLevel9Shader(&tempoutbLevel9Shader));
    return tempoutbLevel9Shader != 0;
}

Boolean ShaderReflection1::IsSampleFrequencyShader::get()
{
    BOOL tempoutbSampleFrequency;
    Validate::VerifyResult(CastInterface<ID3D10ShaderReflection1>()->IsSampleFrequencyShader(&tempoutbSampleFrequency));
    return tempoutbSampleFrequency != 0;
}

