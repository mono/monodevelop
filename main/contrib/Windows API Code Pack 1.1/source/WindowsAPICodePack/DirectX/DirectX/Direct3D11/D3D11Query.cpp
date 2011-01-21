// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11Query.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

QueryDescription D3DQuery::Description::get()
{
    D3D11_QUERY_DESC desc;
    CastInterface<ID3D11Query>()->GetDesc(&desc);
    
    return QueryDescription(desc);
}

