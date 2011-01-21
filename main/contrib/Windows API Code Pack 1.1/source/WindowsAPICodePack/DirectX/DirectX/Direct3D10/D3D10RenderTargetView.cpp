// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10RenderTargetView.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

RenderTargetViewDescription RenderTargetView::Description::get()
{
    RenderTargetViewDescription desc;
    pin_ptr<RenderTargetViewDescription> ptr = &desc;

    CastInterface<ID3D10RenderTargetView>()->GetDesc((D3D10_RENDER_TARGET_VIEW_DESC*)ptr);
    
    return desc;
}

