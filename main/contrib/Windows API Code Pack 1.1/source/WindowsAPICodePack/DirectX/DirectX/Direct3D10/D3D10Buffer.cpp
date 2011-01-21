// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Buffer.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

BufferDescription D3DBuffer::Description::get()
{
    D3D10_BUFFER_DESC desc = {0};
    CastInterface<ID3D10Buffer>()->GetDesc(&desc);
    
    return BufferDescription(desc);
}

IntPtr D3DBuffer::Map(Direct3D10::Map type, Direct3D10::MapOptions options)
{
    void* tempoutData;    
    Validate::VerifyResult(CastInterface<ID3D10Buffer>()->Map(static_cast<D3D10_MAP>(type), static_cast<UINT>(options), &tempoutData));    
    
    return IntPtr(tempoutData);
}

void D3DBuffer::Unmap()
{
    CastInterface<ID3D10Buffer>()->Unmap();
}

