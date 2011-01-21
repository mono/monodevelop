// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10StreamOutputPipelineStage.h"

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

IEnumerable<OutputBuffer>^ StreamOutputPipelineStage::Targets::get(void)
{
    vector<ID3D10Buffer*> tempTargets(maximumOutputBufferCount);
    vector<UINT> offsets(maximumOutputBufferCount);

    Parent->CastInterface<ID3D10Device>()->SOGetTargets(
        static_cast<UINT>(maximumOutputBufferCount), &tempTargets[0], &offsets[0]);

    array<OutputBuffer>^ outputBuffers = gcnew array<OutputBuffer>(maximumOutputBufferCount);

    for (int index = 0; index < maximumOutputBufferCount; index++)
    {
        outputBuffers[index] = OutputBuffer(
            tempTargets[index] != NULL ? gcnew D3DBuffer(tempTargets[index]) : nullptr,
            offsets[index]);
    }

    return Array::AsReadOnly(outputBuffers);
}

void StreamOutputPipelineStage::Targets::set(IEnumerable<OutputBuffer>^ targets)
{
    int count = Enumerable::Count(targets);
    vector<ID3D10Buffer*> itemsVector(count);
    vector<UINT> offsets(count);

    int index = 0;
    IEnumerator<OutputBuffer>^ targetsEnum = targets->GetEnumerator();

    while (targetsEnum->MoveNext())
    {
        itemsVector[index] = targetsEnum->Current.Buffer->CastInterface<ID3D10Buffer>();
        offsets[index] = targetsEnum->Current.Offset;
        index++;
    }

    Parent->CastInterface<ID3D10Device>()->SOSetTargets(
        count, 
        count > 0 ? &itemsVector[0] : NULL,
        count > 0 ? &offsets[0] : NULL);
}

