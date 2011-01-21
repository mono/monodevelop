// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectConstantBuffer.h"

#include "D3D10Buffer.h"
#include "D3D10ShaderResourceView.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

D3DBuffer^ EffectConstantBuffer::ConstantBuffer::get(void)
{
    ID3D10Buffer* tempoutConstantBuffer = NULL;
    Validate::VerifyResult(CastInterface<ID3D10EffectConstantBuffer>()->GetConstantBuffer(&tempoutConstantBuffer));
    return tempoutConstantBuffer == NULL ? nullptr : gcnew D3DBuffer(tempoutConstantBuffer);
}

ShaderResourceView^ EffectConstantBuffer::TextureBuffer::get(void)
{
    ID3D10ShaderResourceView* tempoutTextureBuffer = NULL;
    Validate::VerifyResult(CastInterface<ID3D10EffectConstantBuffer>()->GetTextureBuffer(&tempoutTextureBuffer));
    return tempoutTextureBuffer == NULL ? nullptr : gcnew ShaderResourceView(tempoutTextureBuffer);
}

void EffectConstantBuffer::ConstantBuffer::set(D3DBuffer^ constantBuffer)
{

    Validate::CheckNull(constantBuffer, "constantBuffer");

    Validate::VerifyResult(CastInterface<ID3D10EffectConstantBuffer>()->SetConstantBuffer(constantBuffer->CastInterface<ID3D10Buffer>()));
}

void EffectConstantBuffer::TextureBuffer::set(ShaderResourceView^ textureBuffer)
{

    Validate::CheckNull(textureBuffer, "textureBuffer");

    Validate::VerifyResult(CastInterface<ID3D10EffectConstantBuffer>()->SetTextureBuffer(textureBuffer->CastInterface<ID3D10ShaderResourceView>()));
}

