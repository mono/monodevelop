// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11ComputeShaderPipelineStage.h"

#include "D3D11UnorderedAccessView.h"
#include "D3D11Buffer.h"
#include "D3D11SamplerState.h"
#include "D3D11ClassInstance.h"
#include "D3D11ShaderResourceView.h"
#include "D3D11ComputeShader.h"

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

ReadOnlyCollection<SamplerState^>^ ComputeShaderPipelineStage::GetSamplers(UInt32 startSlot, UInt32 samplerCount)
{
    vector<ID3D11SamplerState*> tempoutSamplers(samplerCount);
    Parent->CastInterface<ID3D11DeviceContext>()->CSGetSamplers(static_cast<UINT>(startSlot), static_cast<UINT>(samplerCount), &tempoutSamplers[0]);

    return Utilities::Convert::GetCollection<SamplerState, ID3D11SamplerState>(samplerCount, tempoutSamplers);
}

void ComputeShaderPipelineStage::SetSamplers(UInt32 startSlot, IEnumerable<SamplerState^>^ samplers)
{
    // Compiler crashes with "samplers == nullptr" or "nullptr == samplers"
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

    Parent->CastInterface<ID3D11DeviceContext>()->CSSetSamplers(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}

ReadOnlyCollection<D3DBuffer^>^ ComputeShaderPipelineStage::GetConstantBuffers(UInt32 startSlot, UInt32 bufferCount)
{
    vector<ID3D11Buffer*> tempoutConstantBuffers(bufferCount);
    Parent->CastInterface<ID3D11DeviceContext>()->CSGetConstantBuffers(static_cast<UINT>(startSlot), static_cast<UINT>(bufferCount), &tempoutConstantBuffers[0]);
        
    return Utilities::Convert::GetCollection<D3DBuffer, ID3D11Buffer>(bufferCount, tempoutConstantBuffers);
}

void ComputeShaderPipelineStage::SetConstantBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ constantBuffers)
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

    Parent->CastInterface<ID3D11DeviceContext>()->CSSetConstantBuffers(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}


ShaderAndClasses<ComputeShader^> ComputeShaderPipelineStage::GetShaderAndClasses(UInt32 classInstanceCountMax)
{
    ID3D11ComputeShader* computeShader = NULL;

    UINT classInstanceCount = static_cast<UINT>(classInstanceCountMax);
    vector<ID3D11ClassInstance*> classInstanceVector(classInstanceCountMax); 

    Parent->CastInterface<ID3D11DeviceContext>()->CSGetShader(
        &computeShader, 
        classInstanceCount > 0 ? &classInstanceVector[0] : NULL,
        &classInstanceCount
        );

    return ShaderAndClasses<ComputeShader^>(computeShader == NULL ? nullptr : gcnew ComputeShader(computeShader),
        classInstanceCount > 0 ? Utilities::Convert::GetCollection<ClassInstance, ID3D11ClassInstance>(
        classInstanceCount, classInstanceVector) : nullptr);
}


ComputeShader^ ComputeShaderPipelineStage::Shader::get(void)
{
    ID3D11ComputeShader* tempoutComputeShader = NULL;

    UINT tempoutNumClassInstances = 0;

    Parent->CastInterface<ID3D11DeviceContext>()->CSGetShader(
        &tempoutComputeShader, 
        NULL,
        &tempoutNumClassInstances
        );

    return tempoutComputeShader == NULL ? nullptr : gcnew ComputeShader(tempoutComputeShader);
}

void ComputeShaderPipelineStage::SetShader(ComputeShader^ shader, IEnumerable<ClassInstance^>^ classInstances)
{
    vector<ID3D11ClassInstance*> classInstancesVector;

    UINT count = classInstances == nullptr ? 0 :
        Utilities::Convert::FillIUnknownsVector<ClassInstance,ID3D11ClassInstance>(classInstances, classInstancesVector);

    Parent->CastInterface<ID3D11DeviceContext>()->CSSetShader(
        shader == nullptr ? NULL : shader->CastInterface<ID3D11ComputeShader>(), 
        count == 0 ? NULL : &(classInstancesVector[0]), 
        count);
}

void ComputeShaderPipelineStage::SetShader(ShaderAndClasses<ComputeShader^> shaderAndClasses)
{
    SetShader(shaderAndClasses.Shader, shaderAndClasses.Classes);
}

void ComputeShaderPipelineStage::Shader::set(ComputeShader^ shader)
{
    SetShader(shader, nullptr);
}

ReadOnlyCollection<ShaderResourceView^>^ ComputeShaderPipelineStage::GetShaderResources(UInt32 startSlot, UInt32 viewCount)
{
    vector<ID3D11ShaderResourceView*> tempoutShaderResourceViews(viewCount);
    Parent->CastInterface<ID3D11DeviceContext>()->CSGetShaderResources(static_cast<UINT>(startSlot), static_cast<UINT>(viewCount), &tempoutShaderResourceViews[0]);
    
    return Utilities::Convert::GetCollection<ShaderResourceView, ID3D11ShaderResourceView>(viewCount, tempoutShaderResourceViews);
}

void ComputeShaderPipelineStage::SetShaderResources(UInt32 startSlot, IEnumerable<ShaderResourceView^>^ shaderResourceViews)
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

    Parent->CastInterface<ID3D11DeviceContext>()->CSSetShaderResources(
        static_cast<UINT>(startSlot), count, &itemsVector[0]);
}

ReadOnlyCollection<UnorderedAccessView^>^ ComputeShaderPipelineStage::GetUnorderedAccessViews(UInt32 startSlot, UInt32 viewCount)
{
    vector<ID3D11UnorderedAccessView*> views(viewCount);

    Parent->CastInterface<ID3D11DeviceContext>()->CSGetUnorderedAccessViews(
        static_cast<UINT>(startSlot), static_cast<UINT>(viewCount), &views[0]);
    
    return Utilities::Convert::GetCollection<UnorderedAccessView, ID3D11UnorderedAccessView>(viewCount, views);
}

void ComputeShaderPipelineStage::SetUnorderedAccessViews(UInt32 startSlot, IEnumerable<UnorderedAccessView^>^ unorderedAccessViews, array<UInt32>^ initialCounts)
{
    if (Object::ReferenceEquals(unorderedAccessViews, nullptr))
    {
        throw gcnew ArgumentNullException("unorderedAccessViews");
    }

    if (Object::ReferenceEquals(initialCounts, nullptr))
    {
        throw gcnew ArgumentNullException("initialCounts");
    }

    vector<ID3D11UnorderedAccessView*> itemsVector;
    UINT count = Utilities::Convert::FillIUnknownsVector<UnorderedAccessView, ID3D11UnorderedAccessView>(unorderedAccessViews, itemsVector);

    // REVIEW: DX headers suggest 0 is a valid number of views to set; but the docs
    // don't mention 0/NULL as permissible values. /analysis warning C6387 is emitted
    // for the call to CSSetUnorderedAccessViews(), hence the range-checking. But if
    // it turns out to be important to allow a caller to set zero views, we'll have to
    // suppress the warning with #pragmas here (or pass a potentially zero-length array),
    // since the DX headers don't match the desired behavior.

    if (count == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "unorderedAccessViews");
    }

    if (count != static_cast<UINT>(initialCounts->Length))
    {
        throw gcnew InvalidOperationException("Both unorderedAccessViews and initialCounts sizes must be equal.");
    }

    pin_ptr<UINT> countArray = &initialCounts[0];

    Parent->CastInterface<ID3D11DeviceContext>()->CSSetUnorderedAccessViews(
        static_cast<UINT>(startSlot), count, &itemsVector[0], countArray);
}