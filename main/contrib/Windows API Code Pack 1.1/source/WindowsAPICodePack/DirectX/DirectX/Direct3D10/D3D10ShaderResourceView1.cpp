// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10ShaderResourceView1.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

ShaderResourceViewDescription1 ShaderResourceView1::Description1::get()
{
	D3D10_SHADER_RESOURCE_VIEW_DESC1 nativeDescription;

    CastInterface<ID3D10ShaderResourceView1>()->GetDesc1(&nativeDescription);
    
    return ShaderResourceViewDescription1(nativeDescription);
}

