// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10VertexShaderPipelineStage.h"
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

#include <vector>
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

ReadOnlyCollection<D3DBuffer^>^ VertexShaderPipelineStage::GetConstantBuffers(UInt32 startSlot, UInt32 bufferCount)
{
    vector<ID3D10Buffer*> items(bufferCount);

    Parent->CastInterface<ID3D10Device>()->VSGetConstantBuffers(static_cast<UINT>(startSlot), static_cast<UINT>(bufferCount), &(items[0]));

    return Utilities::Convert::GetCollection<D3DBuffer, ID3D10Buffer>(bufferCount, items);
}

ReadOnlyCollection<SamplerState^>^ VertexShaderPipelineStage::GetSamplers(UInt32 startSlot, UInt32 samplerCount)
{
    vector<ID3D10SamplerState*> samplersVector(samplerCount);
    Parent->CastInterface<ID3D10Device>()->VSGetSamplers(static_cast<UINT>(startSlot), static_cast<UINT>(samplerCount), &samplersVector[0]);

    return Utilities::Convert::GetCollection<SamplerState, ID3D10SamplerState>(samplerCount, samplersVector);
}

VertexShader^ VertexShaderPipelineStage::Shader::get(void)
{
    ID3D10VertexShader* tempShader = NULL;

    Parent->CastInterface<ID3D10Device>()->VSGetShader(
        &tempShader);

    return tempShader == NULL ? nullptr : gcnew VertexShader(tempShader);
}

ReadOnlyCollection<ShaderResourceView^>^ VertexShaderPipelineStage::GetShaderResources(UInt32 startSlot, UInt32 viewCount)
{
    vector<ID3D10ShaderResourceView*> tempoutShaderResourceViews(viewCount);
    Parent->CastInterface<ID3D10Device>()->VSGetShaderResources(static_cast<UINT>(startSlot), static_cast<UINT>(viewCount), &tempoutShaderResourceViews[0]);
    
    return Utilities::Convert::GetCollection<ShaderResourceView, ID3D10ShaderResourceView>(viewCount, tempoutShaderResourceViews);
}

void VertexShaderPipelineStage::SetConstantBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ constantBuffers)
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

    Parent->CastInterface<ID3D10Device>()->VSSetConstantBuffers(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}

void VertexShaderPipelineStage::SetSamplers(UInt32 startSlot, IEnumerable<SamplerState^>^ samplers)
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

    Parent->CastInterface<ID3D10Device>()->VSSetSamplers(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}

void VertexShaderPipelineStage::Shader::set(VertexShader^ vertexShader)
{
    Parent->CastInterface<ID3D10Device>()->VSSetShader(
        vertexShader == nullptr ? NULL : vertexShader->CastInterface<ID3D10VertexShader>());
}

void VertexShaderPipelineStage::SetShaderResources(UInt32 startSlot, IEnumerable<ShaderResourceView^>^ shaderResourceViews)
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

    Parent->CastInterface<ID3D10Device>()->VSSetShaderResources(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}
