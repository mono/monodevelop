//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DXGIDeviceSubObject.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {

using namespace System;

    /// <summary>
    /// An Resource interface allows resource sharing and identifies the memory that a resource resides in.
    /// <para>(Also see DirectX SDK: IDXGIResource)</para>
    /// </summary>
    public ref class Resource :
        public DeviceSubObject
    {
    public: 
        /// <summary>
        /// The priority for evicting the resource from memory
        /// <para>(Also see DirectX SDK: IDXGIResource::GetEvictionPriority)</para>
        /// </summary>
        property ResourcePriority EvictionPriority
        {
            ResourcePriority get();
            void set(ResourcePriority value);
        }

        /// <summary>
        /// Get the handle to a shared resource. 
        /// The returned handle can be used to open the resource using different Direct3D devices.
        /// <para>(Also see DirectX SDK: IDXGIResource::GetSharedHandle)</para>
        /// </summary>
        property IntPtr SharedHandle        
        {
            IntPtr get();
        }

        /// <summary>
        /// Get the expected resource usage.
        /// <para>(Also see DirectX SDK: IDXGIResource::GetUsage)</para>
        /// </summary>
        /// <param name="outUsage">A usage flag (see <see cref="UsageOptions"/>)<seealso cref="UsageOptions"/>. For Direct3D 10, a surface can be used as a shader input or a render-target output.</param>
        property UsageOptions UsageOptions
        {
            Graphics::UsageOptions get();
        }

    internal:

        Resource(void)
        { }

        // REVIEW: unused for now. A Resource instance is created by calling D3DDevice::OpenSharedResource
        // which in turn calls CreateIUnknownWrapper(), which in turn calls the default constructor.
        // A better implementation of CreateIUnknownWrapper() would use this constructor instead.

        CA_SUPPRESS_MESSAGE("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")
        Resource(IDXGIResource* pNativeIDXGIResource)
            : DeviceSubObject(pNativeIDXGIResource)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(IDXGIResource)); }
        }
    };
} } } }
