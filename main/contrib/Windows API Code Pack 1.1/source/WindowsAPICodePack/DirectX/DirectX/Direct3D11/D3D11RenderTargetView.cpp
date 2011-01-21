// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11RenderTargetView.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

RenderTargetViewDescription RenderTargetView::Description::get()
{
    RenderTargetViewDescription desc;
    pin_ptr<RenderTargetViewDescription> ptr = &desc;

    CastInterface<ID3D11RenderTargetView>()->GetDesc((D3D11_RENDER_TARGET_VIEW_DESC*)ptr);
    
    return desc;
}

