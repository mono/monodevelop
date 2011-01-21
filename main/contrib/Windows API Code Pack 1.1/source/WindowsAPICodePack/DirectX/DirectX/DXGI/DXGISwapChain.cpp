// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DXGISwapChain.h"
#include "DXGIOutput.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

generic <typename T>  where T : DirectUnknown  
T SwapChain::GetBuffer(UInt32 bufferIndex)
{
    void* tempResource = NULL;
    GUID guid = CommonUtils::GetGuid(T::typeid);
    
    Validate::VerifyResult(CastInterface<IDXGISwapChain>()->GetBuffer(bufferIndex, guid, &tempResource));

    return Utilities::Convert::CreateIUnknownWrapper<T>(static_cast<IUnknown*>(tempResource));
}

Output^ SwapChain::ContainingOutput::get(void)
{
    IDXGIOutput* tempOutput = NULL;
    Validate::VerifyResult(CastInterface<IDXGISwapChain>()->GetContainingOutput(&tempOutput));

    return (tempOutput == NULL ? nullptr : gcnew Output(tempOutput));
}

SwapChainDescription SwapChain::Description::get()
{
    DXGI_SWAP_CHAIN_DESC desc = {0};

    Validate::VerifyResult(CastInterface<IDXGISwapChain>()->GetDesc(&desc));

    return SwapChainDescription(desc);
}

FrameStatistics SwapChain::FrameStatistics::get(void)
{
    Graphics::FrameStatistics stats;
    pin_ptr<Graphics::FrameStatistics> ptr = &stats;

    Validate::VerifyResult(CastInterface<IDXGISwapChain>()->GetFrameStatistics((DXGI_FRAME_STATISTICS*)ptr));
    
    return stats;
}

Output^ SwapChain::FullScreenOutput::get(void)
{
    BOOL tempFullscreen;
    IDXGIOutput* tempTarget = NULL;
    
    Validate::VerifyResult(CastInterface<IDXGISwapChain>()->GetFullscreenState(&tempFullscreen, &tempTarget));
    
    return tempTarget == NULL ? nullptr : gcnew Output(tempTarget);
}

bool SwapChain::IsFullScreen::get()
{
    BOOL tempFullscreen;
    IDXGIOutput* tempTarget;
    
    Validate::VerifyResult(CastInterface<IDXGISwapChain>()->GetFullscreenState(&tempFullscreen, &tempTarget));
    
    return tempFullscreen != 0;
}

void SwapChain::IsFullScreen::set(bool value)
{
    SetFullScreenState(value, nullptr);
}

UInt32 SwapChain::LastPresentCount::get()
{
    UINT tempLastPresentCount;
    Validate::VerifyResult(CastInterface<IDXGISwapChain>()->GetLastPresentCount(&tempLastPresentCount));
    return tempLastPresentCount;
}

void SwapChain::Present(UInt32 syncInterval, Graphics::PresentOptions options)
{
    Validate::VerifyResult(CastInterface<IDXGISwapChain>()->Present(static_cast<UINT>(syncInterval), static_cast<UINT>(options)));
}

bool SwapChain::TryPresent(UInt32 syncInterval, Graphics::PresentOptions options, [System::Runtime::InteropServices::Out] ErrorCode % error)
{
    HRESULT hr = CastInterface<IDXGISwapChain>()->Present(static_cast<UINT>(syncInterval), static_cast<UINT>(options));

    error = safe_cast<ErrorCode>(hr);

    return SUCCEEDED(hr);
}

void SwapChain::ResizeBuffers(UInt32 bufferCount, UInt32 width, UInt32 height, Format newFormat, SwapChainOptions options)
{
    Validate::VerifyResult(
        CastInterface<IDXGISwapChain>()->ResizeBuffers(
        bufferCount, width, height, static_cast<DXGI_FORMAT>(newFormat), static_cast<UINT>(options)));
}

bool SwapChain::TryResizeBuffers(UInt32 bufferCount, UInt32 width, UInt32 height, Format newFormat, SwapChainOptions options, [System::Runtime::InteropServices::Out] ErrorCode % errorCode)
{
    errorCode = static_cast<ErrorCode>(CastInterface<IDXGISwapChain>()->ResizeBuffers(
        bufferCount, width, height, static_cast<DXGI_FORMAT>(newFormat), static_cast<UINT>(options)));

    return SUCCEEDED(errorCode);
    
}

void SwapChain::ResizeTarget(ModeDescription newTargetParameters)
{
    DXGI_MODE_DESC desc;
    newTargetParameters.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<IDXGISwapChain>()->ResizeTarget(&desc));
}

void SwapChain::SetFullScreenState(Boolean isFullScreen, Output^ target)
{
    Validate::VerifyResult(CastInterface<IDXGISwapChain>()->SetFullscreenState(
		isFullScreen ? 1 : 0, target == nullptr ? NULL : target->CastInterface<IDXGIOutput>()));
}

