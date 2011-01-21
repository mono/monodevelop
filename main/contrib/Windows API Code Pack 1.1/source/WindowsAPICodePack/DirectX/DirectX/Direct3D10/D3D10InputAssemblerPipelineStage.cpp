// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10InputAssemblerPipelineStage.h"
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

Direct3D10::IndexBuffer InputAssemblerPipelineStage::IndexBuffer::get(void)
{
    ID3D10Buffer* tempBuffer;
    DXGI_FORMAT tempFormat;
    UINT tempOffset;
    
    Parent->CastInterface<ID3D10Device>()->IAGetIndexBuffer(&tempBuffer, &tempFormat, &tempOffset);
        
    return Direct3D10::IndexBuffer(tempBuffer == NULL ? nullptr : gcnew D3DBuffer(tempBuffer),
        static_cast<Format>(tempFormat), tempOffset);
}

InputLayout^ InputAssemblerPipelineStage::InputLayout::get(void)
{
    ID3D10InputLayout* tempoutInputLayout;
    Parent->CastInterface<ID3D10Device>()->IAGetInputLayout(&tempoutInputLayout);

    return tempoutInputLayout == NULL ? nullptr : gcnew Direct3D10::InputLayout(tempoutInputLayout);
}

PrimitiveTopology InputAssemblerPipelineStage::PrimitiveTopology::get(void)
{
    D3D10_PRIMITIVE_TOPOLOGY tempoutTopology;
    Parent->CastInterface<ID3D10Device>()->IAGetPrimitiveTopology(&tempoutTopology);
    
    return static_cast<Direct3D10::PrimitiveTopology>(tempoutTopology);
}

 ReadOnlyCollection<VertexBuffer>^ InputAssemblerPipelineStage::GetVertexBuffers(UInt32 startSlot, UInt32 bufferCount)
{
    vector<ID3D10Buffer*> buffers(bufferCount);
    vector<UINT> strides(bufferCount);
    vector<UINT> offsets(bufferCount);

    Parent->CastInterface<ID3D10Device>()->IAGetVertexBuffers(
        static_cast<UINT>(startSlot), 
        static_cast<UINT>(bufferCount), 
        bufferCount > 0 ? &buffers[0] : NULL,
        bufferCount > 0 ? &strides[0] : NULL, 
        bufferCount > 0 ? &offsets[0] : NULL);

    array<VertexBuffer>^ bufferList = gcnew array<VertexBuffer>(bufferCount);

    for (UInt32 index = 0; index < bufferCount; index++)
    {
        bufferList[index] = VertexBuffer(
            buffers[index] != NULL ? gcnew D3DBuffer(buffers[index]) : nullptr,
            strides[index], offsets[index]);
    }

    return Array::AsReadOnly(bufferList);
}

 void InputAssemblerPipelineStage::IndexBuffer::set(Direct3D10::IndexBuffer buffer)
{
    Parent->CastInterface<ID3D10Device>()->IASetIndexBuffer(
        buffer.Buffer != nullptr ? buffer.Buffer->CastInterface<ID3D10Buffer>() : NULL, 
        static_cast<DXGI_FORMAT>(buffer.Format), static_cast<UINT>(buffer.Offset));
}

void InputAssemblerPipelineStage::InputLayout::set(Direct3D10::InputLayout^ inputLayout)
{
    Parent->CastInterface<ID3D10Device>()->IASetInputLayout(
        inputLayout ? inputLayout->CastInterface<ID3D10InputLayout>() : NULL);
}

void InputAssemblerPipelineStage::PrimitiveTopology::set(Direct3D10::PrimitiveTopology topology)
{
    Parent->CastInterface<ID3D10Device>()->IASetPrimitiveTopology(static_cast<D3D10_PRIMITIVE_TOPOLOGY>(topology));
}

void InputAssemblerPipelineStage::SetVertexBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ vertexBuffers, array<UInt32>^ strides, array<UInt32>^ offsets)
{
    if (Object::ReferenceEquals(vertexBuffers, nullptr))
    {
        throw gcnew ArgumentNullException("vertexBuffers");
    }

    if (Object::ReferenceEquals(strides, nullptr))
    {
        throw gcnew ArgumentNullException("strides");
    }

    if (Object::ReferenceEquals(offsets, nullptr))
    {
        throw gcnew ArgumentNullException("offsets");
    }

    vector<ID3D10Buffer*> itemsVector;
    UINT count = Utilities::Convert::FillIUnknownsVector<D3DBuffer, ID3D10Buffer>(vertexBuffers, itemsVector);

    if (count == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "vertexBuffers");
    }

    if (count != static_cast<UINT>(strides->Length) || count != static_cast<UINT>(offsets->Length))
    {
        throw gcnew InvalidOperationException("Invalid array lengths; vertexBuffers, strides and offsets sizes must be equal.");
    }

    pin_ptr<UINT> stridesArray = &strides[0];
    pin_ptr<UINT> offsetsArray = &offsets[0];

    Parent->CastInterface<ID3D10Device>()->IASetVertexBuffers(
        static_cast<UINT>(startSlot), count, &itemsVector[0], stridesArray, offsetsArray);
}

void InputAssemblerPipelineStage::SetVertexBuffers(UInt32 startSlot, IEnumerable<VertexBuffer>^ vertexBuffers)
{
    IList<D3DBuffer^>^ buffers = gcnew List<D3DBuffer^>();

    for each (VertexBuffer buffer in vertexBuffers)
    {
        buffers->Add(buffer.Buffer);
    }

    array<UInt32>^ strides = gcnew array<UInt32>(buffers->Count);
    array<UInt32>^ offsets = gcnew array<UInt32>(buffers->Count);

    int index = 0;
    IEnumerator<VertexBuffer>^ bufferEnum = vertexBuffers->GetEnumerator();

    while (bufferEnum->MoveNext())
    {
        strides[index] = bufferEnum->Current.Stride;
        offsets[index] = bufferEnum->Current.Offset;
        index++;
    }

    SetVertexBuffers(startSlot, buffers, strides, offsets);
}