// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11ShaderResourceView.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

ShaderResourceViewDescription ShaderResourceView::Description::get()
{
    D3D11_SHADER_RESOURCE_VIEW_DESC desc;
    CastInterface<ID3D11ShaderResourceView>()->GetDesc(&desc);

    return ShaderResourceViewDescription(desc);
}

