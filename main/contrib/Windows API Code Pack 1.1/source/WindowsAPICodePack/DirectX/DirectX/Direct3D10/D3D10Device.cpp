// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"

#include <vector>
#include "LibraryLoader.h"
#include "D3D10Device.h"
#include "D3D10DepthStencilView.h"
#include "D3D10RenderTargetView.h"
#include "D3D10Resource.h"
#include "D3D10BlendState.h"
#include "D3D10Buffer.h"
#include "D3D10DepthStencilState.h"
#include "D3D10GeometryShader.h"
#include "D3D10InputLayout.h"
#include "D3D10PixelShader.h"
#include "D3D10RasterizerState.h"
#include "D3D10SamplerState.h"
#include "D3D10ShaderResourceView.h"
#include "D3D10Texture1D.h"
#include "D3D10Texture2D.h"
#include "D3D10Texture3D.h"
#include "D3D10VertexShader.h"
#include "D3D10SwitchToRef.h"
#include "D3D10Debug.h"
#include "D3D10GeometryShaderPipelineStage.h"
#include "D3D10InputAssemblerPipelineStage.h"
#include "D3D10OutputMergerPipelineStage.h"
#include "D3D10PixelShaderPipelineStage.h"
#include "D3D10RasterizerPipelineStage.h"
#include "D3D10StreamOutputPipelineStage.h"
#include "D3D10VertexShaderPipelineStage.h"
#include "D3D10Effect.h"
#include "D3D10Counter.h"
#include "D3D10Predicate.h"
#include "D3D10Query.h"
#include "D3D10InfoQueue.h"
#include "D3D10Multithread.h"

#include "DXGI/DXGISwapChain.h"
#include "DXGI/DXGIAdapter.h"
#include "DXGI/DXGIDevice.h"
#include "DXGI/DXGIDevice1.h"
#include "DXGI/DXGIObject.h"


using namespace std;

#pragma unmanaged

typedef HRESULT (WINAPI *D3D10CreateEffectFromMemoryPtr)(
    void *pData,
    SIZE_T DataLength,
    UINT FXFlags,
    ID3D10Device *pDevice, 
    ID3D10EffectPool *pEffectPool,
    ID3D10Effect **ppEffect
);

// REVIEW: surely there's a better way to expose this function and others like it.
// At the very least, how about proper static methods in a suitable class somewhere?
// And do we really need to inspect the DLL explicitly? What's wrong with just linking
// against D3D10.DLL, or at least handling the "is DLL present" question at a higher
// level?
HRESULT CreateEffectFromMemory(                                     
    void *pData,
    SIZE_T DataLength,
    UINT FXFlags,
    ID3D10Device *pDevice, 
    ID3D10EffectPool *pEffectPool,
    ID3D10Effect **ppEffect)
{
    D3D10CreateEffectFromMemoryPtr createEffectFuncPtr = 
        (D3D10CreateEffectFromMemoryPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10Library, "D3D10CreateEffectFromMemory");

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

using namespace System::IO;
using namespace System::Runtime::InteropServices;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;



typedef HRESULT (WINAPI *D3D10CreateDeviceAndSwapChainFuncPtr)(
    IDXGIAdapter *pAdapter,
    D3D10_DRIVER_TYPE DriverType,
    HMODULE Software,
    UINT Flags,
    UINT SDKVersion,
    DXGI_SWAP_CHAIN_DESC *pSwapChainDesc,
    IDXGISwapChain **ppSwapChain,    
    ID3D10Device **ppDevice
);

typedef HRESULT (WINAPI *D3D10CreateDeviceFuncPtr)(
    IDXGIAdapter *pAdapter,
    D3D10_DRIVER_TYPE DriverType,
    HMODULE Software,
    UINT Flags,
    UINT SDKVersion,
    ID3D10Device **ppDevice
);

typedef HRESULT (WINAPI *D3D10CreateStateBlockFuncPtr)(
    ID3D10Device *pDevice,
    D3D10_STATE_BLOCK_MASK *pStateBlockMask,
    ID3D10StateBlock **ppStateBlock
);


D3DDevice^ D3DDevice::CreateDeviceAndSwapChain(IntPtr windowHandle)
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
#endif

    if (!String::IsNullOrEmpty(System::Environment::GetEnvironmentVariable("DXSDK_DIR")))
    {
        options  = Direct3D10::CreateDeviceOptions::Debug;
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

    return CreateDeviceAndSwapChain(nullptr, Direct3D10::DriverType::Hardware, nullptr, options, swapChainDescription);
}


D3DDevice^ D3DDevice::CreateDeviceAndSwapChain(Adapter^ adapter, Direct3D10::DriverType driverType, String^ softwareRasterizerLibrary, Direct3D10::CreateDeviceOptions options, SwapChainDescription swapChainDescription)
{
    DXGI_SWAP_CHAIN_DESC nativeDesc = {0};
    swapChainDescription.CopyTo(&nativeDesc);

    D3D10CreateDeviceAndSwapChainFuncPtr createFuncPtr = 
        (D3D10CreateDeviceAndSwapChainFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10Library, "D3D10CreateDeviceAndSwapChain");

    ID3D10Device* pd3dDevice = NULL;
    IDXGISwapChain* pSwapChain = NULL;

    HINSTANCE softwareModule = NULL;
    if (!String::IsNullOrEmpty(softwareRasterizerLibrary))
    {
        softwareModule = CommonUtils::LoadDll(softwareRasterizerLibrary);
    }

    HRESULT hr = (*createFuncPtr)( 
        adapter == nullptr ? NULL : adapter->CastInterface<IDXGIAdapter>(), 
        static_cast<D3D10_DRIVER_TYPE>(driverType),
        softwareModule,
        static_cast<UINT>(options),
        D3D10_SDK_VERSION, 
        &nativeDesc, 
        &pSwapChain, 
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
    D3D10CreateDeviceFuncPtr createFuncPtr = 
        (D3D10CreateDeviceFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10Library, "D3D10CreateDevice");

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

    ID3D10Device* pd3dDevice = NULL;

    HRESULT hr = (*createFuncPtr)( NULL, D3D10_DRIVER_TYPE_HARDWARE, NULL, createDeviceFlags,
                                        D3D10_SDK_VERSION, &pd3dDevice);

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

D3DDevice^ D3DDevice::CreateDevice(Adapter^ adapter, Microsoft::WindowsAPICodePack::DirectX::Direct3D10::DriverType driverType, String^ softwareRasterizerLibrary, Direct3D10::CreateDeviceOptions options)
{
    D3D10CreateDeviceFuncPtr createFuncPtr = 
        (D3D10CreateDeviceFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10Library, "D3D10CreateDevice");

    ID3D10Device* pd3dDevice = NULL;

    HINSTANCE softwareModule = NULL;
    if (!String::IsNullOrEmpty(softwareRasterizerLibrary))
    {
        softwareModule = CommonUtils::LoadDll(softwareRasterizerLibrary);
    }

    HRESULT hr = (*createFuncPtr)( 
        adapter == nullptr ? NULL : adapter->CastInterface<IDXGIAdapter>(), 
        static_cast<D3D10_DRIVER_TYPE>(driverType),
        softwareModule,
        static_cast<UINT>(options),
        D3D10_SDK_VERSION, 
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

    return pd3dDevice ? gcnew D3DDevice(pd3dDevice) : nullptr;
}

CounterData^ D3DDevice::GetCounterData(CounterDescription description)
{

    D3D10_COUNTER_DESC desc;
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
        D3D10_COUNTER_TYPE type;
        UINT activeCounterCount;

        UINT nameLength = 0;
        UINT unitsLength = 0;
        UINT descriptionLength = 0;

        Validate::VerifyResult(CastInterface<ID3D10Device>()->CheckCounter(
            &desc, &type, &activeCounterCount, NULL, &nameLength, NULL,
            &unitsLength, NULL, &descriptionLength));

        name = new char[nameLength];
        units = new char[unitsLength];
        descString = new char[descriptionLength];

        Validate::VerifyResult(CastInterface<ID3D10Device>()->CheckCounter(
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

CounterInformation D3DDevice::CounterInformation::get()
{
    D3D10_COUNTER_INFO info;

    CastInterface<ID3D10Device>()->CheckCounterInfo(&info);
    
    return Direct3D10::CounterInformation(info);
}

FormatSupportOptions D3DDevice::GetFormatSupport(Format format)
{
    UINT tempoutFormatSupport = 0;
    
    Validate::VerifyResult(CastInterface<ID3D10Device>()->CheckFormatSupport(static_cast<DXGI_FORMAT>(format), &tempoutFormatSupport));
    
    return static_cast<FormatSupportOptions>(tempoutFormatSupport);
}

UInt32 D3DDevice::GetMultisampleQualityLevels(Format format, UInt32 sampleCount)
{
    UINT tempoutNumQualityLevels;
    Validate::VerifyResult(CastInterface<ID3D10Device>()->CheckMultisampleQualityLevels(static_cast<DXGI_FORMAT>(format), static_cast<UINT>(sampleCount), &tempoutNumQualityLevels));
    return tempoutNumQualityLevels;
}

void D3DDevice::ClearDepthStencilView(DepthStencilView^ depthStencilView, ClearOptions options, Single depth, Byte stencil)
{
    CastInterface<ID3D10Device>()->ClearDepthStencilView(depthStencilView->CastInterface<ID3D10DepthStencilView>(), static_cast<UINT>(options), static_cast<FLOAT>(depth), static_cast<UINT8>(stencil));
}

void D3DDevice::ClearRenderTargetView(RenderTargetView^ renderTargetView, ColorRgba colorRgba)
{
    pin_ptr<ColorRgba> rgba = &colorRgba;
    CastInterface<ID3D10Device>()->ClearRenderTargetView(renderTargetView->CastInterface<ID3D10RenderTargetView>(), (float*)rgba);
}
void D3DDevice::ClearState()
{
    CastInterface<ID3D10Device>()->ClearState();
}

void D3DDevice::CopyResource(D3DResource^ destinationResource, D3DResource^ sourceResource)
{
    CastInterface<ID3D10Device>()->CopyResource(destinationResource->CastInterface<ID3D10Resource>(), sourceResource->CastInterface<ID3D10Resource>());
}

void D3DDevice::CopySubresourceRegion(D3DResource^ destinationResource, UInt32 destinationSubresource, UInt32 destinationX, UInt32 destinationY, UInt32 destinationZ, D3DResource^ sourceResource, UInt32 sourceSubresource, Box sourceBox)
{
    pin_ptr<Box> boxPtr = &sourceBox;
    CastInterface<ID3D10Device>()->CopySubresourceRegion(destinationResource->CastInterface<ID3D10Resource>(), static_cast<UINT>(destinationSubresource), static_cast<UINT>(destinationX), static_cast<UINT>(destinationY), static_cast<UINT>(destinationZ), sourceResource->CastInterface<ID3D10Resource>(), static_cast<UINT>(sourceSubresource), (D3D10_BOX*)boxPtr);
}

void D3DDevice::CopySubresourceRegion(D3DResource^ destinationResource, UInt32 destinationSubresource, UInt32 destinationX, UInt32 destinationY, UInt32 destinationZ, D3DResource^ sourceResource, UInt32 sourceSubresource)
{
    CastInterface<ID3D10Device>()->CopySubresourceRegion(destinationResource->CastInterface<ID3D10Resource>(), static_cast<UINT>(destinationSubresource), static_cast<UINT>(destinationX), static_cast<UINT>(destinationY), static_cast<UINT>(destinationZ), sourceResource->CastInterface<ID3D10Resource>(), static_cast<UINT>(sourceSubresource), NULL);
}

BlendState^ D3DDevice::CreateBlendState(BlendDescription description)
{
    ID3D10BlendState* tempoutBlendState = NULL;

    D3D10_BLEND_DESC desc = {0};
    description.CopyTo(&desc);
    
    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateBlendState(
        &desc, &tempoutBlendState));
    
    return tempoutBlendState == NULL ? nullptr : gcnew BlendState(tempoutBlendState);
}

D3DBuffer^ D3DDevice::CreateBuffer(BufferDescription description, ... array<SubresourceData>^ initialData)
{
    if (initialData == nullptr || initialData->Length == 0)
    {
        return CreateBuffer(description);
    }

    ID3D10Buffer* buffer = NULL;
    D3D10_BUFFER_DESC desc;
    vector<D3D10_SUBRESOURCE_DATA> data;

    description.CopyTo(desc);
    Utilities::Convert::CopyToStructsVector((IEnumerable<SubresourceData>^)initialData, data);
    
    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateBuffer(
        &desc, &data[0], &buffer));

    return buffer == NULL ? nullptr : gcnew D3DBuffer(buffer);
}

D3DBuffer^ D3DDevice::CreateBuffer(BufferDescription description)
{
    ID3D10Buffer* tempoutBuffer = NULL;
    
    D3D10_BUFFER_DESC nativeDesc = {0};
    description.CopyTo(nativeDesc);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateBuffer(
        &nativeDesc, 
        NULL, 
        &tempoutBuffer));
    
    return tempoutBuffer == NULL ? nullptr : gcnew D3DBuffer(tempoutBuffer);
}

D3DCounter^ D3DDevice::CreateCounter(CounterDescription description)
{
    ID3D10Counter* tempoutCounter = NULL;

    D3D10_COUNTER_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateCounter(&desc, &tempoutCounter));
    return tempoutCounter == NULL ? nullptr : gcnew D3DCounter(tempoutCounter);
}

DepthStencilState^ D3DDevice::CreateDepthStencilState(DepthStencilDescription description)
{
    ID3D10DepthStencilState* tempoutDepthStencilState = NULL;
    D3D10_DEPTH_STENCIL_DESC desc = {0};
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateDepthStencilState(
        &desc,
        &tempoutDepthStencilState));
    
    return tempoutDepthStencilState == NULL ? nullptr : gcnew DepthStencilState(tempoutDepthStencilState);
}

DepthStencilView^ D3DDevice::CreateDepthStencilView(D3DResource^ resource, DepthStencilViewDescription description)
{
    ID3D10DepthStencilView* tempoutDepthStencilView = NULL;
    D3D10_DEPTH_STENCIL_VIEW_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateDepthStencilView(
        resource->CastInterface<ID3D10Resource>(), 
        &desc, 
        &tempoutDepthStencilView));
    
    return tempoutDepthStencilView == NULL ? nullptr : gcnew DepthStencilView(tempoutDepthStencilView);
}

DepthStencilView^ D3DDevice::CreateDepthStencilView(D3DResource^ resource)
{
    ID3D10DepthStencilView* tempoutDepthStencilView = NULL;

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateDepthStencilView(
        resource->CastInterface<ID3D10Resource>(), 
        NULL, 
        &tempoutDepthStencilView));
    
    return tempoutDepthStencilView == NULL ? nullptr : gcnew DepthStencilView(tempoutDepthStencilView);
}

GeometryShader^ D3DDevice::CreateGeometryShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength)
{
    ID3D10GeometryShader* tempoutGeometryShader = NULL;
    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateGeometryShader(shaderBytecode.ToPointer(), static_cast<SIZE_T>(shaderBytecodeLength), &tempoutGeometryShader));
    return tempoutGeometryShader == NULL ? nullptr : gcnew GeometryShader(tempoutGeometryShader);
}

GeometryShader^ D3DDevice::CreateGeometryShader(Stream^ stream)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateGeometryShader(IntPtr(data), dataArray->Length);    
}

GeometryShader^ D3DDevice::CreateGeometryShaderWithStreamOutput(IntPtr shaderBytecode, UInt32 shaderBytecodeLength, IEnumerable<StreamOutputDeclarationEntry>^ streamOutputDeclarations, UInt32 outputStreamStride)
{
    ID3D10GeometryShader* tempoutGeometryShader = NULL;

    vector<D3D10_SO_DECLARATION_ENTRY> declEntries;
    marshal_context^ ctx = gcnew marshal_context();

    UINT declEntriesLen = Utilities::Convert::FillValStructsVector<StreamOutputDeclarationEntry,D3D10_SO_DECLARATION_ENTRY>(streamOutputDeclarations, declEntries, ctx);

    Validate::VerifyResult(
        CastInterface<ID3D10Device>()->CreateGeometryShaderWithStreamOutput(
        shaderBytecode.ToPointer(), 
        shaderBytecodeLength, 
        declEntriesLen == 0 ? NULL : &declEntries[0], 
        declEntriesLen, 
        static_cast<UINT>(outputStreamStride), 
        &tempoutGeometryShader));

    delete ctx;

    return tempoutGeometryShader == NULL ? nullptr : gcnew GeometryShader(tempoutGeometryShader);
}
GeometryShader^ D3DDevice::CreateGeometryShaderWithStreamOutput(Stream^ stream, IEnumerable<StreamOutputDeclarationEntry>^ streamOutputDeclarations, UInt32 outputStreamStride)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateGeometryShaderWithStreamOutput(IntPtr(data), dataArray->Length, streamOutputDeclarations, outputStreamStride);
}


InputLayout^ D3DDevice::CreateInputLayout(
    IEnumerable<InputElementDescription>^ inputElementDescriptions, 
    IntPtr shaderBytecodeWithInputSignature, 
    UInt32 shaderBytecodeWithInputSignatureSize)
{
    if (inputElementDescriptions == nullptr)
    {
        throw gcnew ArgumentNullException("inputElementDescriptions");
    }

    vector<D3D10_INPUT_ELEMENT_DESC> inputElements;
    marshal_context^ ctx = gcnew marshal_context();
    UINT count = Utilities::Convert::FillValStructsVector<InputElementDescription,D3D10_INPUT_ELEMENT_DESC>(inputElementDescriptions,inputElements, ctx);

    if (count == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "inputElementDescriptions");
    }

    ID3D10InputLayout* tempoutInputLayout = NULL;

    Validate::VerifyResult(
        CastInterface<ID3D10Device>()->CreateInputLayout(
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

PixelShader^ D3DDevice::CreatePixelShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength)
{
    ID3D10PixelShader* tempoutPixelShader = NULL;
    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreatePixelShader(shaderBytecode.ToPointer(), static_cast<SIZE_T>(shaderBytecodeLength), &tempoutPixelShader));
    
    return tempoutPixelShader == NULL ? nullptr : gcnew PixelShader(tempoutPixelShader);
}

PixelShader^ D3DDevice::CreatePixelShader(Stream^ stream)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];

    return CreatePixelShader(IntPtr(data), dataArray->Length);
}

D3DPredicate^ D3DDevice::CreatePredicate(QueryDescription description)
{
    ID3D10Predicate* tempoutPredicate = NULL;
    D3D10_QUERY_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreatePredicate(&desc, &tempoutPredicate));
    
    return tempoutPredicate == NULL ? nullptr : gcnew D3DPredicate(tempoutPredicate);
}

D3DQuery^ D3DDevice::CreateQuery(QueryDescription description)
{
    ID3D10Query* tempoutQuery = NULL;
    D3D10_QUERY_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateQuery(&desc, &tempoutQuery));
    return tempoutQuery == NULL ? nullptr : gcnew D3DQuery(tempoutQuery);
}

RasterizerState^ D3DDevice::CreateRasterizerState(RasterizerDescription description)
{
    D3D10_RASTERIZER_DESC desc;
    description.CopyTo(&desc);

    ID3D10RasterizerState* tempoutRasterizerState = NULL;
    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateRasterizerState(
        &desc, 
        &tempoutRasterizerState));
    return tempoutRasterizerState == NULL ? nullptr : gcnew RasterizerState(tempoutRasterizerState);
}

RenderTargetView^ D3DDevice::CreateRenderTargetView(D3DResource^ resource, RenderTargetViewDescription description)
{
    ID3D10RenderTargetView* tempoutRTView = NULL;
    pin_ptr<RenderTargetViewDescription> ptr = &description;

    Validate::VerifyResult(
        CastInterface<ID3D10Device>()->CreateRenderTargetView(
        resource->CastInterface<ID3D10Resource>(), 
        (D3D10_RENDER_TARGET_VIEW_DESC*)ptr, 
        &tempoutRTView));

    return tempoutRTView == NULL ? nullptr : gcnew RenderTargetView(tempoutRTView);
}

RenderTargetView^ D3DDevice::CreateRenderTargetView(D3DResource^ resource)
{
    ID3D10RenderTargetView* tempoutRTView = NULL;

    Validate::VerifyResult(
        CastInterface<ID3D10Device>()->CreateRenderTargetView(
        resource->CastInterface<ID3D10Resource>(), 
        NULL, 
        &tempoutRTView));

    return tempoutRTView == NULL ? nullptr : gcnew RenderTargetView(tempoutRTView);
}

SamplerState^ D3DDevice::CreateSamplerState(SamplerDescription description)
{

    ID3D10SamplerState* tempoutSamplerState = NULL;
    D3D10_SAMPLER_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateSamplerState(&desc, &tempoutSamplerState));
    return tempoutSamplerState == NULL ? nullptr : gcnew SamplerState(tempoutSamplerState);
}

ShaderResourceView^ D3DDevice::CreateShaderResourceView(D3DResource^ resource, ShaderResourceViewDescription description)
{
    Validate::CheckNull(resource, "resource");

    ID3D10ShaderResourceView* tempoutSRView = NULL;
    D3D10_SHADER_RESOURCE_VIEW_DESC desc;
    description.CopyTo(&desc);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateShaderResourceView(
        resource->CastInterface<ID3D10Resource>(), 
        &desc, 
        &tempoutSRView));

    return tempoutSRView == NULL ? nullptr : gcnew ShaderResourceView(tempoutSRView);
}

ShaderResourceView^ D3DDevice::CreateShaderResourceView(D3DResource^ resource)
{
    ID3D10ShaderResourceView* tempoutSRView = NULL;
    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateShaderResourceView(
        resource->CastInterface<ID3D10Resource>(), 
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

    ID3D10Texture1D *texture = NULL;
    D3D10_TEXTURE1D_DESC desc;
    vector<D3D10_SUBRESOURCE_DATA> subresourceVector;

    description.CopyTo(desc);
    subresourceVector.reserve(initialData->Length);
    Utilities::Convert::CopyToStructsVector((IEnumerable<SubresourceData>^)initialData, subresourceVector);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateTexture1D(
        &desc, &subresourceVector[0], &texture));
    
    return texture == NULL ? nullptr : gcnew Texture1D(texture);
}

Texture2D^ D3DDevice::CreateTexture2D(Texture2DDescription description, ... array<SubresourceData>^ initialData)
{
    if (initialData == nullptr || initialData->Length == 0)
    {
        return CreateTexture2D(description);
    }

    ID3D10Texture2D *texture = NULL;
    D3D10_TEXTURE2D_DESC desc;
    vector<D3D10_SUBRESOURCE_DATA> subresourceVector;

    description.CopyTo(desc);
    subresourceVector.reserve(initialData->Length);
    Utilities::Convert::CopyToStructsVector((IEnumerable<SubresourceData>^)initialData, subresourceVector);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateTexture2D(
        &desc, &subresourceVector[0], &texture));

    return texture == NULL ? nullptr : gcnew Texture2D(texture);
}

Texture3D^ D3DDevice::CreateTexture3D(Texture3DDescription description, ... array<SubresourceData>^ initialData)
{
    if (initialData == nullptr || initialData->Length == 0)
    {
        return CreateTexture3D(description);
    }

    ID3D10Texture3D *texture = NULL;
    D3D10_TEXTURE3D_DESC desc;
    vector<D3D10_SUBRESOURCE_DATA> subresourceVector;

    description.CopyTo(desc);
    subresourceVector.reserve(initialData->Length);
    Utilities::Convert::CopyToStructsVector((IEnumerable<SubresourceData>^)initialData, subresourceVector);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateTexture3D(
        &desc, &subresourceVector[0], &texture));

    return texture == NULL ? nullptr : gcnew Texture3D(texture);
}

Texture1D^ D3DDevice::CreateTexture1D(Texture1DDescription description)
{
    ID3D10Texture1D *texture = NULL;
    D3D10_TEXTURE1D_DESC desc;

    description.CopyTo(desc);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateTexture1D(
        &desc, NULL, &texture));
    
    return texture == NULL ? nullptr : gcnew Texture1D(texture);
}

Texture2D^ D3DDevice::CreateTexture2D(Texture2DDescription description)
{
    ID3D10Texture2D *texture = NULL;
    D3D10_TEXTURE2D_DESC desc;

    description.CopyTo(desc);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateTexture2D(
        &desc, NULL, &texture));

    return texture == NULL ? nullptr : gcnew Texture2D(texture);
}

Texture3D^ D3DDevice::CreateTexture3D(Texture3DDescription description)
{
    ID3D10Texture3D *texture = NULL;
    D3D10_TEXTURE3D_DESC desc;

    description.CopyTo(desc);

    Validate::VerifyResult(CastInterface<ID3D10Device>()->CreateTexture3D(
        &desc, NULL, &texture));

    return texture == NULL ? nullptr : gcnew Texture3D(texture);
}

VertexShader^ D3DDevice::CreateVertexShader(IntPtr shaderBytecode, UInt32 shaderBytecodeLength)
{
    ID3D10VertexShader* tempoutVertexShader = NULL;
    Validate::VerifyResult(
        CastInterface<ID3D10Device>()->CreateVertexShader(
            shaderBytecode.ToPointer(), 
            shaderBytecodeLength, 
            &tempoutVertexShader));
    
    return tempoutVertexShader == NULL ? nullptr : gcnew VertexShader(tempoutVertexShader);
}

VertexShader^ D3DDevice::CreateVertexShader(Stream^ stream)
{
    array<unsigned char>^ dataArray = CommonUtils::ReadStream(stream);
    pin_ptr<unsigned char> data = &dataArray[0];
    return CreateVertexShader(IntPtr(data), dataArray->Length);
}

void D3DDevice::Draw(UInt32 vertexCount, UInt32 startVertexLocation)
{
    CastInterface<ID3D10Device>()->Draw(static_cast<UINT>(vertexCount), static_cast<UINT>(startVertexLocation));
}

void D3DDevice::DrawAuto()
{
    CastInterface<ID3D10Device>()->DrawAuto();
}

void D3DDevice::DrawIndexed(UInt32 indexCount, UInt32 startIndexLocation, Int32 baseVertexLocation)
{
    CastInterface<ID3D10Device>()->DrawIndexed(static_cast<UINT>(indexCount), static_cast<UINT>(startIndexLocation), safe_cast<INT>(baseVertexLocation));
}

void D3DDevice::DrawIndexedInstanced(UInt32 indexCountPerInstance, UInt32 instanceCount, UInt32 startIndexLocation, Int32 baseVertexLocation, UInt32 startInstanceLocation)
{
    CastInterface<ID3D10Device>()->DrawIndexedInstanced(static_cast<UINT>(indexCountPerInstance), static_cast<UINT>(instanceCount), static_cast<UINT>(startIndexLocation), safe_cast<INT>(baseVertexLocation), static_cast<UINT>(startInstanceLocation));
}

void D3DDevice::DrawInstanced(UInt32 vertexCountPerInstance, UInt32 instanceCount, UInt32 startVertexLocation, UInt32 startInstanceLocation)
{
    CastInterface<ID3D10Device>()->DrawInstanced(static_cast<UINT>(vertexCountPerInstance), static_cast<UINT>(instanceCount), static_cast<UINT>(startVertexLocation), static_cast<UINT>(startInstanceLocation));
}

void D3DDevice::Flush()
{
    CastInterface<ID3D10Device>()->Flush();
}

void D3DDevice::GenerateMips(ShaderResourceView^ shaderResourceView)
{
    CastInterface<ID3D10Device>()->GenerateMips(shaderResourceView->CastInterface<ID3D10ShaderResourceView>());
}

D3DPredicate^ D3DDevice::Predication::get(void)
{
    ID3D10Predicate* predicate = NULL;
    BOOL value;

    CastInterface<ID3D10Device>()->GetPredication(&predicate, &value);

    return predicate == NULL ? nullptr :
        gcnew D3DPredicate(predicate, value != FALSE);
}

void D3DDevice::Predication::set(D3DPredicate^ predicate)
{
    CastInterface<ID3D10Device>()->SetPredication(
        predicate->CastInterface<ID3D10Predicate>(), predicate->WhenTrue ? TRUE : FALSE);
}

void D3DDevice::SetPredication(D3DPredicate^ predicate, Boolean predicateValue)
{
    predicate->WhenTrue = predicateValue;

    CastInterface<ID3D10Device>()->SetPredication(
        predicate->CastInterface<ID3D10Predicate>(), predicateValue ? TRUE : FALSE);
}

generic <typename T> where T : DirectUnknown
T D3DDevice::OpenSharedResource(IntPtr resource)
{
    void* tempoutResource = NULL;

    GUID guid = CommonUtils::GetGuid(T::typeid);
    
    Validate::VerifyResult(CastInterface<ID3D10Device>()->OpenSharedResource(static_cast<HANDLE>(resource.ToPointer()), guid, &tempoutResource));
    
    return Utilities::Convert::CreateIUnknownWrapper<T>(static_cast<IUnknown*>(tempoutResource));
}

void D3DDevice::ResolveSubresource(D3DResource^ destinationResource, UInt32 destinationSubresource, D3DResource^ sourceResource, UInt32 sourceSubresource, Format format)
{
    CastInterface<ID3D10Device>()->ResolveSubresource(destinationResource->CastInterface<ID3D10Resource>(), static_cast<UINT>(destinationSubresource), sourceResource->CastInterface<ID3D10Resource>(), static_cast<UINT>(sourceSubresource), static_cast<DXGI_FORMAT>(format));
}

void D3DDevice::UpdateSubresource(D3DResource^ destinationResource, UInt32 destinationSubresource, IntPtr sourceData, UInt32 sourceRowPitch, UInt32 sourceDepthPitch, Box destinationBox)
{
    pin_ptr<Box> boxPtr = &destinationBox;;
    CastInterface<ID3D10Device>()->UpdateSubresource(destinationResource->CastInterface<ID3D10Resource>(), static_cast<UINT>(destinationSubresource), (D3D10_BOX*)boxPtr, reinterpret_cast<LPVOID>(sourceData.ToPointer()), static_cast<UINT>(sourceRowPitch), static_cast<UINT>(sourceDepthPitch));
}

void D3DDevice::UpdateSubresource(D3DResource^ destinationResource, UInt32 destinationSubresource, IntPtr sourceData, UInt32 sourceRowPitch, UInt32 sourceDepthPitch)
{
    CastInterface<ID3D10Device>()->UpdateSubresource(destinationResource->CastInterface<ID3D10Resource>(), static_cast<UINT>(destinationSubresource), NULL, reinterpret_cast<LPVOID>(sourceData.ToPointer()), static_cast<UINT>(sourceRowPitch), static_cast<UINT>(sourceDepthPitch));
}

CreateDeviceOptions D3DDevice::CreateDeviceOptions::get()
{
    UINT returnValue = CastInterface<ID3D10Device>()->GetCreationFlags();
    return safe_cast<Direct3D10::CreateDeviceOptions>(returnValue);
}

ErrorCode D3DDevice::DeviceRemovedReason::get()
{
    return safe_cast<ErrorCode>(CastInterface<ID3D10Device>()->GetDeviceRemovedReason());
}

ExceptionErrors D3DDevice::ExceptionMode::get()
{
    UINT returnValue = CastInterface<ID3D10Device>()->GetExceptionMode();
    return safe_cast<ExceptionErrors>(returnValue);
}

void D3DDevice::ExceptionMode::set(ExceptionErrors value)
{
    Validate::VerifyResult(CastInterface<ID3D10Device>()->SetExceptionMode(static_cast<UINT>(value)));
}

InfoQueue ^ D3DDevice::InfoQueue::get(void)
{
    ID3D10InfoQueue * infoQueue = NULL;

    Validate::VerifyResult(CastInterface<ID3D10Device>()->QueryInterface(__uuidof(ID3D10InfoQueue), (void**) &infoQueue));

    return infoQueue == NULL ? nullptr : gcnew Direct3D10::InfoQueue(infoQueue);
}

// Gets a switch-to-reference object that enables an application to switch between a hardware and software device.
// Can only be obtained if the device was created using CreateDeviceOptions.SwitchToRef flag
// If it was not - it will throw an InvalidCastException
SwitchToRef ^ D3DDevice::SwitchToRef::get(void)
{
    ID3D10SwitchToRef * switchToRef = NULL;

    Validate::VerifyResult(CastInterface<ID3D10Device>()->QueryInterface(__uuidof(ID3D10SwitchToRef), (void**) &switchToRef));

    return switchToRef == NULL ? nullptr : gcnew Direct3D10::SwitchToRef(switchToRef);
}

// The pipeline stage accessors

GeometryShaderPipelineStage^ D3DDevice::GS::get() 
{ 
    if (m_GS == nullptr) 
        m_GS = gcnew GeometryShaderPipelineStage(this); 
    
    return m_GS; 
}


InputAssemblerPipelineStage^ D3DDevice::IA::get() 
{ 
    if (m_IA == nullptr) 
        m_IA = gcnew InputAssemblerPipelineStage(this); 
    
    return m_IA; 
}

OutputMergerPipelineStage^ D3DDevice::OM::get() 
{ 
    if (m_OM == nullptr) 
        m_OM = gcnew OutputMergerPipelineStage(this); 
    
    return m_OM; 
}

PixelShaderPipelineStage^ D3DDevice::PS::get() 
{ 
    if (m_PS == nullptr) 
        m_PS = gcnew PixelShaderPipelineStage(this); 
    
    return m_PS; 
}

RasterizerPipelineStage^ D3DDevice::RS::get() 
{ 
    if (m_RS == nullptr) 
        m_RS = gcnew RasterizerPipelineStage(this); 
    
    return m_RS; 
}

StreamOutputPipelineStage^ D3DDevice::SO::get() 
{ 
    if (m_SO == nullptr) 
        m_SO = gcnew StreamOutputPipelineStage(this); 
    
    return m_SO; 
}

VertexShaderPipelineStage^ D3DDevice::VS::get() 
{ 
    if (m_VS == nullptr) 
        m_VS = gcnew VertexShaderPipelineStage(this); 
    
    return m_VS; 
}

Effect^ D3DDevice::CreateEffectFromCompiledBinary( String^ path )
{
    FileStream^ stream = File::Open( path, FileMode::Open );
    return CreateEffectFromCompiledBinary( gcnew BinaryReader( stream ), 0, nullptr );
}

Effect^ D3DDevice::CreateEffectFromCompiledBinary( BinaryReader^ binaryEffect )
{
    return CreateEffectFromCompiledBinary( binaryEffect, 0, nullptr );
}

Effect^ D3DDevice::CreateEffectFromCompiledBinary( Stream^ inputStream )
{
    return CreateEffectFromCompiledBinary( gcnew BinaryReader( inputStream ), 0, nullptr );
}

Effect^ D3DDevice::CreateEffectFromCompiledBinary( Stream^ inputStream, int effectCompileOptions, EffectPool^ effectPool )
{
    return CreateEffectFromCompiledBinary( gcnew BinaryReader( inputStream ), effectCompileOptions, effectPool );
}


Effect^ D3DDevice::CreateEffectFromCompiledBinary( BinaryReader^ binaryEffect, int effectCompileOptions, EffectPool^ effectPool )
{
    array<unsigned char>^ data = binaryEffect->ReadBytes((int)binaryEffect->BaseStream->Length);
    pin_ptr<unsigned char> pData = &data[0];

    ID3D10Effect* pEffect = NULL;
    Validate::VerifyResult(
        CreateEffectFromMemory( 
            pData,
            data->Length,
            effectCompileOptions,
            this->CastInterface<ID3D10Device>(),
            effectPool ? effectPool->CastInterface<ID3D10EffectPool>() : NULL,
            &pEffect));

    return pEffect ? gcnew Effect( pEffect ) : nullptr;
}

Device^ D3DDevice::GraphicsDevice::get()
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

Direct3D10::Multithread^ D3DDevice::Multithread::get(void)
{
	ID3D10Multithread *debugInterface;

	Validate::VerifyResult(CastInterface<IUnknown>()->QueryInterface(__uuidof(ID3D10Multithread), (void **)&debugInterface));

	return debugInterface == NULL ? nullptr : gcnew Direct3D10::Multithread(debugInterface);
}

D3DDebug^ D3DDevice::Debug::get(void)
{
	ID3D10Debug *debugInterface;

	Validate::VerifyResult(CastInterface<IUnknown>()->QueryInterface(__uuidof(ID3D10Debug), (void **)&debugInterface));

	return debugInterface == NULL ? nullptr : gcnew D3DDebug(debugInterface);
}

StateBlock^ D3DDevice::CreateStateBlock(StateBlockMask^ stateBlockMask)
{
    D3D10_STATE_BLOCK_MASK nativeMask;

    stateBlockMask->CopyTo(nativeMask);

    D3D10CreateStateBlockFuncPtr createFuncPtr = 
        (D3D10CreateStateBlockFuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            D3D10Library, "D3D10CreateStateBlock");

    ID3D10StateBlock *nativeStateBlock;

    Validate::VerifyResult((*createFuncPtr)(CastInterface<ID3D10Device>(), &nativeMask, &nativeStateBlock));

    return gcnew StateBlock(nativeStateBlock);
}
