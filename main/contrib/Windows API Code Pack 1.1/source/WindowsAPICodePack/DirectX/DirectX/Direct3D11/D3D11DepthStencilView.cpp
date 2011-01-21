// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11DepthStencilView.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

DepthStencilViewDescription DepthStencilView::Description::get()
{
    D3D11_DEPTH_STENCIL_VIEW_DESC desc;
    CastInterface<ID3D11DepthStencilView>()->GetDesc(&desc);
    return DepthStencilViewDescription(desc);
}

