// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10ShaderReflectionConstantBuffer.h"

#include "D3D10ShaderReflectionVariable.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;
using namespace msclr::interop;

ShaderBufferDescription ShaderReflectionConstantBuffer::Description::get()
{
    D3D10_SHADER_BUFFER_DESC desc = {0};
    Validate::VerifyResult(CastInterface<ID3D10ShaderReflectionConstantBuffer>()->GetDesc(&desc));
    
    return ShaderBufferDescription(desc);
}

ShaderReflectionVariable^ ShaderReflectionConstantBuffer::GetVariableByIndex(UInt32 index)
{
    ID3D10ShaderReflectionVariable* returnValue = CastInterface<ID3D10ShaderReflectionConstantBuffer>()->GetVariableByIndex(static_cast<UINT>(index));
    return returnValue == NULL ? nullptr : gcnew ShaderReflectionVariable(returnValue);
}

ShaderReflectionVariable^ ShaderReflectionConstantBuffer::GetVariableByName(String^ name)
{
    IntPtr ptr = Marshal::StringToHGlobalAnsi(name);

    try
    {
        ID3D10ShaderReflectionVariable * returnValue = CastInterface<ID3D10ShaderReflectionConstantBuffer>()->GetVariableByName(static_cast<char*>(ptr.ToPointer()));

        return returnValue ? gcnew ShaderReflectionVariable(returnValue) : nullptr;
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

