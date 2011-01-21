//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10Device.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

ref class BlendState1;
ref class D3DResource;
ref class ShaderResourceView1;

    /// <summary>
    /// The device interface represents a virtual adapter for Direct3D 10.1; it is used to perform rendering and create Direct3D resources.
    /// <para>To create a D3DDevice1 instance, use one of the static factory method overloads: CreateDevice1() or CreateDeviceAndSwapChain1().</para>
    /// <para>(Also see DirectX SDK: ID3D10Device1)</para>
    /// </summary>
    public ref class D3DDevice1 :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D10::D3DDevice
    {
    public: 
        /// <summary>
        /// Create a blend-state object that encapsules blend state for the output-merger stage.
        /// <para>(Also see DirectX SDK: ID3D10Device1::CreateBlendState1)</para>
        /// </summary>
        /// <param name="blendStateDescription">A blend-state description (see <see cref="BlendDescription1"/>)<seealso cref="BlendDescription1"/>.</param>
        /// <returns>A blend-state object created (see BlendState1 Object).</returns>
        BlendState1^ CreateBlendState1(BlendDescription1 blendStateDescription);

        /// <summary>
        /// Create a shader-resource view for accessing data in a resource.
        /// <para>(Also see DirectX SDK: ID3D10Device1::CreateShaderResourceView1)</para>
        /// </summary>
        /// <param name="resource">Pointer to the resource that will serve as input to a shader. This resource must have been created with the ShaderResource flag.</param>
        /// <param name="description">A shader-resource-view description (see <see cref="ShaderResourceViewDescription1"/>)<seealso cref="ShaderResourceViewDescription1"/>. Set this parameter to NULL to create a view that accesses the entire resource (using the format the resource was created with).</param>
        /// <returns>A shader-resource view (see ShaderResourceView1)</returns>
        ShaderResourceView1^ CreateShaderResourceView1(D3DResource^ resource, ShaderResourceViewDescription1 description);

        /// <summary>
        /// Gets the feature level of the hardware device.
        /// </summary>
        property FeatureLevel DeviceFeatureLevel
        {
            FeatureLevel get();
        };

        /// <summary>
        /// Create a Direct3D 10.1 device and a swap chain using the default hardware adapter 
        /// and the most common settings.
        /// <para>(Also see DirectX SDK: D3D10CreateDeviceAndSwapChain1() function)</para>
        /// </summary>
        /// <param name="windowHandle">The window handle to the output window.</param>
        /// <returns>The created Direct3D 10.1 Device</returns>
        /// <remarks>
        /// The swap chain created can be retrieved from the <see cref="SwapChain" /> property.
        /// If DirectX SDK environment variable is found, and the build is a Debug one,
        /// this method will attempt to use <see cref="CreateDeviceOptions"/>.Debug flag. This should allow 
        /// using the InfoQueue object.
        /// </remarks>
        static D3DDevice1^ CreateDeviceAndSwapChain1(IntPtr windowHandle);

        /// <summary>
        /// Create a Direct3D 10.1 device and a swap chain.
        /// <para>(Also see DirectX SDK: D3D10CreateDeviceAndSwapChain1() function)</para>
        /// </summary>
        /// <param name="adapter">An Adapter object. Can be null.</param>
        /// <param name="driverType">The type of driver for the device.</param>
        /// <param name="softwareRasterizerLibrary">A path the DLL that implements a software rasterizer. Must be NULL if driverType is non-software.</param>
        /// <param name="options">Device creation flags that enable API layers. These flags can be bitwise OR'd together.</param>
        /// <param name="hardwareLevel">The feature level supported by this device.</param>
        /// <param name="swapChainDescription">Description of the swap chain.</param>
        /// <returns>The created Direct3D 10.1 Device</returns>
        /// <remarks>
        /// The swap chain created can be retrieved from the <see cref="SwapChain" /> property.
        /// By default, all Direct3D 10 calls are handled in a thread-safe way. 
        /// By creating a single-threaded device (using <see cref="CreateDeviceOptions"/>.SingleThreaded), 
        /// you disable thread-safe calling.
        /// </remarks>
        static D3DDevice1^ CreateDeviceAndSwapChain1(
            Adapter^ adapter, 
            DriverType driverType,
            String^ softwareRasterizerLibrary,
            Direct3D10::CreateDeviceOptions options,
            FeatureLevel hardwareLevel,
            SwapChainDescription swapChainDescription);

        /// <summary>
        /// Create a Direct3D 10.1 device using the default hardware adapter and the most common settings.
        /// <para>(Also see DirectX SDK: D3D10CreateDevice1() function)</para>
        /// </summary>
        /// <returns>The created Direct3D 10.1 Device</returns>
        /// <remarks>
        /// If DirectX SDK environment variable is found, and the build is a Debug one,
        /// this method will attempt to use <see cref="CreateDeviceOptions"/>.Debug flag. This should allow 
        /// using the InfoQueue object.
        /// </remarks>
        static D3DDevice1^ CreateDevice1();

        /// <summary>
        /// Create a Direct3D 10.1 device. 
        /// <para>(Also see DirectX SDK: D3D10CreateDevice1() function)</para>
        /// </summary>
        /// <param name="adapter">An Adapter object. Can be null.</param>
        /// <param name="driverType">The type of driver for the device.</param>
        /// <param name="options">Device creation flags that enable API layers. These flags can be bitwise OR'd together.</param>
        /// <param name="softwareRasterizerLibrary">A path the DLL that implements a software rasterizer. Must be NULL if driverType is non-software.</param>
        /// <param name="hardwareLevel">The highest hardware feature level supported by this device.</param>
        /// <returns>The created Direct3D 10.1 Device</returns>
        /// <remarks>By default, all Direct3D 10 calls are handled in a thread-safe way. 
        /// By creating a single-threaded device (using <see cref="CreateDeviceOptions"/>.SingleThreaded), 
        /// you disable thread-safe calling.
        /// </remarks>
        static D3DDevice1^ CreateDevice1(
            Adapter^ adapter, 
            DriverType driverType,
            String^ softwareRasterizerLibrary,
            Direct3D10::CreateDeviceOptions options,
            FeatureLevel hardwareLevel);

        /// <summary>
        /// Loads a precompiled effect from a binary data stream.
        /// <para>(Also see DirectX SDK: D3D10CreateEffectFromMemory() function)</para>
        /// </summary>
        /// <param name="binaryEffect">The binary data stream.</param>
        /// <param name="effectCompileOptions">Effect compile options</param>
        /// <param name="effectPool">A memory space for effect variables shared across effects.</param>
        virtual Effect^ CreateEffectFromCompiledBinary( BinaryReader^ binaryEffect, int effectCompileOptions, EffectPool^ effectPool ) override;

    internal:
        D3DDevice1() : D3DDevice()
        { }

        D3DDevice1(ID3D10Device1* pNativeID3D10Device1) : D3DDevice(pNativeID3D10Device1)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10Device1)); }
        }

    private:

        static HRESULT TryCreateDeviceAndSwapChain1(
            Adapter^ adapter, 
            DriverType driverType,
            String^ softwareRasterizerLibrary,
            Direct3D10::CreateDeviceOptions options,
            FeatureLevel hardwareLevel,
            SwapChainDescription swapChainDescription,
            D3DDevice1^ %device);

    };
} } } }
