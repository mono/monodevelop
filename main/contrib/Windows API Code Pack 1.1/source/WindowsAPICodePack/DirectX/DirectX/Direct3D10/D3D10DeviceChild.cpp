// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10DeviceChild.h"
#include "D3D10Device.h"
#include "D3D10Device1.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

D3DDevice^ DeviceChild::Device::get(void)
{
    ID3D10Device* tempoutDevice = NULL;
    CastInterface<ID3D10DeviceChild>()->GetDevice(&tempoutDevice);
    return tempoutDevice == NULL ? nullptr : gcnew D3DDevice(tempoutDevice);
}

