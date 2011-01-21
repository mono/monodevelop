// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10View.h"

#include "D3D10Resource.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

D3DResource^ View::Resource::get(void)
{
    ID3D10Resource* tempoutResource = NULL;
    CastInterface<ID3D10View>()->GetResource(&tempoutResource);
    return tempoutResource == NULL ? nullptr : gcnew D3DResource(tempoutResource);
}

