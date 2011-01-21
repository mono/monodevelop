//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DXGIObject.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {

using namespace System;

ref class Surface;
ref class Adapter;
ref class Resource;

    /// <summary>
    /// Implements a derived class for Graphics objects that produce image data.
    /// <para>(Also see DirectX SDK: IDXGIDevice)</para>
    /// </summary>
    public ref class Device :
        public GraphicsObject
    {
    public: 

        /// <summary>
        /// Returns the adapter for the specified device.
        /// <para>(Also see DirectX SDK: IDXGIDevice::GetAdapter)</para>
        /// </summary>
        property Adapter^ Adapter
        {
            Graphics::Adapter^ get(void);
        }

        /// <summary>
        /// Sets or Gets the GPU thread priority.
        /// <para>(Also see DirectX SDK: IDXGIDevice::GetGPUThreadPriority and IDXGIDevice::SetGPUThreadPriority)</para>
        /// </summary>
        property Int32 GpuThreadPriority
        {
            Int32 get();
            void set(Int32 value);
        }

        /// <summary>
        /// Gets the residency status of a colleciton of resources.
        /// <para>Note: This method should not be called every frame as it incurs a non-trivial amount of overhead.</para>
        /// <para>(Also see DirectX SDK: IDXGIDevice::QueryResourceResidency)</para>
        /// </summary>
        /// <param name="resources">A collection or array of Resource interfaces.</param>
        /// <returns>An array of residency flags. Each element describes the residency status for corresponding element in
        /// the resources argument.</returns>
        array<Residency>^ QueryResourceResidency(IEnumerable<Resource^>^ resources);

    internal:

        Device(void)
        { }

		Device(IDXGIDevice* pNativeIDXGIDevice) : GraphicsObject(pNativeIDXGIDevice)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(IDXGIDevice)); }
        }
    };
} } } }
