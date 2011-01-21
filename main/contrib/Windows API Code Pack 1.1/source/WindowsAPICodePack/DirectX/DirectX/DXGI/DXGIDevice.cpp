// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DXGIDevice.h"
#include "DXGIResource.h"
#include "DXGISurface.h"
#include "DXGIAdapter.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

Adapter^ Device::Adapter::get(void)
{
    IDXGIAdapter* tempoutAdapter = NULL;
    Validate::VerifyResult(CastInterface<IDXGIDevice>()->GetAdapter(&tempoutAdapter));
    return (tempoutAdapter == NULL ? nullptr : gcnew Graphics::Adapter(tempoutAdapter));
}

Int32 Device::GpuThreadPriority::get()
{
    INT tempoutPriority;
    Validate::VerifyResult(CastInterface<IDXGIDevice>()->GetGPUThreadPriority(&tempoutPriority));
    return tempoutPriority;
}

void Device::GpuThreadPriority::set(Int32 value)
{
    if (value < -7 || value > 7)
    {
        throw gcnew ArgumentOutOfRangeException("value", "GpuThreadPriority valid range is -7 to +7, inclusive.");
    }

    Validate::VerifyResult(CastInterface<IDXGIDevice>()->SetGPUThreadPriority(value));
}

array<Residency>^ Device::QueryResourceResidency(IEnumerable<Resource^>^ resources)
{
    vector<IDXGIResource*> resourcesVector;

    UINT count = Utilities::Convert::FillIUnknownsVector<Resource, IDXGIResource>(resources, resourcesVector);
    if (count == 0)
        return nullptr;

    DXGI_RESIDENCY* residencyStatusArray = new DXGI_RESIDENCY[count];
    try
    {
        Validate::VerifyResult(CastInterface<IDXGIDevice>()->QueryResourceResidency((IUnknown**)&resourcesVector[0], residencyStatusArray, count));

        array<Residency>^ returnArray = gcnew array<Residency>(count);
        pin_ptr<Residency> pinnedArray = &returnArray[0];
        memcpy(pinnedArray, residencyStatusArray, sizeof(DXGI_RESIDENCY) * count);
        return returnArray;
    }
    finally
    {
        delete [] residencyStatusArray;
    }
}

