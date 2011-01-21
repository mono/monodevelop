// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10DepthStencilView.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

DepthStencilViewDescription DepthStencilView::Description::get()
{
    D3D10_DEPTH_STENCIL_VIEW_DESC desc;

    CastInterface<ID3D10DepthStencilView>()->GetDesc(&desc);
    return DepthStencilViewDescription(desc);
}

