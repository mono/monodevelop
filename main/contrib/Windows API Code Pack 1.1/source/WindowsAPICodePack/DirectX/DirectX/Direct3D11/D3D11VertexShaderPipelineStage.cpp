// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11VertexShaderPipelineStage.h"

#include "D3D11Buffer.h"
#include "D3D11SamplerState.h"
#include "D3D11ClassInstance.h"
#include "D3D11ShaderResourceView.h"
#include "D3D11VertexShader.h"

#include <vector>
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

ReadOnlyCollection<D3DBuffer^>^ VertexShaderPipelineStage::GetConstantBuffers(UInt32 startSlot, UInt32 bufferCount)
{
    vector<ID3D11Buffer*> items(bufferCount);

    Parent->CastInterface<ID3D11DeviceContext>()->VSGetConstantBuffers(static_cast<UINT>(startSlot), static_cast<UINT>(bufferCount), &(items[0]));

    return Utilities::Convert::GetCollection<D3DBuffer, ID3D11Buffer>(bufferCount, items);
}

ReadOnlyCollection<SamplerState^>^ VertexShaderPipelineStage::GetSamplers(UInt32 startSlot, UInt32 samplerCount)
{
    vector<ID3D11SamplerState*> samplersVector(samplerCount);
    Parent->CastInterface<ID3D11DeviceContext>()->VSGetSamplers(static_cast<UINT>(startSlot), static_cast<UINT>(samplerCount), &samplersVector[0]);

    return Utilities::Convert::GetCollection<SamplerState, ID3D11SamplerState>(samplerCount, samplersVector);
}

ShaderAndClasses<VertexShader^> VertexShaderPipelineStage::GetShaderAndClasses(UInt32 classInstanceCountMax)
{
    ID3D11VertexShader* tempShader = NULL;

    UINT classInstanceCount = static_cast<UINT>(classInstanceCountMax);
    vector<ID3D11ClassInstance*> classInstanceVector(classInstanceCount); 

    Parent->CastInterface<ID3D11DeviceContext>()->VSGetShader(
        &tempShader,
        classInstanceCount > 0 ? &classInstanceVector[0] : NULL,
        &classInstanceCount);

    return ShaderAndClasses<VertexShader^>(tempShader == NULL ? nullptr : gcnew VertexShader(tempShader),
        classInstanceCount > 0 ? Utilities::Convert::GetCollection<ClassInstance, ID3D11ClassInstance>(
        classInstanceCount, classInstanceVector) : nullptr);

}

VertexShader^ VertexShaderPipelineStage::Shader::get(void)
{
    ID3D11VertexShader* tempShader = NULL;

    UINT numInstances = 0;

    Parent->CastInterface<ID3D11DeviceContext>()->VSGetShader(
        &tempShader, 
        NULL, 
        &numInstances);

    return tempShader == NULL ? nullptr : gcnew VertexShader(tempShader);
}

ReadOnlyCollection<ShaderResourceView^>^ VertexShaderPipelineStage::GetShaderResources(UInt32 startSlot, UInt32 viewCount)
{
    vector<ID3D11ShaderResourceView*> tempoutShaderResourceViews(viewCount);
    Parent->CastInterface<ID3D11DeviceContext>()->VSGetShaderResources(static_cast<UINT>(startSlot), static_cast<UINT>(viewCount), &tempoutShaderResourceViews[0]);
    
    return Utilities::Convert::GetCollection<ShaderResourceView, ID3D11ShaderResourceView>(viewCount, tempoutShaderResourceViews);
}

void VertexShaderPipelineStage::SetConstantBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ constantBuffers)
{
    if (Object::ReferenceEquals(constantBuffers, nullptr))
    {
        throw gcnew ArgumentNullException("constantBuffers");
    }

    vector<ID3D11Buffer*> itemsVector;

    UINT count = Utilities::Convert::FillIUnknownsVector<D3DBuffer, ID3D11Buffer>(constantBuffers, itemsVector);

    if (count == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "constantBuffers");
    }

    Parent->CastInterface<ID3D11DeviceContext>()->VSSetConstantBuffers(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}

void VertexShaderPipelineStage::SetSamplers(UInt32 startSlot, IEnumerable<SamplerState^>^ samplers)
{
    if (Object::ReferenceEquals(samplers, nullptr))
    {
        throw gcnew ArgumentNullException("samplers");
    }

    vector<ID3D11SamplerState*> itemsVector;

    UINT count = Utilities::Convert::FillIUnknownsVector<SamplerState, ID3D11SamplerState>(samplers, itemsVector);

    if (count == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "samplers");
    }

    Parent->CastInterface<ID3D11DeviceContext>()->VSSetSamplers(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}

void VertexShaderPipelineStage::SetShader(VertexShader^ shader, IEnumerable<ClassInstance^>^ classInstances)
{
    vector<ID3D11ClassInstance*> classInstancesVector;

    UINT count = classInstances == nullptr ? 0 :
        Utilities::Convert::FillIUnknownsVector<ClassInstance,ID3D11ClassInstance>(classInstances, classInstancesVector);

    Parent->CastInterface<ID3D11DeviceContext>()->VSSetShader(
        shader == nullptr ? NULL : shader->CastInterface<ID3D11VertexShader>(), 
        count == 0 ? NULL : &(classInstancesVector[0]), 
        count);
}

void VertexShaderPipelineStage::SetShader(ShaderAndClasses<VertexShader^> shaderAndClasses)
{
    SetShader(shaderAndClasses.Shader, shaderAndClasses.Classes);
}

void VertexShaderPipelineStage::Shader::set(VertexShader^ shader)
{
    SetShader(shader, nullptr);
}

void VertexShaderPipelineStage::SetShaderResources(UInt32 startSlot, IEnumerable<ShaderResourceView^>^ shaderResourceViews)
{
    if (Object::ReferenceEquals(shaderResourceViews, nullptr))
    {
        throw gcnew ArgumentNullException("shaderResourceViews");
    }

    vector<ID3D11ShaderResourceView*> itemsVector;

    UINT count = Utilities::Convert::FillIUnknownsVector<ShaderResourceView, ID3D11ShaderResourceView>(shaderResourceViews, itemsVector);

    if (count == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "shaderResourceViews");
    }

    Parent->CastInterface<ID3D11DeviceContext>()->VSSetShaderResources(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}
