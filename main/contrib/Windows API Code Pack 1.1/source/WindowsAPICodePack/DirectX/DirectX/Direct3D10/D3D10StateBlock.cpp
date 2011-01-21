// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10StateBlock.h"

#include "D3D10Device.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

void StateBlock::Apply()
{
    Validate::VerifyResult(CastInterface<ID3D10StateBlock>()->Apply());
}

void StateBlock::Capture()
{
    Validate::VerifyResult(CastInterface<ID3D10StateBlock>()->Capture());
}

D3DDevice^ StateBlock::Device::get(void)
{
    ID3D10Device* tempoutDevice;
    Validate::VerifyResult(CastInterface<ID3D10StateBlock>()->GetDevice(&tempoutDevice));
    
    return tempoutDevice == NULL ? nullptr : gcnew D3DDevice(tempoutDevice);
}

void StateBlock::ReleaseAllDeviceObjects()
{
    Validate::VerifyResult(CastInterface<ID3D10StateBlock>()->ReleaseAllDeviceObjects());
}

