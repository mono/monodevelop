// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11RasterizerPipelineStage.h"

#include "D3D11RasterizerState.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

IEnumerable<D3DRect>^ RasterizerPipelineStage::BoundedGetScissorRects(UInt32 maxNumberOfRects)
{
    UINT tempoutNumRects = static_cast<UINT>(maxNumberOfRects);
    vector<D3D11_RECT> rects(maxNumberOfRects);

    Parent->CastInterface<ID3D11DeviceContext>()->RSGetScissorRects(&tempoutNumRects, &rects[0]);

    List<D3DRect>^ collection = gcnew List<D3DRect>();
    for (UINT i = 0; i < tempoutNumRects; i++)
    {
        collection->Add(D3DRect(rects[i]));
    }

    return gcnew ReadOnlyCollection<D3DRect>(collection);
}

IEnumerable<D3DRect>^ RasterizerPipelineStage::ScissorRects::get(void)
{
    UInt32 rectCount = 0;

    Parent->CastInterface<ID3D11DeviceContext>()->RSGetScissorRects(&rectCount, NULL);

    return BoundedGetScissorRects(rectCount);
}

RasterizerState^ RasterizerPipelineStage::State::get(void)
{
    ID3D11RasterizerState* tempoutRasterizerState;
    Parent->CastInterface<ID3D11DeviceContext>()->RSGetState(&tempoutRasterizerState);
    
    return tempoutRasterizerState == NULL ? nullptr : gcnew RasterizerState(tempoutRasterizerState);
}

IEnumerable<Viewport>^ RasterizerPipelineStage::BoundedGetViewports(UInt32 maxNumberOfViewports)
{
    UINT numViewports = static_cast<UINT>(maxNumberOfViewports);
    vector<D3D11_VIEWPORT> viewports(numViewports);

    Parent->CastInterface<ID3D11DeviceContext>()->RSGetViewports(&numViewports, &viewports[0]);

    IList<Viewport>^ collection = gcnew List<Viewport>((int)numViewports);

    for (UINT i = 0; i < numViewports; i++)
    {
        collection->Add(Viewport(viewports[i]));
    }

    return gcnew ReadOnlyCollection<Viewport>(collection);
}

IEnumerable<Viewport>^ RasterizerPipelineStage::Viewports::get(void)
{
    UInt32 viewportCount = 0;

    Parent->CastInterface<ID3D11DeviceContext>()->RSGetViewports(&viewportCount, NULL);

    return BoundedGetViewports(viewportCount);
}

void RasterizerPipelineStage::ScissorRects::set(IEnumerable<D3DRect>^ rects)
{
    vector<D3D11_RECT> rectsVector;

    UINT numRects = Utilities::Convert::FillValStructsVector<D3DRect, D3D11_RECT>(rects, rectsVector);

    Parent->CastInterface<ID3D11DeviceContext>()->RSSetScissorRects(
        numRects, 
        numRects == 0 ? NULL : &rectsVector[0]);
}

void RasterizerPipelineStage::State::set(RasterizerState^ rasterizerState)
{
    Parent->CastInterface<ID3D11DeviceContext>()->RSSetState(
        rasterizerState == nullptr ? NULL : rasterizerState->CastInterface<ID3D11RasterizerState>());
}

void RasterizerPipelineStage::Viewports::set(IEnumerable<Viewport>^ viewports)
{
    vector<D3D11_VIEWPORT> portsVector;

    UINT numPorts = Utilities::Convert::FillValStructsVector<Viewport, D3D11_VIEWPORT>(viewports, portsVector);

    Parent->CastInterface<ID3D11DeviceContext>()->RSSetViewports(
        numPorts, 
        numPorts == 0 ? NULL : &portsVector[0]);
}