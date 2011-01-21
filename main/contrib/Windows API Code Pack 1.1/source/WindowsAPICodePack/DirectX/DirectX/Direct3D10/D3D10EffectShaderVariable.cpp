// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectShaderVariable.h"

#include "D3D10GeometryShader.h"
#include "D3D10PixelShader.h"
#include "D3D10VertexShader.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

GeometryShader^ EffectShaderVariable::GetGeometryShader(UInt32 shaderIndex)
{
    ID3D10GeometryShader* tempoutGS = NULL;
    Validate::VerifyResult(CastInterface<ID3D10EffectShaderVariable>()->GetGeometryShader(static_cast<UINT>(shaderIndex), &tempoutGS));

    return tempoutGS == NULL ? nullptr : gcnew GeometryShader(tempoutGS);
}

SignatureParameterDescription EffectShaderVariable::GetInputSignatureElementDescription(UInt32 shaderIndex, UInt32 elementIndex)
{
    D3D10_SIGNATURE_PARAMETER_DESC desc = {0};

    Validate::VerifyResult(CastInterface<ID3D10EffectShaderVariable>()->GetInputSignatureElementDesc(
        shaderIndex, elementIndex, &desc));

    return SignatureParameterDescription(desc);
}

SignatureParameterDescription EffectShaderVariable::GetOutputSignatureElementDescription(UInt32 shaderIndex, UInt32 elementIndex)
{
    D3D10_SIGNATURE_PARAMETER_DESC desc = {0};

    Validate::VerifyResult(CastInterface<ID3D10EffectShaderVariable>()->GetOutputSignatureElementDesc(
        shaderIndex, elementIndex, &desc));

    return SignatureParameterDescription(desc);
}

PixelShader^ EffectShaderVariable::GetPixelShader(UInt32 shaderIndex)
{
    ID3D10PixelShader* tempoutPS = NULL;
    Validate::VerifyResult(CastInterface<ID3D10EffectShaderVariable>()->GetPixelShader(static_cast<UINT>(shaderIndex), &tempoutPS));
    
    return tempoutPS == NULL ? nullptr : gcnew PixelShader(tempoutPS);
}

EffectShaderDescription EffectShaderVariable::GetShaderDescription(UInt32 shaderIndex)
{
    D3D10_EFFECT_SHADER_DESC desc = {0};
    Validate::VerifyResult(CastInterface<ID3D10EffectShaderVariable>()->GetShaderDesc(shaderIndex, &desc));

    return EffectShaderDescription(desc);
}

VertexShader^ EffectShaderVariable::GetVertexShader(UInt32 shaderIndex)
{
    ID3D10VertexShader* tempoutVS = NULL;    
    Validate::VerifyResult(CastInterface<ID3D10EffectShaderVariable>()->GetVertexShader(static_cast<UINT>(shaderIndex), &tempoutVS));

    return tempoutVS == NULL ? nullptr : gcnew VertexShader(tempoutVS);
}

