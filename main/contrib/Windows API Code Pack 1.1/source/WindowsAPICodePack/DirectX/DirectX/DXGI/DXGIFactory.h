//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DXGIObject.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {
ref class D3DDevice;
}}}}

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {
ref class D3DDevice;
}}}}

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;

ref class Adapter;
ref class SwapChain;

    // REVIEW: IDXGIAdapter1 inherits IDXGIAdapter. .NET 4.0 supports variance for interfaces.
    // If we require .NET 4.0, it would be possible to have a single Adapters property for both
    // Factory and Factory1, where Factory1 populates the same underlying data but with Adapter1
    // instances instead of Adapter instances.
    //
    // For now, with support for .NET 3.5 a given, having Factory1::Adapters hide Factory::Adapters
    // is probably fine.

    /// <summary>
    /// Implements methods for generating Graphics objects (which handle fullscreen transitions).
    /// <para>(Also see DirectX SDK: IDXGIFactory)</para>
    /// </summary>
    public ref class Factory :
        public GraphicsObject
    {
    public: 

        /// <summary>
        /// Creates a swap chain.
        /// <para>(Also see DirectX SDK: IDXGIFactory::CreateSwapChain)</para>
        /// </summary>
        /// <param name="device">The device that will write 2D images to the swap chain.</param>
        /// <param name="description">The swap-chain description (see <see cref="SwapChainDescription"/>)<seealso cref="SwapChainDescription"/>. This parameter cannot be NULL.</param>
        /// <return>The swap chain created (see <see cref="SwapChain"/>)<seealso cref="SwapChain"/>.</return>
        SwapChain^ CreateSwapChain(Microsoft::WindowsAPICodePack::DirectX::Direct3D10::D3DDevice^ device, SwapChainDescription description);

        /// <summary>
        /// Creates a swap chain.
        /// <para>(Also see DirectX SDK: IDXGIFactory::CreateSwapChain)</para>
        /// </summary>
        /// <param name="device">The device that will write 2D images to the swap chain.</param>
        /// <param name="description">The swap-chain description (see <see cref="SwapChainDescription"/>)<seealso cref="SwapChainDescription"/>. This parameter cannot be NULL.</param>
        /// <return>The swap chain created (see <see cref="SwapChain"/>)<seealso cref="SwapChain"/>.</return>
        SwapChain^ CreateSwapChain(Microsoft::WindowsAPICodePack::DirectX::Direct3D11::D3DDevice^ device, SwapChainDescription description);

        /// <summary>
        /// Enumerates the adapters (video cards).
        /// <para>(Also see DirectX SDK: IDXGIFactory::EnumAdapters)</para>
        /// </summary>
        /// <remarks>
        /// Note that if the set of installed adapters changes, the adapter collection
        /// returned by this factory instance is stale. To get the current collection
        /// of adapters, a new factory instance must be created.
        /// </remarks>
        property ReadOnlyCollection<Adapter^>^ Adapters
        {
            ReadOnlyCollection<Adapter^>^ get(void);
        }

        /// <summary>
        /// Get the window through which the user controls the transition to and from fullscreen.
        /// <para>(Also see DirectX SDK: IDXGIFactory::GetWindowAssociation)</para>
        /// </summary>
        /// <return>A window handle.</return>
        property IntPtr WindowAssociation
        {
            IntPtr get(void);
        }

        /// <summary>
        /// Allows Graphics to monitor an application's message queue for the alt-enter key sequence (which causes the application to switch from windowed to fullscreen or vice versa).
        /// <para>(Also see DirectX SDK: IDXGIFactory::MakeWindowAssociation)</para>
        /// </summary>
        /// <param name="windowHandle">The handle of the window that is to be monitored. This parameter can be NULL; but only if the flags are also 0.</param>
        /// <param name="options">One or more of the MakeWindowAssociation flags.</param>
        void MakeWindowAssociation(IntPtr windowHandle, MakeWindowAssociationOptions options);

        /// <summary>
        /// Creates a Graphics 1.0 factory that generates objects used to enumerate and specify video graphics settings.
        /// <para>(Also see DirectX SDK: CreateDXGIFactory() Function.)</para>
        /// </summary>
        /// <returns>Factory Object.</returns>
        static Factory^ Create();
    
    internal:

        Factory(void)
        { }
    
        Factory(IDXGIFactory* pNativeIDXGIFactory) : GraphicsObject(pNativeIDXGIFactory)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(IDXGIFactory)); }
        }

    private:

        ReadOnlyCollection<Adapter^>^ adapters;
    };
} } } }
