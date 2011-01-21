// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DXGIDeviceSubObject.h"

#include "DXGIDevice.h"
#include "Direct3D11/D3D11Device.h"
#include "Direct3D10/D3D10Device.h"
#include "Direct3D10/D3D10Device1.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

Microsoft::WindowsAPICodePack::DirectX::Direct3D10::D3DDevice^ DeviceSubObject::AsDirect3D10Device::get(void)
{
    ID3D10Device* tempoutDevice = NULL;

    Validate::VerifyResult(CastInterface<IDXGIDeviceSubObject>()->GetDevice(__uuidof(tempoutDevice), (void**)&tempoutDevice));

    return tempoutDevice == NULL ? nullptr : gcnew Microsoft::WindowsAPICodePack::DirectX::Direct3D10::D3DDevice(tempoutDevice);
}

Microsoft::WindowsAPICodePack::DirectX::Direct3D10::D3DDevice1^ DeviceSubObject::AsDirect3D10Device1::get(void)
{
    ID3D10Device1* tempoutDevice = NULL;

    Validate::VerifyResult(CastInterface<IDXGIDeviceSubObject>()->GetDevice(__uuidof(tempoutDevice), (void**)&tempoutDevice));

    return tempoutDevice == NULL ? nullptr : gcnew Microsoft::WindowsAPICodePack::DirectX::Direct3D10::D3DDevice1(tempoutDevice);
}

Microsoft::WindowsAPICodePack::DirectX::Direct3D11::D3DDevice^ DeviceSubObject::AsDirect3D11Device::get(void)
{
    ID3D11Device* tempoutDevice = NULL;

    Validate::VerifyResult(CastInterface<IDXGIDeviceSubObject>()->GetDevice(__uuidof(tempoutDevice), (void**)&tempoutDevice));

    return tempoutDevice == NULL ? nullptr : gcnew Microsoft::WindowsAPICodePack::DirectX::Direct3D11::D3DDevice(tempoutDevice);
}

Graphics::Device^ DeviceSubObject::AsGraphicsDevice::get(void)
{
    IDXGIDevice* tempoutDevice = NULL;

    Validate::VerifyResult(CastInterface<IDXGIDeviceSubObject>()->GetDevice(__uuidof(tempoutDevice), (void**)&tempoutDevice));

    return tempoutDevice == NULL ? nullptr : gcnew Graphics::Device(tempoutDevice);
}

