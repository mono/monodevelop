// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DXGIAdapter.h"
#include "DXGIOutput.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;
using namespace System::Collections::Generic;

Nullable<AdapterDriverVersion> Adapter::CheckDeviceSupport(DeviceType deviceType)
{
    LARGE_INTEGER tempVersion;
    GUID interfaceGuid;

    switch (deviceType)
    {
        case DeviceType::Direct3D10:
            interfaceGuid = __uuidof(ID3D10Device);
            break;
        case DeviceType::Direct3D10Point1:
            interfaceGuid = __uuidof(ID3D10Device1);
            break;
        case DeviceType::Direct3D11:
            interfaceGuid = __uuidof(ID3D11Device);
            break;
        default:
            throw gcnew ArgumentOutOfRangeException("deviceType", "Device type not supported.");
    }

    HRESULT hr = CastInterface<IDXGIAdapter>()->CheckInterfaceSupport(interfaceGuid, &tempVersion);

    if (!SUCCEEDED(hr))
    {
        return Nullable<AdapterDriverVersion>();
    }
    else
    {
        return Nullable<AdapterDriverVersion>(AdapterDriverVersion(tempVersion.HighPart, tempVersion.LowPart));
    }
}

ReadOnlyCollection<Output^>^ Adapter::Outputs::get(void)
{
    int i = 0;
    IList<Output^>^ outputsCache = gcnew List<Output^>();
    IDXGIAdapter *adapter = CastInterface<IDXGIAdapter>();
    IDXGIOutput* tempoutOutput;

    while (adapter->EnumOutputs(i++, &tempoutOutput) != DXGI_ERROR_NOT_FOUND)
    {
        outputsCache->Add(gcnew Output(tempoutOutput));
    }

    return gcnew ReadOnlyCollection<Output^>(outputsCache);
}

Output^ Adapter::GetOutput(UInt32 index)
{
    IDXGIOutput* tempoutOutput = NULL;
    Validate::VerifyResult(CastInterface<IDXGIAdapter>()->EnumOutputs(index, &tempoutOutput));
    return tempoutOutput == NULL ? nullptr : gcnew Output(tempoutOutput);
}

// REVIEW: I'm not convinced this really needs to be lazily init'ed. But for now, keep
// the original behavior, just cleaned up a bit.

AdapterDescription Adapter::Description::get()
{
	if (!description.HasValue)
	{
        DXGI_ADAPTER_DESC nativeDesc;

        Validate::VerifyResult(CastInterface<IDXGIAdapter>()->GetDesc(&nativeDesc));

        description = Nullable<Graphics::AdapterDescription>(Graphics::AdapterDescription(nativeDesc));
	}

    return description.Value;
}
