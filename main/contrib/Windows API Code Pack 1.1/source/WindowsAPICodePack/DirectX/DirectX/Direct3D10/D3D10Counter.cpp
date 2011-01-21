// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Counter.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

CounterDescription D3DCounter::Description::get()
{
    D3D10_COUNTER_DESC desc;
    CastInterface<ID3D10Counter>()->GetDesc(&desc);
    
    return CounterDescription(desc);
}

