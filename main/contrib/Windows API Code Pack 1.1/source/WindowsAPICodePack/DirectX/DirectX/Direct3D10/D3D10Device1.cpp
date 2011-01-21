// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Device1.h"
#include "LibraryLoader.h"

#include "D3D10Effect.h"
#include "D3D10BlendState1.h"
#include "D3D10Resource.h"
#include "D3D10ShaderResourceView1.h"
#include "DXGI/DXGIAdapter.h"
#include "DXGI/DXGISwapChain.h"

#pragma unmanaged

typedef HRESULT (WINAPI *D3D10CreateEffectFromMemoryPtr)(
    void *pData,
    SIZE_T DataLength,
    UINT FXFlags,
    ID3D10Device *pDevice, 
    ID3D10EffectPool *pEffectPool,
    ID3D10Effect **ppEffect
);

HRESULT CreateEffectFromMemory1(                                     
    void *pData,
    SIZE_T DataLength,
    UINT FXFlags,
    ID3D10Device *pDevice, 
    ID3D10EffectPool *pEffectPool,
    ID3D10Effect **ppEffect)
{
    D3D10CreateEffectFromMemoryPtr createEffectFuncPtr = 
        (D3D10CreateEffectFromMemoryPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10_1Library, "D3D10CreateEffectFromMemory");

    HRESULT hr =
        (*createEffectFuncPtr)( 
            pData,
            DataLength,
            FXFlags,
            pDevice,
            pEffectPool,
            ppEffect);

    return hr;
}

#pragma managed
using namespace std;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

typedef HRESULT (WINAPI *D3D10CreateDeviceAndSwapChain1FuncPtr)(
    IDXGIAdapter *pAdapter,
    D3D10_DRIVER_TYPE DriverType,
    HMODULE Software,
    UINT Flags,
    D3D10_FEATURE_LEVEL1 HardwareLevel,
    UINT SDKVersion,
    DXGI_SWAP_CHAIN_DESC *pSwapChainDesc,
    IDXGISwapChain **ppSwapChain,    
    ID3D10Device1 **ppDevice
);

typedef HRESULT (WINAPI *D3D10CreateDevice1FuncPtr)(
    IDXGIAdapter *pAdapter,
    D3D10_DRIVER_TYPE DriverType,
    HMODULE Software,
    UINT Flags,
    D3D10_FEATURE_LEVEL1 HardwareLevel,
    UINT SDKVersion,
    ID3D10Device1 **ppDevice
);

typedef HRESULT (WINAPI *D3D10CreateEffectFromMemoryPtr)(
    void *pData,
    SIZE_T DataLength,
    UINT FXFlags,
    ID3D10Device *pDevice, 
    ID3D10EffectPool *pEffectPool,
    ID3D10Effect **ppEffect
);

D3DDevice1^ D3DDevice1::CreateDeviceAndSwapChain1(IntPtr windowHandle)
{
    RECT rc;

	Validate::VerifyBoolean(GetClientRect((HWND)windowHandle.ToPointer(), &rc));

    UInt32 width = rc.right - rc.left;
    UInt32 height = rc.bottom - rc.top;

    Direct3D10::CreateDeviceOptions options = Direct3D10::CreateDeviceOptions::None;

#ifdef _DEBUG

// Make sure GetEnvironmentVariable is not defined
#ifdef GetEnvironmentVariable
#undef GetEnvironmentVariable
#endif // GetEnvironmentVariable

    if (!String::IsNullOrEmpty(Environment::GetEnvironmentVariable("DXSDK_DIR")))
    {
        options  = Direct3D10::CreateDeviceOptions::Debug;
    }
#endif // _DEBUG

    SwapChainDescription swapChainDescription = SwapChainDescription();

    swapChainDescription.BufferCount = 1;
    swapChainDescription.BufferDescription =
        ModeDescription(width, height, Format::R8G8B8A8UNorm, Rational(60, 1));
    swapChainDescription.BufferUsage = UsageOptions::RenderTargetOutput;
    swapChainDescription.OutputWindowHandle = windowHandle;
    swapChainDescription.SampleDescription = SampleDescription(1, 0);
    swapChainDescription.Windowed = true;
    swapChainDescription.SwapEffect = SwapEffect::Discard;
    swapChainDescription.Options = SwapChainOptions::None;

    HRESULT hr = S_OK;
    array<FeatureLevel>^ levels =
    {
        FeatureLevel::TenPointOne,
        FeatureLevel::Ten,
        FeatureLevel::NinePointThree,
        FeatureLevel::NinePointTwo,
        FeatureLevel::NinePointOne,
    };

    for each (FeatureLevel featureLevel in levels)
    {
        D3DDevice1^ device;

        hr = TryCreateDeviceAndSwapChain1(nullptr, DriverType::Hardware, nullptr,
            options, featureLevel, swapChainDescription, device);

        if (SUCCEEDED(hr))
        {
            return device;
        }
    }

    if (hr == E_FAIL)
    {
        throw gcnew Direct3DException("Attempted to create a device with the debug layer enabled and the layer is not installed.", (int)hr);
    }
    else
    {
        Validate::VerifyResult(hr);
    }

    // Unreachable
    return nullptr;
}

D3DDevice1^ D3DDevice1::CreateDeviceAndSwapChain1(
    Adapter^ adapter,
    Direct3D10::DriverType driverType,
    String^ softwareRasterizerLibrary,
    Direct3D10::CreateDeviceOptions options,
    FeatureLevel hardwareLevel,
    SwapChainDescription swapChainDescription)
{
    HRESULT hr;
    D3DDevice1^ device;

    hr = TryCreateDeviceAndSwapChain1(adapter, driverType, softwareRasterizerLibrary, options, hardwareLevel, swapChainDescription, device);

    if (SUCCEEDED(hr))
    {
        return device;
    }

    if (hr == E_FAIL)
    {
        throw gcnew Direct3DException("Attempted to create a device with the debug layer enabled and the layer is not installed.", (int)hr);
    }
    else
    {
        Validate::VerifyResult(hr);
    }

    // Unreachable
    return nullptr;
}

HRESULT D3DDevice1::TryCreateDeviceAndSwapChain1(
    Adapter^ adapter,
    Direct3D10::DriverType driverType,
    String^ softwareRasterizerLibrary,
    Direct3D10::CreateDeviceOptions options,
    FeatureLevel hardwareLevel,
    SwapChainDescription swapChainDescription,
    D3DDevice1^ %device)
{
    DXGI_SWAP_CHAIN_DESC nativeDesc = {0};
    swapChainDescription.CopyTo(&nativeDesc);

    D3D10CreateDeviceAndSwapChain1FuncPtr createFuncPtr = 
        (D3D10CreateDeviceAndSwapChain1FuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10_1Library, "D3D10CreateDeviceAndSwapChain1");

    HINSTANCE softwareModule = NULL;
    if (!String::IsNullOrEmpty(softwareRasterizerLibrary))
    {
        softwareModule = CommonUtils::LoadDll(softwareRasterizerLibrary);
    }

    ID3D10Device1* pd3dDevice = NULL;
    IDXGISwapChain* pSwapChain = NULL;

    HRESULT hr = (*createFuncPtr)( 
        adapter == nullptr ? NULL : adapter->CastInterface<IDXGIAdapter>(), 
        static_cast<D3D10_DRIVER_TYPE>(driverType),
        softwareModule,
        static_cast<UINT>(options),
        static_cast<D3D10_FEATURE_LEVEL1>(hardwareLevel),
        D3D10_1_SDK_VERSION, 
        &nativeDesc, 
        &pSwapChain, 
        &pd3dDevice);

    if (SUCCEEDED(hr) && pd3dDevice != NULL && pSwapChain != NULL)
    {
        device = gcnew D3DDevice1(pd3dDevice);

        device->SetSwapChain(gcnew Graphics::SwapChain(pSwapChain));
    }
    else
    {
        device = nullptr;

        // In theory, this code should never be needed. But, just in case...
        if (pSwapChain != NULL)
        {
            pSwapChain->Release();
        }

        if (pd3dDevice != NULL)
        {
            pd3dDevice->Release();
        }
    }

    return hr;
}

D3DDevice1^ D3DDevice1::CreateDevice1()
{
    D3D10CreateDevice1FuncPtr createFuncPtr = 
        (D3D10CreateDevice1FuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10_1Library, "D3D10CreateDevice1");

    UINT createDeviceFlags = 0;
#ifdef _DEBUG

// Make sure GetEnvironmentVariable is not defined
#ifdef GetEnvironmentVariable
#undef GetEnvironmentVariable
#endif

    if (!String::IsNullOrEmpty(System::Environment::GetEnvironmentVariable("DXSDK_DIR")))
    {
        createDeviceFlags  = D3D10_CREATE_DEVICE_DEBUG;
    }
#endif

    ID3D10Device1* pd3dDevice = NULL;

    array<D3D10_FEATURE_LEVEL1>^ featureLevels = 
    {
        D3D10_FEATURE_LEVEL_10_1,
        D3D10_FEATURE_LEVEL_10_0,
        D3D10_FEATURE_LEVEL_9_3,
        D3D10_FEATURE_LEVEL_9_2,
        D3D10_FEATURE_LEVEL_9_1
    };

    HRESULT hr = S_OK;
    for each (D3D10_FEATURE_LEVEL1 level in featureLevels)
    {
        hr = (*createFuncPtr)( NULL, D3D10_DRIVER_TYPE_HARDWARE, NULL, createDeviceFlags,
            level, D3D10_1_SDK_VERSION, &pd3dDevice);

        if (SUCCEEDED(hr))
        {
            break;
        }
    }

    if (FAILED(hr))
    {
        if (hr == E_FAIL)
        {
            throw gcnew Direct3DException("Attempted to create a device with the debug layer enabled and the layer is not installed.", (int)hr);
        }
        else
        {
            Validate::VerifyResult(hr);
        }
    }

    return pd3dDevice ? gcnew D3DDevice1(pd3dDevice) : nullptr;
}

D3DDevice1^ D3DDevice1::CreateDevice1(Adapter^ adapter,  Microsoft::WindowsAPICodePack::DirectX::Direct3D10::DriverType driverType, String^ softwareRasterizerLibrary, Direct3D10::CreateDeviceOptions options, FeatureLevel hardwareLevel)
{
    D3D10CreateDevice1FuncPtr createFuncPtr = 
        (D3D10CreateDevice1FuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10_1Library, "D3D10CreateDevice1");

    ID3D10Device1* pd3dDevice = NULL;

    HINSTANCE softwareModule = NULL;
    if (!String::IsNullOrEmpty(softwareRasterizerLibrary))
    {
        softwareModule = CommonUtils::LoadDll(softwareRasterizerLibrary);
    }

    HRESULT hr =(*createFuncPtr)( 
        adapter == nullptr ? NULL : adapter->CastInterface<IDXGIAdapter>(), 
        static_cast<D3D10_DRIVER_TYPE>(driverType),
        softwareModule,
        static_cast<UINT>(options),
        static_cast<D3D10_FEATURE_LEVEL1>(hardwareLevel),
        D3D10_1_SDK_VERSION, 
        &pd3dDevice);

    if (FAILED(hr))
    {
        if (hr == E_FAIL)
        {
            throw gcnew Direct3DException("Attempted to create a device with the debug layer enabled and the layer is not installed.", (int)hr);
        }
        else
        {
            Validate::VerifyResult(hr);
        }
    }

    return pd3dDevice ? gcnew D3DDevice1(pd3dDevice) : nullptr;
}


BlendState1^ D3DDevice1::CreateBlendState1(BlendDescription1 BlendStateDescription)
{
    ID3D10BlendState1* tempoutBlendState = NULL;
    D3D10_BLEND_DESC1 desc;

    BlendStateDescription.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D10Device1>()->CreateBlendState1(
        &desc, &tempoutBlendState));
    
    return tempoutBlendState == NULL ? nullptr : gcnew BlendState1(tempoutBlendState);
}

ShaderResourceView1^ D3DDevice1::CreateShaderResourceView1(D3DResource^ resource, ShaderResourceViewDescription1 description)
{
    ID3D10ShaderResourceView1* tempoutSRView = NULL;

    pin_ptr<ShaderResourceViewDescription1> ptr = &description;

    Validate::VerifyResult(CastInterface<ID3D10Device1>()->CreateShaderResourceView1(
        resource->CastInterface<ID3D10Resource>(), (D3D10_SHADER_RESOURCE_VIEW_DESC1*)ptr, &tempoutSRView));

    return tempoutSRView == NULL ? nullptr : gcnew ShaderResourceView1(tempoutSRView);
}

FeatureLevel D3DDevice1::DeviceFeatureLevel::get()
{
    return static_cast<FeatureLevel>(CastInterface<ID3D10Device1>()->GetFeatureLevel());
}

Effect^ D3DDevice1::CreateEffectFromCompiledBinary( BinaryReader^ binaryEffect, int effectCompileOptions, EffectPool^ effectPool )
{
    array<unsigned char>^ data = binaryEffect->ReadBytes((int)binaryEffect->BaseStream->Length);
    pin_ptr<unsigned char> pData = &data[0];

    ID3D10Effect* pEffect = NULL;
    Validate::VerifyResult(
        CreateEffectFromMemory1( 
            pData,
            data->Length,
            effectCompileOptions,
            this->CastInterface<ID3D10Device>(),
            effectPool ? effectPool->CastInterface<ID3D10EffectPool>() : NULL,
            &pEffect));

    return pEffect ? gcnew Effect( pEffect ) : nullptr;
}

