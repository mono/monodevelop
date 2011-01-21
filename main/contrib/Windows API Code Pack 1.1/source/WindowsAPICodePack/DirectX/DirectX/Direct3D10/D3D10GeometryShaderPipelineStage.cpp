// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10GeometryShaderPipelineStage.h"
#include "D3D10DepthStencilView.h"
#include "D3D10RenderTargetView.h"
#include "D3D10Resource.h"
#include "D3D10Buffer.h"
#include "D3D10SamplerState.h"
#include "D3D10ShaderResourceView.h"
#include "D3D10GeometryShader.h"
#include "D3D10InputLayout.h"
#include "D3D10BlendState.h"
#include "D3D10DepthStencilState.h"
#include "D3D10PixelShader.h"
#include "D3D10RasterizerState.h"
#include "D3D10VertexShader.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

ReadOnlyCollection<D3DBuffer^>^ GeometryShaderPipelineStage::GetConstantBuffers(UInt32 startSlot, UInt32 bufferCount)
{
    vector<ID3D10Buffer*> tempoutConstantBuffers(bufferCount);
    Parent->CastInterface<ID3D10Device>()->GSGetConstantBuffers(static_cast<UINT>(startSlot), static_cast<UINT>(bufferCount), &tempoutConstantBuffers[0]);
        
    return Utilities::Convert::GetCollection<D3DBuffer, ID3D10Buffer>(bufferCount, tempoutConstantBuffers);
}

ReadOnlyCollection<SamplerState^>^ GeometryShaderPipelineStage::GetSamplers(UInt32 startSlot, UInt32 samplerCount)
{
    vector<ID3D10SamplerState*> tempoutSamplers(samplerCount);
    Parent->CastInterface<ID3D10Device>()->GSGetSamplers(static_cast<UINT>(startSlot), static_cast<UINT>(samplerCount), &tempoutSamplers[0]);

    return Utilities::Convert::GetCollection<SamplerState, ID3D10SamplerState>(samplerCount, tempoutSamplers);
}

GeometryShader^ GeometryShaderPipelineStage::Shader::get(void)
{
    ID3D10GeometryShader* tempShader = NULL;

    Parent->CastInterface<ID3D10Device>()->GSGetShader(
        &tempShader);

    return tempShader == NULL ? nullptr : gcnew GeometryShader(tempShader);
}

ReadOnlyCollection<ShaderResourceView^>^ GeometryShaderPipelineStage::GetShaderResources(UInt32 startSlot, UInt32 viewCount)
{
    vector <ID3D10ShaderResourceView*> tempoutShaderResourceViews(viewCount);
    Parent->CastInterface<ID3D10Device>()->GSGetShaderResources(static_cast<UINT>(startSlot), static_cast<UINT>(viewCount), &tempoutShaderResourceViews[0]);
    
    return Utilities::Convert::GetCollection<ShaderResourceView, ID3D10ShaderResourceView>(viewCount, tempoutShaderResourceViews);
}

void GeometryShaderPipelineStage::SetConstantBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ constantBuffers)
{
    if (Object::ReferenceEquals(constantBuffers, nullptr))
    {
        throw gcnew ArgumentNullException("constantBuffers");
    }

    vector<ID3D10Buffer*> itemsVector;

    UINT count = Utilities::Convert::FillIUnknownsVector<D3DBuffer, ID3D10Buffer>(constantBuffers, itemsVector);

    if (count == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "constantBuffers");
    }

    Parent->CastInterface<ID3D10Device>()->GSSetConstantBuffers(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}

void GeometryShaderPipelineStage::SetSamplers(UInt32 startSlot, IEnumerable<SamplerState^>^ samplers)
{
    if (Object::ReferenceEquals(samplers, nullptr))
    {
        throw gcnew ArgumentNullException("samplers");
    }

    vector<ID3D10SamplerState*> itemsVector;

    UINT count = Utilities::Convert::FillIUnknownsVector<SamplerState, ID3D10SamplerState>(samplers, itemsVector);

    if (count == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "samplers");
    }

    Parent->CastInterface<ID3D10Device>()->GSSetSamplers(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}

void GeometryShaderPipelineStage::Shader::set(GeometryShader^ shader)
{
    Parent->CastInterface<ID3D10Device>()->GSSetShader(
        shader == nullptr ? NULL : shader->CastInterface<ID3D10GeometryShader>());
}

void GeometryShaderPipelineStage::SetShaderResources(UInt32 startSlot, IEnumerable<ShaderResourceView^>^ shaderResourceViews)
{
    if (Object::ReferenceEquals(shaderResourceViews, nullptr))
    {
        throw gcnew ArgumentNullException("shaderResourceViews");
    }

    vector<ID3D10ShaderResourceView*> itemsVector;

    UINT count = Utilities::Convert::FillIUnknownsVector<ShaderResourceView, ID3D10ShaderResourceView>(shaderResourceViews, itemsVector);

    if (count == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "shaderResourceViews");
    }

    Parent->CastInterface<ID3D10Device>()->GSSetShaderResources(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}

