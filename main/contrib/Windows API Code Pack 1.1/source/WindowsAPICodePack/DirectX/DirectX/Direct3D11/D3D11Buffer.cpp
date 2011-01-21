// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11Buffer.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

BufferDescription D3DBuffer::Description::get()
{
    BufferDescription description;
    pin_ptr<BufferDescription> ptr = &description;

    CastInterface<ID3D11Buffer>()->GetDesc((D3D11_BUFFER_DESC*) ptr);
    return description;

}

