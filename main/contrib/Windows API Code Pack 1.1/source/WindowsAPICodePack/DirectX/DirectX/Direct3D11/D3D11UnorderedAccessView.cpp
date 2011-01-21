// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11UnorderedAccessView.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

UnorderedAccessViewDescription UnorderedAccessView::Description::get()
{
    D3D11_UNORDERED_ACCESS_VIEW_DESC desc;
    CastInterface<ID3D11UnorderedAccessView>()->GetDesc(&desc);
    return UnorderedAccessViewDescription(desc);
}

