// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10RasterizerPipelineStage.h"
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

IEnumerable<D3DRect>^ RasterizerPipelineStage::BoundedGetScissorRects(UInt32 maxNumberOfRects)
{
    // REVIEW: sanity check count?

    UINT tempoutNumRects = static_cast<UINT>(maxNumberOfRects);
    vector<D3D10_RECT> rects(maxNumberOfRects);

    Parent->CastInterface<ID3D10Device>()->RSGetScissorRects(&tempoutNumRects, &rects[0]);

    List<D3DRect>^ collection = gcnew List<D3DRect>();
    for (UINT i = 0; i < tempoutNumRects; i++)
    {
        collection->Add(D3DRect(rects[i]));
    }

    return gcnew ReadOnlyCollection<D3DRect>(collection);
}

RasterizerState^ RasterizerPipelineStage::State::get(void)
{
    ID3D10RasterizerState* tempoutRasterizerState;
    Parent->CastInterface<ID3D10Device>()->RSGetState(&tempoutRasterizerState);
    
    return tempoutRasterizerState == NULL ? nullptr : gcnew RasterizerState(tempoutRasterizerState);
}

IEnumerable<Viewport>^ RasterizerPipelineStage::BoundedGetViewports(UInt32 maxNumberOfViewports)
{
    UINT numViewports = static_cast<UINT>(maxNumberOfViewports);
    vector<D3D10_VIEWPORT> viewports(numViewports);

    Parent->CastInterface<ID3D10Device>()->RSGetViewports(&numViewports, &viewports[0]);

    IList<Viewport>^ collection = gcnew List<Viewport>((int)numViewports);

    for (UINT i = 0; i < numViewports; i++)
    {
        collection->Add(Viewport(viewports[i]));
    }

    return gcnew ReadOnlyCollection<Viewport>(collection);
}

IEnumerable<D3DRect>^ RasterizerPipelineStage::ScissorRects::get(void)
{
    UINT rectCount = 0;

    Parent->CastInterface<ID3D10Device>()->RSGetScissorRects(&rectCount, NULL);

    return BoundedGetScissorRects(rectCount);
}

void RasterizerPipelineStage::ScissorRects::set(IEnumerable<D3DRect>^ rects)
{
    vector<D3D10_RECT> rectsVector;

    UINT numRects = Utilities::Convert::FillValStructsVector<D3DRect, D3D10_RECT>(rects, rectsVector);

    Parent->CastInterface<ID3D10Device>()->RSSetScissorRects(
        numRects, 
        numRects == 0 ? NULL : &rectsVector[0]);
}

void RasterizerPipelineStage::State::set(RasterizerState^ rasterizerState)
{
    Parent->CastInterface<ID3D10Device>()->RSSetState(
        rasterizerState == nullptr ? NULL : rasterizerState->CastInterface<ID3D10RasterizerState>());
}

IEnumerable<Viewport>^ RasterizerPipelineStage::Viewports::get(void)
{
    UINT viewportCount = 0;

    Parent->CastInterface<ID3D10Device>()->RSGetViewports(&viewportCount, NULL);

    return BoundedGetViewports(viewportCount);
}

void RasterizerPipelineStage::Viewports::set(IEnumerable<Viewport>^ viewports)
{
    vector<D3D10_VIEWPORT> portsVector;

    UINT numPorts = Utilities::Convert::FillValStructsVector<Viewport, D3D10_VIEWPORT>(viewports, portsVector);

    Parent->CastInterface<ID3D10Device>()->RSSetViewports(
        numPorts, 
        numPorts == 0 ? NULL : &portsVector[0]);
}

