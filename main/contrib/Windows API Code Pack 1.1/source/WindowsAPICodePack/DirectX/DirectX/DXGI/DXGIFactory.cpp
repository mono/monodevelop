// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"

#include "CommonUtils.h"
#include "LibraryLoader.h"
#include "Direct3D11/D3D11Device.h"
#include "Direct3D10/D3D10Device.h"
#include "DXGIFactory.h"
#include "DXGIAdapter.h"
#include "DXGISwapChain.h"

using namespace System::Collections::Generic;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;

typedef HRESULT (WINAPI *CreateDXGIFactoryFuncPtr)(REFIID riid, void **ppFactory);

Factory^ Factory::Create()
{
    CreateDXGIFactoryFuncPtr createFuncPtr = 
        (CreateDXGIFactoryFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            DXGILibrary, "CreateDXGIFactory");

    IDXGIFactory * pNativeIDXGIFactory = NULL;

    Validate::VerifyResult(
        (*createFuncPtr)(__uuidof(IDXGIFactory), (void**)(&pNativeIDXGIFactory)));

    return gcnew Factory(pNativeIDXGIFactory);
}

SwapChain^ Factory::CreateSwapChain(Microsoft::WindowsAPICodePack::DirectX::Direct3D10::D3DDevice^ device, SwapChainDescription description)
{
    Validate::CheckNull(device, "device");

    DXGI_SWAP_CHAIN_DESC nativeDesc = {0};
    description.CopyTo(&nativeDesc);
    
    IDXGISwapChain* tempoutSwapChain = NULL;

    Validate::VerifyResult(
        CastInterface<IDXGIFactory>()->CreateSwapChain(
            device->CastInterface<ID3D10Device>(), 
            &nativeDesc, 
            &tempoutSwapChain));

    return tempoutSwapChain ? gcnew SwapChain(tempoutSwapChain) : nullptr;
}

SwapChain^ Factory::CreateSwapChain(Microsoft::WindowsAPICodePack::DirectX::Direct3D11::D3DDevice^ device, SwapChainDescription description)
{
    Validate::CheckNull(device, "device");

    DXGI_SWAP_CHAIN_DESC nativeDesc = {0};
    description.CopyTo(&nativeDesc);
    
    IDXGISwapChain* tempoutSwapChain = NULL;

    Validate::VerifyResult(
        CastInterface<IDXGIFactory>()->CreateSwapChain(
            device->CastInterface<ID3D11Device>(), 
            &nativeDesc, 
            &tempoutSwapChain));

    return tempoutSwapChain ? gcnew SwapChain(tempoutSwapChain) : nullptr;
}

ReadOnlyCollection<Adapter^>^ Factory::Adapters::get(void)
{
    if (adapters != nullptr)
    {
        return adapters;
    }

    IList<Adapter^>^ adaptersCache = gcnew List<Adapter^>();

    int i = 0;
    IDXGIAdapter* tempAdapter;

    while (DXGI_ERROR_NOT_FOUND != CastInterface<IDXGIFactory>()->EnumAdapters(i++, &tempAdapter))
    {
        adaptersCache->Add(gcnew Adapter(tempAdapter));
    }

    adapters = gcnew ReadOnlyCollection<Adapter^>(adaptersCache);

    return adapters;
}

IntPtr Factory::WindowAssociation::get(void)
{
    HWND hWnd = 0;
    Validate::VerifyResult(CastInterface<IDXGIFactory>()->GetWindowAssociation(&hWnd));
    
    return IntPtr(hWnd);
}

void Factory::MakeWindowAssociation(IntPtr windowHandle, MakeWindowAssociationOptions options)
{
    Validate::VerifyResult(CastInterface<IDXGIFactory>()->MakeWindowAssociation(static_cast<HWND>(windowHandle.ToPointer()), static_cast<UINT>(options)));
}

