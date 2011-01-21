// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10OutputMergerPipelineStage.h"
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

OutputMergerBlendState^ OutputMergerPipelineStage::BlendState::get()
{
    ID3D10BlendState *blendState;
    vector<FLOAT> blendFactor(4);
    UINT sampleMask;

    Parent->CastInterface<ID3D10Device>()->OMGetBlendState(&blendState, &blendFactor[0], &sampleMask);
    
    return gcnew OutputMergerBlendState(blendState == NULL ? nullptr : gcnew Direct3D10::BlendState(blendState),
         ColorRgba(&blendFactor[0]), sampleMask);
}

void OutputMergerPipelineStage::BlendState::set(OutputMergerBlendState^ mergerBlendState)
{
    ID3D10BlendState *blendState = mergerBlendState->BlendState != nullptr ?
        mergerBlendState->BlendState->CastInterface<ID3D10BlendState>() : NULL;
    ColorRgba blendFactor = mergerBlendState->BlendFactor;

    FLOAT blendArray[] = { blendFactor.Red, blendFactor.Green, blendFactor.Blue, blendFactor.Alpha };

    Parent->CastInterface<ID3D10Device>()->OMSetBlendState(
        blendState, blendArray, static_cast<UINT>(mergerBlendState->SampleMask));
}

DepthStencilState^ OutputMergerPipelineStage::GetDepthStencilStateAndReferenceValue([System::Runtime::InteropServices::Out] UInt32 %outStencilRef)
{
    ID3D10DepthStencilState* tempoutDepthStencilState;
    UINT tempoutStencilRef;

    Parent->CastInterface<ID3D10Device>()->OMGetDepthStencilState(&tempoutDepthStencilState, &tempoutStencilRef);

    outStencilRef = tempoutStencilRef;

    return tempoutDepthStencilState == NULL ? nullptr : gcnew DepthStencilState(tempoutDepthStencilState);
}

OutputMergerRenderTargets^ OutputMergerPipelineStage::RenderTargets::get(void)
{
    vector<ID3D10RenderTargetView*> renderTargetsVector(D3D10_SIMULTANEOUS_RENDER_TARGET_COUNT);
    ID3D10DepthStencilView* depthStencilView;
    
    Parent->CastInterface<ID3D10Device>()->OMGetRenderTargets(D3D10_SIMULTANEOUS_RENDER_TARGET_COUNT, &renderTargetsVector[0], &depthStencilView);

	return gcnew OutputMergerRenderTargets(
		Utilities::Convert::GetTrimmedCollection<RenderTargetView, ID3D10RenderTargetView>(NULL, renderTargetsVector),
		depthStencilView == NULL ? nullptr : gcnew Direct3D10::DepthStencilView(depthStencilView));
}

 DepthStencilView^ OutputMergerPipelineStage::DepthStencilView::get(void)
{
    ID3D10DepthStencilView* tempoutDepthStencilView;
    
    Parent->CastInterface<ID3D10Device>()->OMGetRenderTargets(0, NULL, &tempoutDepthStencilView);
    
    return tempoutDepthStencilView == NULL ? nullptr : gcnew Direct3D10::DepthStencilView(tempoutDepthStencilView);
}

void OutputMergerPipelineStage::SetDepthStencilStateAndReferenceValue(DepthStencilState^ depthStencilState, UInt32 stencilRef)
{
    Parent->CastInterface<ID3D10Device>()->OMSetDepthStencilState(
        depthStencilState ? depthStencilState->CastInterface<ID3D10DepthStencilState>() : NULL, 
        stencilRef);
}

void OutputMergerPipelineStage::RenderTargets::set(OutputMergerRenderTargets^ renderTargets)
{
	IEnumerable<RenderTargetView^>^ renderTargetViews = renderTargets->RenderTargetViews;
	Direct3D10::DepthStencilView^ depthStencilView = renderTargets->DepthStencilView;
	vector<ID3D10RenderTargetView*> itemsVector;

    UINT count = Utilities::Convert::FillIUnknownsVector<RenderTargetView, ID3D10RenderTargetView>(renderTargetViews, itemsVector);

    Parent->CastInterface<ID3D10Device>()->OMSetRenderTargets(
        count, 
        count == 0 ? NULL : &itemsVector[0], 
        depthStencilView == nullptr ? NULL : depthStencilView->CastInterface<ID3D10DepthStencilView>());
}