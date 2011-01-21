// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11Counter.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

CounterDescription D3DCounter::Description::get()
{
    D3D11_COUNTER_DESC desc;
    CastInterface<ID3D11Counter>()->GetDesc(&desc);
    
    return CounterDescription(desc);
}

