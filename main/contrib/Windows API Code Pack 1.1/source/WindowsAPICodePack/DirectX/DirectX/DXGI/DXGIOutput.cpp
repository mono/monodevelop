// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include <vector>

#include "DXGIOutput.h"
#include "DXGISurface.h"
#include "DXGIDevice.h"
#include "Direct3D11/D3D11Device.h"
#include "Direct3D10/D3D10Device.h"

using namespace std;
using namespace System::Collections::Generic;
using namespace System::Collections::ObjectModel;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;

ModeDescription Output::FindClosestMatchingMode(ModeDescription modeToMatch, Microsoft::WindowsAPICodePack::DirectX::Direct3D10::D3DDevice^ ConcernedDevice)
{
    DXGI_MODE_DESC inMode;
    modeToMatch.CopyTo(&inMode);

    DXGI_MODE_DESC outMode;

    Validate::VerifyResult(CastInterface<IDXGIOutput>()->FindClosestMatchingMode(
        &inMode, &outMode, 
        ConcernedDevice == nullptr ? NULL : ConcernedDevice->CastInterface<IUnknown>()));

    return ModeDescription(outMode);
}

ModeDescription Output::FindClosestMatchingMode(ModeDescription modeToMatch, Microsoft::WindowsAPICodePack::DirectX::Direct3D11::D3DDevice^ ConcernedDevice)
{
    DXGI_MODE_DESC inMode;
    modeToMatch.CopyTo(&inMode);

    DXGI_MODE_DESC outMode;

    Validate::VerifyResult(CastInterface<IDXGIOutput>()->FindClosestMatchingMode(
        &inMode, &outMode, 
        (ConcernedDevice == nullptr ? NULL : ConcernedDevice->CastInterface<IUnknown>())));

    return ModeDescription(outMode);
}

ModeDescription Output::FindClosestMatchingMode(ModeDescription modeToMatch)
{
    DXGI_MODE_DESC inMode;
    modeToMatch.CopyTo(&inMode);

    DXGI_MODE_DESC outMode;

    Validate::VerifyResult(CastInterface<IDXGIOutput>()->FindClosestMatchingMode(
        &inMode, &outMode, 
        NULL));
    
    return ModeDescription(outMode);
}

UInt32 Output::GetNumberOfDisplayModes(Format colorFormat, NonDefaultModes modes)
{
    UINT outNumModes = 0;

    Validate::VerifyResult(
        CastInterface<IDXGIOutput>()->GetDisplayModeList(
        static_cast<DXGI_FORMAT>(colorFormat), static_cast<UINT>(modes), &outNumModes, NULL));

    return outNumModes;
}

array<ModeDescription>^ Output::GetDisplayModeList(Format colorFormat, NonDefaultModes modes)
{
    UINT numModes = GetNumberOfDisplayModes(colorFormat, modes);
    array<ModeDescription>^ descriptions = nullptr;

    if (numModes > 0)
    {
        descriptions = gcnew array<ModeDescription>(numModes);
        pin_ptr<ModeDescription> ptr = &descriptions[0];

        Validate::VerifyResult(
            CastInterface<IDXGIOutput>()->GetDisplayModeList(
            static_cast<DXGI_FORMAT>(colorFormat), static_cast<UINT>(modes), &numModes, (DXGI_MODE_DESC*) ptr));
    }

    return descriptions;
}
OutputDescription Output::Description::get()
{

    DXGI_OUTPUT_DESC desc; 
    Validate::VerifyResult(CastInterface<IDXGIOutput>()->GetDesc(&desc));

    return OutputDescription(desc);
}

void Output::GetDisplaySurfaceData(Surface^ destination)
{
    Validate::VerifyResult(CastInterface<IDXGIOutput>()->GetDisplaySurfaceData(destination->CastInterface<IDXGISurface>()));
    
    // Since the native function increase the ref count, we need to make sure 
    // it's decreased again so we don't get a memory leak
    
    destination->CastInterface<IDXGISurface>()->Release();
}

FrameStatistics Output::RenderedFrameStatistics::get(void)
{
    DXGI_FRAME_STATISTICS tempoutStats;
    
    Validate::VerifyResult(CastInterface<IDXGIOutput>()->GetFrameStatistics(&tempoutStats));
    
    return FrameStatistics(tempoutStats);
}

GammaControl Output::GammaControl::get(void)
{
    DXGI_GAMMA_CONTROL ctrl = {0};

    Validate::VerifyResult(CastInterface<IDXGIOutput>()->GetGammaControl(&ctrl));

    return Graphics::GammaControl(ctrl);
}

void Output::GammaControl::set(Graphics::GammaControl gammaControl)
{
    DXGI_GAMMA_CONTROL ctrl = {0};
    gammaControl.CopyTo(&ctrl);
    Validate::VerifyResult(CastInterface<IDXGIOutput>()->SetGammaControl(&ctrl));
}

GammaControlCapabilities Output::GammaControlCapabilities::get(void)
{
    DXGI_GAMMA_CONTROL_CAPABILITIES caps = {0};
    Validate::VerifyResult(CastInterface<IDXGIOutput>()->GetGammaControlCapabilities(&caps));
    return Graphics::GammaControlCapabilities(caps);
}

void Output::SetDisplaySurface(Surface^ surface)
{
    Validate::VerifyResult(CastInterface<IDXGIOutput>()->SetDisplaySurface(surface->CastInterface<IDXGISurface>()));
}

void Output::ReleaseOwnership()
{
    CastInterface<IDXGIOutput>()->ReleaseOwnership();
}
void Output::TakeOwnership(Microsoft::WindowsAPICodePack::DirectX::Direct3D10::D3DDevice^ device, Boolean exclusive)
{
    Validate::VerifyResult(CastInterface<IDXGIOutput>()->TakeOwnership(device->CastInterface<IUnknown>(), safe_cast<BOOL>(exclusive)));
}

void Output::TakeOwnership(Microsoft::WindowsAPICodePack::DirectX::Direct3D11::D3DDevice^ device, Boolean exclusive)
{
    Validate::VerifyResult(CastInterface<IDXGIOutput>()->TakeOwnership(device->CastInterface<IUnknown>(), safe_cast<BOOL>(exclusive)));
}

void Output::TakeOwnership(Device^ device, Boolean exclusive)
{
    Validate::VerifyResult(CastInterface<IDXGIOutput>()->TakeOwnership(device->CastInterface<IDXGIDevice>(), safe_cast<BOOL>(exclusive)));
}
void Output::WaitForVBlank()
{
    Validate::VerifyResult(CastInterface<IDXGIOutput>()->WaitForVBlank());
}

