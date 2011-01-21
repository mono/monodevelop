// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"

#include "D3D10Debug.h"
#include "DXGI/DXGISwapChain.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;
DebugFeatures D3DDebug::FeatureMask::get()
{
    return static_cast<DebugFeatures>(CastInterface<ID3D10Debug>()->GetFeatureMask());
}

UInt32 D3DDebug::PresentPerRenderOperationDelay::get()
{
    return CastInterface<ID3D10Debug>()->GetPresentPerRenderOpDelay();
}

SwapChain^ D3DDebug::RuntimeSwapChain::get()
{
    IDXGISwapChain* tempoutSwapChain = NULL;
    Validate::VerifyResult(CastInterface<ID3D10Debug>()->GetSwapChain(&tempoutSwapChain));
    
    return tempoutSwapChain == NULL ? nullptr : gcnew SwapChain(tempoutSwapChain);
}
void D3DDebug::FeatureMask::set(DebugFeatures value)
{
    Validate::VerifyResult(CastInterface<ID3D10Debug>()->SetFeatureMask(static_cast<UINT>(value)));
}

void D3DDebug::PresentPerRenderOperationDelay::set(UInt32 value)
{
    Validate::VerifyResult(CastInterface<ID3D10Debug>()->SetPresentPerRenderOpDelay(static_cast<UINT>(value)));
}

void D3DDebug::RuntimeSwapChain::set(SwapChain^ value)
{
    Validate::VerifyResult(CastInterface<ID3D10Debug>()->SetSwapChain(value->CastInterface<IDXGISwapChain>()));
}

void D3DDebug::Validate()
{
    Validate::VerifyResult(CastInterface<ID3D10Debug>()->Validate());
}

