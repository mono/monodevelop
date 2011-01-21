// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10ShaderResourceView.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

ShaderResourceViewDescription ShaderResourceView::Description::get()
{
    D3D10_SHADER_RESOURCE_VIEW_DESC desc;

    CastInterface<ID3D10ShaderResourceView>()->GetDesc(&desc);
    return ShaderResourceViewDescription(desc);
}

