// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Structs.h"
#include "D3D10EffectShaderVariable.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;


PassShaderDescription::PassShaderDescription(const D3D10_PASS_SHADER_DESC& desc)
{
    ShaderVariable = desc.pShaderVariable ? gcnew EffectShaderVariable(desc.pShaderVariable) : nullptr;
    ShaderIndex = desc.ShaderIndex;
}
