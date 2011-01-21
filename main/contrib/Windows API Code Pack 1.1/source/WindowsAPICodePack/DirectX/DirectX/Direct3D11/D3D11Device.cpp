// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11Device.h"
#include "LibraryLoader.h"

#include "D3D11BlendState.h"
#include "D3D11Buffer.h"
#include "D3D11ClassLinkage.h"
#include "D3D11ComputeShader.h"
#include "D3D11Counter.h"
#include "D3D11DeviceContext.h"
#include "D3D11DepthStencilState.h"
#include "D3D11Resource.h"
#include "D3D11DepthStencilView.h"
#include "D3D11DomainShader.h"
#include "D3D11GeometryShader.h"
#include "D3D11HullShader.h"
#include "D3D11InputLayout.h"
#include "D3D11PixelShader.h"
#include "D3D11Predicate.h"
#include "D3D11Query.h"
#include "D3D11RasterizerState.h"
#include "D3D11RenderTargetView.h"
#include "D3D11SamplerState.h"
#include "D3D11ShaderResourceView.h"
#include "D3D11Texture1D.h"
#include "D3D11Texture2D.h"
#include "D3D11Texture3D.h"
#include "D3D11UnorderedAccessView.h"
#include "D3D11VertexShader.h"
#include "D3D11InfoQueue.h"
#include "D3D11SwitchToRef.h"
#include "D3D11Debug.h"
#include "DXGI/DXGISwapChain.h"
#include "DXGI/DXGIDevice.h"
#include "DXGI/DXGIDevice1.h"
#include "DXGI/DXGIAdapter.h"
#include "DXGI/DXGIObject.h"

#include <vector>
using namespace std;
using namespace System::Collections::Generic;

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

D3DDevice^ D3DDevice::CreateDeviceAndSwapChain(IntPtr windowHandle)
{
    RECT rc;
	Validate::VerifyBoolean(GetClientRect((HWND)windowHandle.ToPointer(), &rc));
    UInt32 width = rc.right - rc.left;
    UInt32 height = rc.bottom - rc.top;

    Direct3D11::CreateDeviceOptions options = Direct3D11::CreateDeviceOptions::None;

#ifdef _DEBUG

// Make sure GetEnvironmentVariable is not defined
#ifdef GetEnvironmentVariable
#undef GetEnvironmentVariable
#endif

    if (!String::IsNullOrEmpty(System::Environment::GetEnvironmentVariable("DXSDK_DIR")))
    {
        options  = Direct3D11::CreateDeviceOptions::Debug;
    }
#endif

    SwapChainDescription swapChainDescription = SwapChainDescription();

    swapChainDescription.BufferCount = 1;
    swapChainDescription.BufferDescription =
        ModeDescription(width, height, Graphics::Format::R8G8B8A8UNorm, Rational(60, 1));
    swapChainDescription.BufferUsage = Graphics::UsageOptions::RenderTargetOutput;
    swapChainDescription.OutputWindowHandle = windowHandle;
    swapChainDescription.SampleDescription = SampleDescription(1, 0);
    swapChainDescription.Windowed = true;
    swapChainDescription.SwapEffect = SwapEffect::Discard;
    swapChainDescription.Options = SwapChainOptions::None;

    return CreateDeviceAndSwapChain(nullptr, Direct3D11::DriverType::Hardware, nullptr,
        options, nullptr, swapChainDescription);
}

D3DDevice^ D3DDevice::CreateDeviceAndSwapChain(
    Adapter^ adapter,
    Direct3D11::DriverType driverType,
    String^ softwareRasterizerLibrary,
    Direct3D11::CreateDeviceOptions options,
    array<FeatureLevel>^ featureLevels,
    SwapChainDescription swapChainDescription)
{
    DXGI_SWAP_CHAIN_DESC nativeDesc = {0};
    swapChainDescription.CopyTo(&nativeDesc);

    PFN_D3D11_CREATE_DEVICE_AND_SWAP_CHAIN createFuncPtr = 
        (PFN_D3D11_CREATE_DEVICE_AND_SWAP_CHAIN) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D11Library, "D3D11CreateDeviceAndSwapChain");

    ID3D11Device* pd3dDevice;
    ID3D11DeviceContext* pd3dDeviceContext;
    IDXGISwapChain* pSwapChain;
    D3D_FEATURE_LEVEL featureLevel;
    HINSTANCE softwareModule = NULL;

    if (!String::IsNullOrEmpty(softwareRasterizerLibrary))
    {
        softwareModule = CommonUtils::LoadDll(softwareRasterizerLibrary);
    }

    if (featureLevels != nullptr && featureLevels->Length == 0)
    {
        featureLevels = nullptr;
    }

    pin_ptr<FeatureLevel> levels = featureLevels != nullptr ? &featureLevels[0] : nullptr;

    // REVIEW: we get the immediate device context here; why not just store it and
    // use that for the ImmediateContext property? Likewise for the FeatureLevel property.

    HRESULT hr = (*createFuncPtr)( 
        adapter == nullptr ? NULL : adapter->CastInterface<IDXGIAdapter>(), 
        safe_cast<D3D_DRIVER_TYPE>(driverType),
        softwareModule,
        static_cast<UINT>(options),
        featureLevels != nullptr ? (D3D_FEATURE_LEVEL*)levels : NULL,
        featureLevels != nullptr ? featureLevels->Length : 0, 
        D3D11_SDK_VERSION, 
        &nativeDesc, 
        &pSwapChain, 
        &pd3dDevice, 
        &featureLevel, 
        &pd3dDeviceContext);

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

    if (pd3dDevice != NULL && pSwapChain != NULL)
    {
        D3DDevice^ device = gcnew D3DDevice(pd3dDevice);

        device->swapChain = gcnew Graphics::SwapChain(pSwapChain);

        return device;
    }

    // In theory, this code should never be needed. But, just in case...
    if (pSwapChain != NULL)
    {
        pSwapChain->Release();
    }

    if (pd3dDevice != NULL)
    {
        pd3dDevice->Release();
    }

    return nullptr;
}

D3DDevice^ D3DDevice::CreateDevice()
{
    PFN_D3D11_CREATE_DEVICE createFuncPtr = 
        (PFN_D3D11_CREATE_DEVICE) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D11Library, "D3D11CreateDevice");

    UINT createDeviceFlags = 0;

#ifdef _DEBUG

// Make sure GetEnvironmentVariable is not defined
#ifdef GetEnvironmentVariable
#undef GetEnvironmentVariable
#endif

    if (!String::IsNullOrEmpty(System::Environment::GetEnvironmentVariable("DXSDK_DIR")))
    {
        createDeviceFlags  = D3D11_CREATE_DEVICE_DEBUG;
    }
#endif

    ID3D11Device* pd3dDevice;
    ID3D11DeviceContext* pDeviceContext;
    D3D_FEATURE_LEVEL featureLevel;

    HRESULT hr = (*createFuncPtr)(
        NULL, D3D_DRIVER_TYPE_HARDWARE, NULL, createDeviceFlags, NULL, 0,
        D3D11_SDK_VERSION, &pd3dDevice, &featureLevel, &pDeviceContext);

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

    return pd3dDevice ? gcnew D3DDevice(pd3dDevice) : nullptr;
}

D3DDevice^ D3DDevice::CreateDevice(Adapter^ adapter, Direct3D11::DriverType driverType, String^ softwareRasterizerLibrary, Direct3D11::CreateDeviceOptions options, array<FeatureLevel>^ featureLevels)
{
    PFN_D3D11_CREATE_DEVICE createFuncPtr = 
        (PFN_D3D11_CREATE_DEVICE)LibraryLoader::Instance()->GetFunctionFromDll(
            D3D11Library, "D3D11CreateDevice");

    ID3D11Device* pd3dDevice;
    ID3D11DeviceContext* pd3dDeviceContext;
    HINSTANCE softwareModule = NULL;
    D3D_FEATURE_LEVEL featureLevel;

    if (!String::IsNullOrEmpty(softwareRasterizerLibrary))
    {
        softwareModule = CommonUtils::LoadDll(softwareRasterizerLibrary);
    }
    
    bool hasFeatures = featureLevels != nullptr && featureLevels->Length > 0;
    pin_ptr<FeatureLevel> levels = hasFeatures ? &featureLevels[0] : nullptr;

    HRESULT hr = (*createFuncPtr)( 
        adapter == nullptr ? NULL : adapter->CastInterface<IDXGIAdapter>(), 
        safe_cast<D3D_DRIVER_TYPE>(driverType),
        softwareModule,
        static_cast<UINT>(options),
        levels ? (D3D_FEATURE_LEVEL*)levels : NULL,
        levels ? featureLevels->Length : 0, 
        D3D11_SDK_VERSION, 
        &pd3dDevice, 
        &featureLevel, 
        &pd3dDeviceContext);

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

    return pd3dDevice ? gcnew D3DDevice(pd3dDevice) : nullptr;
}

CounterData^ D3DDevice::GetCounterData(CounterDescription description)
{

    D3D11_COUNTER_DESC desc;
    description.CopyTo(&desc);
    
#pragma warning(push)
    // The three buffers here are only allocated within the 'try' clause and
    // the 'finally' clause will ensure they are cleaned up.
    // cl /analysis appears to not understand the C++/CLI 'finally' syntax
#pragma warning(disable:6211)

    LPSTR name = NULL;
    LPSTR units = NULL;
    LPSTR descString = NULL;

    try
    {
        D3D11_COUNTER_TYPE type;
        UINT activeCounterCount;

        UINT nameLength = 0;
        UINT unitsLength = 0;
        UINT descriptionLength = 0;

        Validate::VerifyResult(CastInterface<ID3D11Device>()->CheckCounter(
            &desc, &type, &activeCounterCount, NULL, &nameLength,
            NULL, &unitsLength, NULL, &descriptionLength));

        name = new char[nameLength];
        units = new char[unitsLength];
        descString = new char[descriptionLength];

        Validate::VerifyResult(CastInterface<ID3D11Device>()->CheckCounter(
            &desc, &type, &activeCounterCount, name, &nameLength,
            units, &unitsLength, descString, &descriptionLength));
        
        return gcnew CounterData(
            static_cast<CounterType>(type), static_cast<int>(activeCounterCount),
            gcnew String(name), gcnew String(units), gcnew String(descString));
    }
    finally
    {
        delete [] name;
        delete [] units;
        delete [] descString;
    }

#pragma warning(pop)

}

CounterInformation D3DDevice::CounterInformation::get(void)
{
    D3D11_COUNTER_INFO info;
    CastInterface<ID3D11Device>()->CheckCounterInfo(&info);
    
    return Direct3D11::CounterInformation(info);
}

FeatureDataThreading D3DDevice::ThreadingSupport::get(void)
{
    D3D11_FEATURE_DATA_THREADING temp;

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CheckFeatureSupport(D3D11_FEATURE_THREADING, &temp, sizeof(D3D11_FEATURE_DATA_THREADING)));

    return FeatureDataThreading(temp);
}

bool D3DDevice::IsDoubleSupported::get(void)
{
    D3D11_FEATURE_DATA_DOUBLES temp;
    
    Validate::VerifyResult(CastInterface<ID3D11Device>()->CheckFeatureSupport(D3D11_FEATURE_DOUBLES, &temp, sizeof(D3D11_FEATURE_DATA_DOUBLES)));

    return temp.DoublePrecisionFloatShaderOps != FALSE;
}

FeatureDataFormatSupport D3DDevice::GetFeatureDataFormatSupport(Format format)
{
    D3D11_FEATURE_DATA_FORMAT_SUPPORT temp;

    temp.InFormat = static_cast<DXGI_FORMAT>(format);
    
    Validate::VerifyResult(CastInterface<ID3D11Device>()->CheckFeatureSupport(D3D11_FEATURE_FORMAT_SUPPORT, &temp, sizeof(D3D11_FEATURE_DATA_FORMAT_SUPPORT)));

    return FeatureDataFormatSupport(temp);
}

FeatureDataFormatSupport2 D3DDevice::GetFeatureDataFormatSupport2(Format format)
{
    D3D11_FEATURE_DATA_FORMAT_SUPPORT2 temp;

    temp.InFormat = static_cast<DXGI_FORMAT>(format);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CheckFeatureSupport(D3D11_FEATURE_FORMAT_SUPPORT2, &temp, sizeof(D3D11_FEATURE_DATA_FORMAT_SUPPORT2)));

    return FeatureDataFormatSupport2(temp);
}

bool D3DDevice::IsComputeShaderWithRawAndStructuredBuffersSupported::get(void)
{
    D3D11_FEATURE_DATA_D3D10_X_HARDWARE_OPTIONS temp;

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CheckFeatureSupport(D3D11_FEATURE_D3D10_X_HARDWARE_OPTIONS, &temp, sizeof(D3D11_FEATURE_DATA_D3D10_X_HARDWARE_OPTIONS)));

    return temp.ComputeShaders_Plus_RawAndStructuredBuffers_Via_Shader_4_x != FALSE;
}

FormatSupportOptions D3DDevice::GetFormatSupport(Format format)
{
    UINT tempoutFormatSupport = 0;
    
    Validate::VerifyResult(CastInterface<ID3D11Device>()->CheckFormatSupport(static_cast<DXGI_FORMAT>(format), &tempoutFormatSupport));
    
    return safe_cast<FormatSupportOptions>(tempoutFormatSupport);
}

UInt32 D3DDevice::GetMultisampleQualityLevels(Format format, UInt32 sampleCount)
{
    UINT tempoutNumQualityLevels;
    Validate::VerifyResult(CastInterface<ID3D11Device>()->CheckMultisampleQualityLevels(static_cast<DXGI_FORMAT>(format), sampleCount, &tempoutNumQualityLevels));
    return tempoutNumQualityLevels;
}

BlendState^ D3DDevice::CreateBlendState(BlendDescription description)
{
    ID3D11BlendState* tempoutBlendState = NULL;
    
    D3D11_BLEND_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateBlendState(&desc, &tempoutBlendState));
    
    return tempoutBlendState == NULL ? nullptr : gcnew BlendState(tempoutBlendState);
}

D3DBuffer^ D3DDevice::CreateBuffer(BufferDescription description, ... array<SubresourceData>^ initialData)
{
    if (initialData == nullptr || initialData->Length == 0)
    {
        return CreateBuffer(description);
    }

    ID3D11Buffer* buffer = NULL;
    D3D11_BUFFER_DESC desc;
    vector<D3D11_SUBRESOURCE_DATA> data;

    description.CopyTo(desc);
    Utilities::Convert::CopyToStructsVector((IEnumerable<SubresourceData>^)initialData, data);
    
    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateBuffer(
        &desc, &data[0], &buffer));

    return buffer == NULL ? nullptr : gcnew D3DBuffer(buffer);
}

D3DBuffer^ D3DDevice::CreateBuffer(BufferDescription description)
{
    ID3D11Buffer* tempoutBuffer = NULL;
    pin_ptr<BufferDescription> ptr = &description;

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateBuffer(
        (D3D11_BUFFER_DESC*)ptr, 
        NULL, 
        &tempoutBuffer));

    return tempoutBuffer == NULL ? nullptr : gcnew D3DBuffer(tempoutBuffer);
}
ClassLinkage^ D3DDevice::CreateClassLinkage()
{
    ID3D11ClassLinkage* tempoutLinkage = NULL;
    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateClassLinkage(&tempoutLinkage));
    return tempoutLinkage == NULL ? nullptr : gcnew ClassLinkage(tempoutLinkage);
}

D3DCounter^ D3DDevice::CreateCounter(CounterDescription description)
{
    ID3D11Counter* tempoutCounter = NULL;

    D3D11_COUNTER_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateCounter(&desc, &tempoutCounter));
    
    return tempoutCounter == NULL ? nullptr : gcnew D3DCounter(tempoutCounter);
}

DeviceContext^ D3DDevice::CreateDeferredContext(UInt32 contextOptions)
{
    ID3D11DeviceContext* tempoutDeferredContext = NULL;
    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateDeferredContext(static_cast<UINT>(contextOptions), &tempoutDeferredContext));
    return tempoutDeferredContext == NULL ? nullptr : gcnew DeviceContext(tempoutDeferredContext);
}

DepthStencilState^ D3DDevice::CreateDepthStencilState(DepthStencilDescription description)
{
    ID3D11DepthStencilState* tempoutDepthStencilState = NULL;

    D3D11_DEPTH_STENCIL_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateDepthStencilState(&desc, &tempoutDepthStencilState));
    return tempoutDepthStencilState == NULL ? nullptr : gcnew DepthStencilState(tempoutDepthStencilState);
}

DepthStencilView^  D3DDevice::CreateDepthStencilView(D3DResource^ resource, DepthStencilViewDescription description)
{
    Validate::CheckNull(resource, "resource");

    D3D11_DEPTH_STENCIL_VIEW_DESC desc;
    description.CopyTo(&desc);

    ID3D11DepthStencilView* tempoutDepthStencilView = NULL;
    
    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateDepthStencilView(
        resource->CastInterface<ID3D11Resource>(), 
        &desc, 
        &tempoutDepthStencilView));

    return tempoutDepthStencilView == NULL ? nullptr : gcnew DepthStencilView(tempoutDepthStencilView);
}

DepthStencilView^ D3DDevice::CreateDepthStencilView(D3DResource^ resource)
{
    Validate::CheckNull(resource, "resource");

    ID3D11DepthStencilView* tempoutDepthStencilView = NULL;

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateDepthStencilView(
        resource->CastInterface<ID3D11Resource>(), 
        NULL,
        &tempoutDepthStencilView));

    return tempoutDepthStencilView == NULL ? nullptr : gcnew DepthStencilView(tempoutDepthStencilView);
}

ComputeShader^ D3DDevice::CreateComputeShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength, ClassLinkage^ classLinkage)
{
    ID3D11ComputeShader* tempShader = NULL;

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateComputeShader(
        shaderBytecode.ToPointer(), 
        shaderBytecodeLength, 
        classLinkage == nullptr ? NULL : classLinkage->CastInterface<ID3D11ClassLinkage>(), 
        &tempShader));

    return tempShader == NULL ? nullptr : gcnew ComputeShader(tempShader);
}


ComputeShader^ D3DDevice::CreateComputeShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength)
{
    return CreateComputeShader(shaderBytecode, shaderBytecodeLength, nullptr);
}

ComputeShader^ D3DDevice::CreateComputeShader(Stream^ stream, ClassLinkage^ classLinkage)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateComputeShader(IntPtr(data), dataArray->Length, classLinkage);

}

ComputeShader^ D3DDevice::CreateComputeShader(Stream^ stream)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateComputeShader(IntPtr(data), dataArray->Length, nullptr);
}


DomainShader^ D3DDevice::CreateDomainShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength, ClassLinkage^ classLinkage)
{
    ID3D11DomainShader* tempShader = NULL;
    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateDomainShader(
        shaderBytecode.ToPointer(), 
        shaderBytecodeLength, 
        classLinkage == nullptr ? NULL : classLinkage->CastInterface<ID3D11ClassLinkage>(), 
        &tempShader));

    return tempShader == NULL ? nullptr : gcnew DomainShader(tempShader);
}

DomainShader^ D3DDevice::CreateDomainShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength)
{
    return CreateDomainShader(shaderBytecode, shaderBytecodeLength, nullptr);
}

DomainShader^ D3DDevice::CreateDomainShader(Stream^ stream, ClassLinkage^ classLinkage)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateDomainShader(IntPtr(data), dataArray->Length, classLinkage);

}

DomainShader^ D3DDevice::CreateDomainShader(Stream^ stream)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateDomainShader (IntPtr(data), dataArray->Length, nullptr);

}

GeometryShader^ D3DDevice::CreateGeometryShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength, ClassLinkage^ classLinkage)
{
    ID3D11GeometryShader* tempShader = NULL;
    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateGeometryShader(
        shaderBytecode.ToPointer(), 
        shaderBytecodeLength, 
        classLinkage == nullptr ? NULL : classLinkage->CastInterface<ID3D11ClassLinkage>(), 
        &tempShader));

    return tempShader == NULL ? nullptr : gcnew GeometryShader(tempShader);
}

GeometryShader^ D3DDevice::CreateGeometryShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength)
{
    return CreateGeometryShader(shaderBytecode, shaderBytecodeLength, nullptr);
}

GeometryShader^ D3DDevice::CreateGeometryShader(Stream^ stream, ClassLinkage^ classLinkage)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateGeometryShader(IntPtr(data), dataArray->Length, classLinkage);

}

GeometryShader^ D3DDevice::CreateGeometryShader(Stream^ stream)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateGeometryShader(IntPtr(data), dataArray->Length, nullptr);
}


GeometryShader^ D3DDevice::CreateGeometryShaderWithStreamOutput(IntPtr shaderBytecode, UInt32 shaderBytecodeLength, IEnumerable<StreamOutputDeclarationEntry>^ SODeclarations, 
    array<UInt32>^ bufferStrides, UInt32 rasterizedStream, ClassLinkage^ classLinkage)
{
    ID3D11GeometryShader* tempoutGeometryShader = NULL;

    vector<D3D11_SO_DECLARATION_ENTRY> declEntries;
    marshal_context^ ctx = gcnew marshal_context();
    UINT declEntriesLen = Utilities::Convert::FillValStructsVector<StreamOutputDeclarationEntry,D3D11_SO_DECLARATION_ENTRY>(SODeclarations, declEntries, ctx);

    pin_ptr<UInt32> strides;
    
    if (bufferStrides != nullptr && bufferStrides->Length > 0)
    {
        strides = &(bufferStrides[ 0 ]);
    }
    else
    {
        strides = nullptr;
    }

    Validate::VerifyResult(
        CastInterface<ID3D11Device>()->CreateGeometryShaderWithStreamOutput(
        shaderBytecode.ToPointer(), 
        shaderBytecodeLength, 
        declEntriesLen == 0 ? NULL : &declEntries[0], 
        declEntriesLen, 
        strides, 
        static_cast<UINT>(bufferStrides->Length), 
        static_cast<UINT>(rasterizedStream), 
        classLinkage == nullptr ? NULL : classLinkage->CastInterface<ID3D11ClassLinkage>(), 
        &tempoutGeometryShader));

	delete ctx;
        
    return tempoutGeometryShader == NULL ? nullptr : gcnew GeometryShader(tempoutGeometryShader);
}

GeometryShader^ D3DDevice::CreateGeometryShaderWithStreamOutput(IntPtr shaderBytecode, UInt32 shaderBytecodeLength, IEnumerable<StreamOutputDeclarationEntry>^ SODeclarations, 
    array<UInt32>^ bufferStrides, UInt32 rasterizedStream)
{
    return CreateGeometryShaderWithStreamOutput(shaderBytecode, shaderBytecodeLength, SODeclarations, 
            bufferStrides, rasterizedStream, nullptr);
}

GeometryShader^ D3DDevice::CreateGeometryShaderWithStreamOutput(Stream^ stream, IEnumerable<StreamOutputDeclarationEntry>^ SODeclarations, 
    array<UInt32>^ bufferStrides, UInt32 rasterizedStream, ClassLinkage^ classLinkage)
{

    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateGeometryShaderWithStreamOutput(IntPtr(data), dataArray->Length, SODeclarations, 
            bufferStrides, rasterizedStream, classLinkage);

}
    
GeometryShader^ D3DDevice::CreateGeometryShaderWithStreamOutput(Stream^ stream, IEnumerable<StreamOutputDeclarationEntry>^ SODeclarations, 
    array<UInt32>^ bufferStrides, UInt32 rasterizedStream)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateGeometryShaderWithStreamOutput(IntPtr(data), dataArray->Length, SODeclarations, 
            bufferStrides, rasterizedStream, nullptr);

}

HullShader^ D3DDevice::CreateHullShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength, ClassLinkage^ classLinkage)
{
    ID3D11HullShader* tempShader = NULL;
    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateHullShader(
        shaderBytecode.ToPointer(), 
        shaderBytecodeLength, 
        classLinkage == nullptr ? NULL : classLinkage->CastInterface<ID3D11ClassLinkage>(), 
        &tempShader));

    return tempShader == NULL ? nullptr : gcnew HullShader(tempShader);
}

HullShader^ D3DDevice::CreateHullShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength)
{
    return CreateHullShader(shaderBytecode, shaderBytecodeLength, nullptr);
}

HullShader^ D3DDevice::CreateHullShader(Stream^ stream, ClassLinkage^ classLinkage)
{        
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateHullShader(IntPtr(data), dataArray->Length, classLinkage);

}

HullShader^ D3DDevice::CreateHullShader(Stream^ stream)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateHullShader(IntPtr(data), dataArray->Length, nullptr);
}

InputLayout^ D3DDevice::CreateInputLayout(
    IEnumerable<InputElementDescription>^ inputElementDescriptions, 
    IntPtr shaderBytecodeWithInputSignature, 
    UInt32 shaderBytecodeWithInputSignatureSize)
{
    if (Object::ReferenceEquals(inputElementDescriptions, nullptr))
    {
        throw gcnew ArgumentNullException("inputElementDescriptions");
    }

    vector<D3D11_INPUT_ELEMENT_DESC> inputElements;

    marshal_context^ ctx = gcnew marshal_context();

    UINT count = Utilities::Convert::FillValStructsVector<InputElementDescription, D3D11_INPUT_ELEMENT_DESC>(
        inputElementDescriptions, inputElements, ctx);

    if (count == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "inputElementDescriptions");
    }

    ID3D11InputLayout* tempoutInputLayout = NULL;

    Validate::VerifyResult(
        CastInterface<ID3D11Device>()->CreateInputLayout(
        &inputElements[0],
        count,
        shaderBytecodeWithInputSignature.ToPointer(), 
        shaderBytecodeWithInputSignatureSize, 
        &tempoutInputLayout));

    delete ctx;

    return tempoutInputLayout == NULL ? nullptr : gcnew InputLayout(tempoutInputLayout);
}

InputLayout^ D3DDevice::CreateInputLayout(
    IEnumerable<InputElementDescription>^ inputElementDescriptions, 
    Stream^ stream)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateInputLayout(inputElementDescriptions, IntPtr(data), dataArray->Length);
}

PixelShader^ D3DDevice::CreatePixelShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength, ClassLinkage^ classLinkage)
{
    ID3D11PixelShader* tempoutPixelShader = NULL;
    Validate::VerifyResult(
        CastInterface<ID3D11Device>()->CreatePixelShader(
            shaderBytecode.ToPointer(), 
            shaderBytecodeLength, 
            classLinkage == nullptr ? NULL : classLinkage->CastInterface<ID3D11ClassLinkage>(), 
            &tempoutPixelShader));
    
    return tempoutPixelShader == NULL ? nullptr : gcnew PixelShader(tempoutPixelShader);
}

PixelShader^ D3DDevice::CreatePixelShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength)
{
    return CreatePixelShader(shaderBytecode, shaderBytecodeLength, nullptr);
}

PixelShader^ D3DDevice::CreatePixelShader(Stream^ stream, ClassLinkage^ classLinkage)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreatePixelShader(IntPtr(data), dataArray->Length, classLinkage);
}

PixelShader^ D3DDevice::CreatePixelShader(Stream^ stream)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreatePixelShader(IntPtr(data), dataArray->Length, nullptr);
}

D3DPredicate^ D3DDevice::CreatePredicate(QueryDescription description)
{
    ID3D11Predicate* predicate = NULL;
    D3D11_QUERY_DESC desc;

    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreatePredicate(&desc, &predicate));

    return predicate == NULL ? nullptr : gcnew D3DPredicate(predicate);
}

D3DQuery^ D3DDevice::CreateQuery(QueryDescription description)
{
    ID3D11Query* tempoutQuery = NULL;

    D3D11_QUERY_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateQuery(&desc, &tempoutQuery));
    return tempoutQuery == NULL ? nullptr : gcnew D3DQuery(tempoutQuery);
}

RasterizerState^ D3DDevice::CreateRasterizerState(RasterizerDescription description)
{
    ID3D11RasterizerState* tempoutRasterizerState = NULL;

    D3D11_RASTERIZER_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateRasterizerState(
        &desc, &tempoutRasterizerState));

    return tempoutRasterizerState == NULL ? nullptr : gcnew RasterizerState(tempoutRasterizerState);
}
RenderTargetView^ D3DDevice::CreateRenderTargetView(D3DResource^ resource, RenderTargetViewDescription description)
{
    Validate::CheckNull(resource, "resource");

    ID3D11RenderTargetView* tempoutRTView = NULL;
    pin_ptr<RenderTargetViewDescription> ptr = &description;

    Validate::VerifyResult(
        CastInterface<ID3D11Device>()->CreateRenderTargetView(
        resource->CastInterface<ID3D11Resource>(), 
        (D3D11_RENDER_TARGET_VIEW_DESC*)ptr, 
        &tempoutRTView));
    return tempoutRTView == NULL ? nullptr : gcnew RenderTargetView(tempoutRTView);
}

RenderTargetView^ D3DDevice::CreateRenderTargetView(D3DResource^ resource)
{
    Validate::CheckNull(resource, "resource");

    ID3D11RenderTargetView* tempoutRTView = NULL;

    Validate::VerifyResult(
        CastInterface<ID3D11Device>()->CreateRenderTargetView(
        resource->CastInterface<ID3D11Resource>(), 
        NULL, 
        &tempoutRTView));
    return tempoutRTView == NULL ? nullptr : gcnew RenderTargetView(tempoutRTView);
}

SamplerState^ D3DDevice::CreateSamplerState(SamplerDescription description)
{
    ID3D11SamplerState* tempoutSamplerState = NULL;
    D3D11_SAMPLER_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateSamplerState(&desc, &tempoutSamplerState));
    return tempoutSamplerState == NULL ? nullptr : gcnew SamplerState(tempoutSamplerState);
}

ShaderResourceView^ D3DDevice::CreateShaderResourceView(D3DResource^ resource, ShaderResourceViewDescription description)
{
    Validate::CheckNull(resource, "resource");

    ID3D11ShaderResourceView* tempoutSRView = NULL;
    D3D11_SHADER_RESOURCE_VIEW_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateShaderResourceView(
        resource->CastInterface<ID3D11Resource>(), 
        &desc, 
        &tempoutSRView));

    return tempoutSRView == NULL ? nullptr : gcnew ShaderResourceView(tempoutSRView);
}

ShaderResourceView^ D3DDevice::CreateShaderResourceView(D3DResource^ resource)
{
    Validate::CheckNull(resource, "resource");

    ID3D11ShaderResourceView* tempoutSRView = NULL;

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateShaderResourceView(
        resource->CastInterface<ID3D11Resource>(), 
        NULL,
        &tempoutSRView));

    return tempoutSRView == NULL ? nullptr : gcnew ShaderResourceView(tempoutSRView);
}

Texture1D^ D3DDevice::CreateTexture1D(Texture1DDescription description, ... array<SubresourceData>^ initialData)
{
    if (initialData == nullptr || initialData->Length == 0)
    {
        return CreateTexture1D(description);
    }

    ID3D11Texture1D *texture = NULL;
    D3D11_TEXTURE1D_DESC desc;
    vector<D3D11_SUBRESOURCE_DATA> subresourceVector;

    description.CopyTo(desc);
    subresourceVector.reserve(initialData->Length);
    Utilities::Convert::CopyToStructsVector((IEnumerable<SubresourceData>^)initialData, subresourceVector);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateTexture1D(
        &desc, &subresourceVector[0], &texture));
    
    return texture == NULL ? nullptr : gcnew Texture1D(texture);
}

Texture2D^ D3DDevice::CreateTexture2D(Texture2DDescription description, ... array<SubresourceData>^ initialData)
{
    if (initialData == nullptr || initialData->Length == 0)
    {
        return CreateTexture2D(description);
    }

    ID3D11Texture2D *texture = NULL;
    D3D11_TEXTURE2D_DESC desc;
    vector<D3D11_SUBRESOURCE_DATA> subresourceVector;

    description.CopyTo(desc);
    subresourceVector.reserve(initialData->Length);
    Utilities::Convert::CopyToStructsVector((IEnumerable<SubresourceData>^)initialData, subresourceVector);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateTexture2D(
        &desc, &subresourceVector[0], &texture));

    return texture == NULL ? nullptr : gcnew Texture2D(texture);
}

Texture3D^ D3DDevice::CreateTexture3D(Texture3DDescription description, ... array<SubresourceData>^ initialData)
{
    if (initialData == nullptr || initialData->Length == 0)
    {
        return CreateTexture3D(description);
    }

    ID3D11Texture3D *texture = NULL;
    D3D11_TEXTURE3D_DESC desc;
    vector<D3D11_SUBRESOURCE_DATA> subresourceVector;

    description.CopyTo(desc);
    subresourceVector.reserve(initialData->Length);
    Utilities::Convert::CopyToStructsVector((IEnumerable<SubresourceData>^)initialData, subresourceVector);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateTexture3D(
        &desc, &subresourceVector[0], &texture));

    return texture == NULL ? nullptr : gcnew Texture3D(texture);
}

Texture1D^ D3DDevice::CreateTexture1D(Texture1DDescription description)
{
    ID3D11Texture1D *texture = NULL;
    D3D11_TEXTURE1D_DESC desc;

    description.CopyTo(desc);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateTexture1D(
        &desc, NULL, &texture));
    
    return texture == NULL ? nullptr : gcnew Texture1D(texture);
}

Texture2D^ D3DDevice::CreateTexture2D(Texture2DDescription description)
{
    ID3D11Texture2D *texture = NULL;
    D3D11_TEXTURE2D_DESC desc;

    description.CopyTo(desc);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateTexture2D(
        &desc, NULL, &texture));

    return texture == NULL ? nullptr : gcnew Texture2D(texture);
}

Texture3D^ D3DDevice::CreateTexture3D(Texture3DDescription description)
{
    ID3D11Texture3D *texture = NULL;
    D3D11_TEXTURE3D_DESC desc;

    description.CopyTo(desc);

    Validate::VerifyResult(CastInterface<ID3D11Device>()->CreateTexture3D(
        &desc, NULL, &texture));

    return texture == NULL ? nullptr : gcnew Texture3D(texture);
}

UnorderedAccessView^ D3DDevice::CreateUnorderedAccessView(D3DResource^ resource, UnorderedAccessViewDescription description)
{
    Validate::CheckNull(resource, "resource");
    D3D11_UNORDERED_ACCESS_VIEW_DESC desc;
    description.CopyTo(&desc);


    ID3D11UnorderedAccessView* tempoutUAView = NULL;
    Validate::VerifyResult(
        CastInterface<ID3D11Device>()->CreateUnorderedAccessView(
            resource->CastInterface<ID3D11Resource>(), 
            &desc, 
            &tempoutUAView));
    
    return tempoutUAView == NULL ? nullptr : gcnew UnorderedAccessView(tempoutUAView);
}

UnorderedAccessView^ D3DDevice::CreateUnorderedAccessView(D3DResource^ resource)
{
    Validate::CheckNull(resource, "resource");

    ID3D11UnorderedAccessView* tempoutUAView = NULL;

    Validate::VerifyResult(
        CastInterface<ID3D11Device>()->CreateUnorderedAccessView(
            resource->CastInterface<ID3D11Resource>(), 
            NULL, 
            &tempoutUAView));
    
    return tempoutUAView == NULL ? nullptr : gcnew UnorderedAccessView(tempoutUAView);
}
VertexShader^ D3DDevice::CreateVertexShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength, ClassLinkage^ classLinkage)
{
    ID3D11VertexShader* tempoutVertexShader = NULL;
    Validate::VerifyResult(
        CastInterface<ID3D11Device>()->CreateVertexShader(
            shaderBytecode.ToPointer(), 
            shaderBytecodeLength, 
            classLinkage == nullptr ? NULL : classLinkage->CastInterface<ID3D11ClassLinkage>(), 
            &tempoutVertexShader));
    
    return tempoutVertexShader == NULL ? nullptr : gcnew VertexShader(tempoutVertexShader);
}

VertexShader^ D3DDevice::CreateVertexShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength)
{
    return CreateVertexShader(shaderBytecode, shaderBytecodeLength, nullptr);
}

VertexShader^ D3DDevice::CreateVertexShader(Stream^ stream, ClassLinkage^ classLinkage)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateVertexShader(IntPtr(data), dataArray->Length, classLinkage);
}

VertexShader^ D3DDevice::CreateVertexShader(Stream^ stream)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateVertexShader(IntPtr(data), dataArray->Length, nullptr);
}

CreateDeviceOptions D3DDevice::CreateDeviceOptions::get()
{
    UINT returnValue = CastInterface<ID3D11Device>()->GetCreationFlags();
	return safe_cast<Direct3D11::CreateDeviceOptions>(returnValue);
}

ErrorCode D3DDevice::DeviceRemovedReason::get()
{
    return safe_cast<ErrorCode>(CastInterface<ID3D11Device>()->GetDeviceRemovedReason());
}

ExceptionErrors D3DDevice::ExceptionMode::get()
{
    UINT returnValue = CastInterface<ID3D11Device>()->GetExceptionMode();
    return safe_cast<ExceptionErrors>(returnValue);
}

void D3DDevice::ExceptionMode::set(ExceptionErrors value)
{
    Validate::VerifyResult(CastInterface<ID3D11Device>()->SetExceptionMode(static_cast<UINT>(value)));
}

DeviceContext^ D3DDevice::ImmediateContext::get(void)
{
    ID3D11DeviceContext* tempoutImmediateContext = NULL;
    CastInterface<ID3D11Device>()->GetImmediateContext(&tempoutImmediateContext);
    return tempoutImmediateContext == NULL ? nullptr : gcnew DeviceContext(tempoutImmediateContext);
}

Direct3D11::InfoQueue ^ D3DDevice::InfoQueue::get(void)
{
    ID3D11InfoQueue * infoQueue = NULL;

    Validate::VerifyResult(CastInterface<ID3D11Device>()->QueryInterface(__uuidof(ID3D11InfoQueue), (void**) &infoQueue));

    return infoQueue == NULL ? nullptr : gcnew Direct3D11::InfoQueue(infoQueue);
}

Direct3D11::SwitchToRef ^ D3DDevice::SwitchToRef::get(void)
{
    ID3D11SwitchToRef * switchToRef = NULL;

    Validate::VerifyResult(CastInterface<ID3D11Device>()->QueryInterface(__uuidof(ID3D11SwitchToRef), (void**) &switchToRef));

    return switchToRef == NULL ? nullptr : gcnew Direct3D11::SwitchToRef(switchToRef);
}

generic <typename T> where T : DirectUnknown
T D3DDevice::OpenSharedResource(IntPtr resource)
{
    void* tempoutResource = NULL;

    GUID guid = CommonUtils::GetGuid(T::typeid);
    
    Validate::VerifyResult(CastInterface<ID3D11Device>()->OpenSharedResource(static_cast<HANDLE>(resource.ToPointer()), guid, &tempoutResource));
    
    return Utilities::Convert::CreateIUnknownWrapper<T>(static_cast<IUnknown*>(tempoutResource));
}

FeatureLevel D3DDevice::DeviceFeatureLevel::get()
{
    return static_cast<FeatureLevel>(CastInterface<ID3D11Device>()->GetFeatureLevel());
}

Device^ D3DDevice::GraphicsDevice::get(void)
{
    IDXGIDevice * pDXGIDevice = NULL;
    Validate::VerifyResult(CastInterface<IUnknown>()->QueryInterface(__uuidof(IDXGIDevice), (void **)&pDXGIDevice));

    return pDXGIDevice == NULL ? nullptr : gcnew Device(pDXGIDevice);
}

Device1^ D3DDevice::GraphicsDevice1::get()
{
    IDXGIDevice1 * pDXGIDevice = NULL;
    Validate::VerifyResult(CastInterface<IUnknown>()->QueryInterface(__uuidof(IDXGIDevice1), (void **)&pDXGIDevice));

    return pDXGIDevice == NULL ? nullptr : gcnew Device1(pDXGIDevice);
}

D3DDebug^ D3DDevice::Debug::get(void)
{
	ID3D11Debug *debugInterface;

	Validate::VerifyResult(CastInterface<IUnknown>()->QueryInterface(__uuidof(ID3D11Debug), (void **)&debugInterface));

	return debugInterface == NULL ? nullptr : gcnew D3DDebug(debugInterface);
}