// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Asynchronous.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

void Asynchronous::Begin()
{
    CastInterface<ID3D10Asynchronous>()->Begin();
}

void Asynchronous::End()
{
    CastInterface<ID3D10Asynchronous>()->End();
}

void Asynchronous::GetData(IntPtr data, UInt32 dataSize, AsyncGetDataOptions options)
{

    Validate::VerifyResult(CastInterface<ID3D10Asynchronous>()->GetData(data.ToPointer(),
		static_cast<UINT>(dataSize), static_cast<UINT>(options)));
}
UInt32 Asynchronous::DataSize::get(void)
{
    UINT returnValue = CastInterface<ID3D10Asynchronous>()->GetDataSize();
    return returnValue;
}

