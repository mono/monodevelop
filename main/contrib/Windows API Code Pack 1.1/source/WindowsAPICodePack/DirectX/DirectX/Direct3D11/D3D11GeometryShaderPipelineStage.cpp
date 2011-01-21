// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11GeometryShaderPipelineStage.h"

#include "D3D11Buffer.h"
#include "D3D11SamplerState.h"
#include "D3D11ClassInstance.h"
#include "D3D11ShaderResourceView.h"
#include "D3D11GeometryShader.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

ReadOnlyCollection<D3DBuffer^>^ GeometryShaderPipelineStage::GetConstantBuffers(UInt32 startSlot, UInt32 bufferCount)
{
    vector<ID3D11Buffer*> tempoutConstantBuffers(bufferCount);
    Parent->CastInterface<ID3D11DeviceContext>()->GSGetConstantBuffers(static_cast<UINT>(startSlot), static_cast<UINT>(bufferCount), &tempoutConstantBuffers[0]);
        
    return Utilities::Convert::GetCollection<D3DBuffer, ID3D11Buffer>(bufferCount, tempoutConstantBuffers);
}

ReadOnlyCollection<SamplerState^>^ GeometryShaderPipelineStage::GetSamplers(UInt32 startSlot, UInt32 samplerCount)
{
    vector<ID3D11SamplerState*> tempoutSamplers(samplerCount);
    Parent->CastInterface<ID3D11DeviceContext>()->GSGetSamplers(static_cast<UINT>(startSlot), static_cast<UINT>(samplerCount), &tempoutSamplers[0]);

    return Utilities::Convert::GetCollection<SamplerState, ID3D11SamplerState>(samplerCount, tempoutSamplers);
}

ShaderAndClasses<GeometryShader^> GeometryShaderPipelineStage::GetShaderAndClasses(UInt32 classInstanceCountMax)
{
    ID3D11GeometryShader* tempShader = NULL;

    UINT classInstanceCount = static_cast<UINT>(classInstanceCountMax);
    vector<ID3D11ClassInstance*> classInstanceVector(classInstanceCount); 

    Parent->CastInterface<ID3D11DeviceContext>()->GSGetShader(
        &tempShader, 
        classInstanceCount > 0 ? &classInstanceVector[0] : NULL,
        &classInstanceCount);

    return ShaderAndClasses<GeometryShader^>(tempShader == NULL ? nullptr : gcnew GeometryShader(tempShader),
        classInstanceCount > 0 ? Utilities::Convert::GetCollection<ClassInstance, ID3D11ClassInstance>(
        classInstanceCount, classInstanceVector) : nullptr);
}


GeometryShader^ GeometryShaderPipelineStage::Shader::get(void)
{
    ID3D11GeometryShader* tempShader = NULL;

    UINT tempoutNumClassInstances = 0;

    Parent->CastInterface<ID3D11DeviceContext>()->GSGetShader(
        &tempShader, 
        NULL,
        &tempoutNumClassInstances);

    return tempShader == NULL ? nullptr : gcnew GeometryShader(tempShader);
}

ReadOnlyCollection<ShaderResourceView^>^ GeometryShaderPipelineStage::GetShaderResources(UInt32 startSlot, UInt32 viewCount)
{
    vector <ID3D11ShaderResourceView*> tempoutShaderResourceViews(viewCount);
    Parent->CastInterface<ID3D11DeviceContext>()->GSGetShaderResources(static_cast<UINT>(startSlot), static_cast<UINT>(viewCount), &tempoutShaderResourceViews[0]);
    
    return Utilities::Convert::GetCollection<ShaderResourceView, ID3D11ShaderResourceView>(viewCount, tempoutShaderResourceViews);
}

void GeometryShaderPipelineStage::SetConstantBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ constantBuffers)
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

    Parent->CastInterface<ID3D11DeviceContext>()->GSSetConstantBuffers(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}

void GeometryShaderPipelineStage::SetSamplers(UInt32 startSlot, IEnumerable<SamplerState^>^ samplers)
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

    Parent->CastInterface<ID3D11DeviceContext>()->GSSetSamplers(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}

void GeometryShaderPipelineStage::SetShader(GeometryShader^ shader, IEnumerable<ClassInstance^>^ classInstances)
{
    vector<ID3D11ClassInstance*> classInstancesVector;

    UINT count = classInstances == nullptr ? 0 :
        Utilities::Convert::FillIUnknownsVector<ClassInstance,ID3D11ClassInstance>(classInstances, classInstancesVector);

    Parent->CastInterface<ID3D11DeviceContext>()->GSSetShader(
        shader == nullptr ? NULL : shader->CastInterface<ID3D11GeometryShader>(), 
        count == 0 ? NULL : &(classInstancesVector[0]), 
        count);
}

void GeometryShaderPipelineStage::SetShader(ShaderAndClasses<GeometryShader^> shaderAndClasses)
{
    SetShader(shaderAndClasses.Shader, shaderAndClasses.Classes);
}

void GeometryShaderPipelineStage::Shader::set(GeometryShader^ shader)
{
    SetShader(shader, nullptr);
}

void GeometryShaderPipelineStage::SetShaderResources(UInt32 startSlot, IEnumerable<ShaderResourceView^>^ shaderResourceViews)
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

    Parent->CastInterface<ID3D11DeviceContext>()->GSSetShaderResources(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}

