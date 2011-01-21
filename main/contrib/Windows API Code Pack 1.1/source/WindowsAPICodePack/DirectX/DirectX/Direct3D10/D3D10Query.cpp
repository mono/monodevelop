// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Query.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

QueryDescription D3DQuery::Description::get()
{
    D3D10_QUERY_DESC desc;
    CastInterface<ID3D10Query>()->GetDesc(&desc);
    
    return QueryDescription(desc);
}

