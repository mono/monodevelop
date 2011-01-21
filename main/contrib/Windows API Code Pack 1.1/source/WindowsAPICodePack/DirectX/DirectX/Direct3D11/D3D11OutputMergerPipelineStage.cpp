// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11OutputMergerPipelineStage.h"

#include "D3D11BlendState.h"
#include "D3D11DepthStencilState.h"
#include "D3D11DepthStencilView.h"
#include "D3D11RenderTargetView.h"
#include "D3D11UnorderedAccessView.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

OutputMergerBlendState^ OutputMergerPipelineStage::BlendState::get()
{
    ID3D11BlendState *blendState;
    vector<FLOAT> blendFactor(4);
    UINT sampleMask;

    Parent->CastInterface<ID3D11DeviceContext>()->OMGetBlendState(&blendState, &blendFactor[0], &sampleMask);
    
    return gcnew OutputMergerBlendState(blendState == NULL ? nullptr : gcnew Direct3D11::BlendState(blendState),
         ColorRgba(&blendFactor[0]), sampleMask);
}

void OutputMergerPipelineStage::BlendState::set(OutputMergerBlendState^ mergerBlendState)
{
    ID3D11BlendState *blendState = mergerBlendState->BlendState != nullptr ?
        mergerBlendState->BlendState->CastInterface<ID3D11BlendState>() : NULL;
    ColorRgba blendFactor = mergerBlendState->BlendFactor;

    FLOAT blendArray[] = { blendFactor.Red, blendFactor.Green, blendFactor.Blue, blendFactor.Alpha };

    Parent->CastInterface<ID3D11DeviceContext>()->OMSetBlendState(
        blendState, blendArray, static_cast<UINT>(mergerBlendState->SampleMask));
}

Direct3D11::DepthStencilState^ OutputMergerPipelineStage::GetDepthStencilStateAndReferenceValue([System::Runtime::InteropServices::Out] UInt32 %outStencilRef)
{
    ID3D11DepthStencilState* tempoutDepthStencilState;
    UINT tempoutStencilRef;

    Parent->CastInterface<ID3D11DeviceContext>()->OMGetDepthStencilState(&tempoutDepthStencilState, &tempoutStencilRef);

    outStencilRef = tempoutStencilRef;

    return tempoutDepthStencilState == NULL ? nullptr : gcnew Direct3D11::DepthStencilState(tempoutDepthStencilState);
}


void OutputMergerPipelineStage::SetDepthStencilStateAndReferenceValue(Direct3D11::DepthStencilState^ depthStencilState, UInt32 stencilRef)
{
    Parent->CastInterface<ID3D11DeviceContext>()->OMSetDepthStencilState(
        depthStencilState ? depthStencilState->CastInterface<ID3D11DepthStencilState>() : NULL, 
        stencilRef);
}

Direct3D11::DepthStencilState^ OutputMergerPipelineStage::DepthStencilState::get(void)
{
    ID3D11DepthStencilState* tempoutDepthStencilState;
    UINT tempoutStencilRef;

    Parent->CastInterface<ID3D11DeviceContext>()->OMGetDepthStencilState(&tempoutDepthStencilState, &tempoutStencilRef);

    return tempoutDepthStencilState == NULL ? nullptr : gcnew Direct3D11::DepthStencilState(tempoutDepthStencilState);
}

DepthStencilView^ OutputMergerPipelineStage::DepthStencilView::get(void)
{
    ID3D11DepthStencilView* tempoutDepthStencilView;
    
    Parent->CastInterface<ID3D11DeviceContext>()->OMGetRenderTargets(0, NULL, &tempoutDepthStencilView);
    
    return tempoutDepthStencilView == NULL ? nullptr : gcnew Direct3D11::DepthStencilView(tempoutDepthStencilView);
}

OutputMergerRenderTargets^ OutputMergerPipelineStage::RenderTargets::get(void)
{
    vector<ID3D11RenderTargetView*> renderTargetViews(D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT);
    ID3D11DepthStencilView *depthStencilView;
    vector<ID3D11UnorderedAccessView*> unorderedAccessViews(D3D11_PS_CS_UAV_REGISTER_COUNT);

    Parent->CastInterface<ID3D11DeviceContext>()->OMGetRenderTargetsAndUnorderedAccessViews(
        D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT, &renderTargetViews[0], &depthStencilView,
        0, D3D11_PS_CS_UAV_REGISTER_COUNT, &unorderedAccessViews[0]);

    ReadOnlyCollection<RenderTargetView^>^ views = Utilities::Convert::GetTrimmedCollection<RenderTargetView, ID3D11RenderTargetView>(NULL, renderTargetViews);
    ReadOnlyCollection<UnorderedAccessView^>^ uavs = Utilities::Convert::GetTrimmedCollection<UnorderedAccessView, ID3D11UnorderedAccessView>(NULL, unorderedAccessViews);

    return gcnew OutputMergerRenderTargets(
        views->Count > 0 ? views : nullptr,
        depthStencilView != NULL ? gcnew Direct3D11::DepthStencilView(depthStencilView) : nullptr,
        0, uavs->Count > 0 ? uavs : nullptr);
}

void OutputMergerPipelineStage::RenderTargets::set(OutputMergerRenderTargets^ renderTargets)
{
    if (renderTargets->UnorderedAccessViews == nullptr)
    {
        SetRenderTargets(renderTargets->RenderTargetViews, renderTargets->DepthStencilView);
    }
    else
    {
        SetRenderTargetsAndUnorderedAccessViews(renderTargets->RenderTargetViews,
            renderTargets->DepthStencilView, renderTargets->UnorderedViewStartSlot,
            renderTargets->UnorderedAccessViews, renderTargets->InitialCounts);
    }
}

void OutputMergerPipelineStage::SetRenderTargets(IEnumerable<RenderTargetView^>^ renderTargetViews, Direct3D11::DepthStencilView^ depthStencilView)
{
    vector<ID3D11RenderTargetView*> itemsVector;

    UINT count = Utilities::Convert::FillIUnknownsVector<RenderTargetView, ID3D11RenderTargetView>(renderTargetViews, itemsVector);

    Parent->CastInterface<ID3D11DeviceContext>()->OMSetRenderTargets(
        count, 
        count == 0 ? NULL : &itemsVector[0], 
        depthStencilView == nullptr ? NULL : depthStencilView->CastInterface<ID3D11DepthStencilView>());
}

void OutputMergerPipelineStage::SetRenderTargetsAndUnorderedAccessViews(
    IEnumerable<RenderTargetView^>^ renderTargetViews, Direct3D11::DepthStencilView^ depthStencilView, UInt32 viewStartSlot, 
    IEnumerable<UnorderedAccessView^>^ unorderedAccessViews, IEnumerable<UInt32>^ initialCounts)
{
    if ((unorderedAccessViews != nullptr && initialCounts == nullptr) || 
        (unorderedAccessViews == nullptr && initialCounts != nullptr))
    {
        throw gcnew InvalidOperationException("Both unorderedAccessViews and initialCounts must be set together. Neither one can be null if the other is not null.");        
    }
    

    vector<ID3D11RenderTargetView*> itemsVector;
    UINT count = Utilities::Convert::FillIUnknownsVector<RenderTargetView, ID3D11RenderTargetView>(renderTargetViews, itemsVector);

    if (unorderedAccessViews != nullptr && (count != static_cast<UINT>(Enumerable::Count(initialCounts))))
    {
        throw gcnew InvalidOperationException("Both unorderedAccessViews and initialCounts sizes must be equal.");
    }
    vector<ID3D11UnorderedAccessView*> UAVVector;
    UINT UAVcount = Utilities::Convert::FillIUnknownsVector<UnorderedAccessView, ID3D11UnorderedAccessView>(unorderedAccessViews, UAVVector);

    array<UInt32>^ initialCountsArray = dynamic_cast<array<UInt32>^>(initialCounts);

    if (initialCountsArray == nullptr)
    {
        initialCountsArray = Enumerable::ToArray(initialCounts);
    }

    pin_ptr<UINT> countArray;

    if (UAVcount > 0) 
        countArray = &initialCountsArray[0];
    else
        countArray = nullptr;

    Parent->CastInterface<ID3D11DeviceContext>()->OMSetRenderTargetsAndUnorderedAccessViews(
        count, 
        count == 0 ? NULL : &itemsVector[0], 
        depthStencilView == nullptr ? NULL : depthStencilView->CastInterface<ID3D11DepthStencilView>(),
        static_cast<UINT>(viewStartSlot), 
        UAVcount,
        UAVcount == 0 ? NULL : &UAVVector[0],
        countArray);
}

