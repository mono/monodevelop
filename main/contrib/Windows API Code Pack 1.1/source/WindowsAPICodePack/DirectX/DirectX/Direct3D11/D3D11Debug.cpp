// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"

#include "D3D11Debug.h"
#include "D3D11DeviceContext.h"
#include "DXGI/DXGISwapChain.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

DebugFeatures D3DDebug::FeatureMask::get()
{
    return safe_cast<DebugFeatures>(CastInterface<ID3D11Debug>()->GetFeatureMask());
}

UInt32 D3DDebug::PresentPerRenderOperationDelay::get()
{
    return CastInterface<ID3D11Debug>()->GetPresentPerRenderOpDelay();
}

SwapChain^ D3DDebug::RuntimeSwapChain::get()
{
    IDXGISwapChain* tempoutSwapChain = NULL;
    Validate::VerifyResult(CastInterface<ID3D11Debug>()->GetSwapChain(&tempoutSwapChain));
    
    return tempoutSwapChain == NULL ? nullptr : gcnew SwapChain(tempoutSwapChain);
}

void D3DDebug::ValidateContext(DeviceContext^ context)
{
    Validate::VerifyResult(CastInterface<ID3D11Debug>()->ValidateContext(context->CastInterface<ID3D11DeviceContext>()));
}

void D3DDebug::FeatureMask::set(DebugFeatures value)
{
    Validate::VerifyResult(CastInterface<ID3D11Debug>()->SetFeatureMask(static_cast<UINT>(value)));
}

void D3DDebug::PresentPerRenderOperationDelay::set(UInt32 value)
{
    Validate::VerifyResult(CastInterface<ID3D11Debug>()->SetPresentPerRenderOpDelay(static_cast<UINT>(value)));
}

void D3DDebug::RuntimeSwapChain::set(SwapChain^ value)
{
    Validate::VerifyResult(CastInterface<ID3D11Debug>()->SetSwapChain(value->CastInterface<IDXGISwapChain>()));
}

