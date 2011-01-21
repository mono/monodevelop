// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11View.h"
#include "D3D11Resource.h"

using namespace Microsoft::WindowsAPICodePack::DirectX;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

D3DResource^ View::Resource::get(void)
{
    ID3D11Resource* tempoutResource = NULL;
    CastInterface<ID3D11View>()->GetResource(&tempoutResource);

    return tempoutResource == NULL ? nullptr : gcnew D3DResource(tempoutResource);
}

