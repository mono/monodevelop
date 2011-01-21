//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DXGIObject.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {
ref class D3DDevice;
}}}}

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {
ref class D3DDevice;
ref class D3DDevice1;
}}}}
namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {

ref class Device;
using namespace System;

    /// <summary>
    /// Inherited from objects that are tied to the device so that they can retrieve it.
    /// <para>(Also see DirectX SDK: IDXGIDeviceSubObject)</para>
    /// </summary>
    public ref class DeviceSubObject :
        public GraphicsObject
    {
    public: 

        /// <summary>
        /// Retrieves the device as Direct3D 10 device.
        /// <para>(Also see DirectX SDK: IDXGIDeviceSubObject::GetDevice)</para>
        /// </summary>
        /// <returns>A Direct3D 10 Device.</returns>
        property Direct3D10::D3DDevice^ AsDirect3D10Device
        {
            Direct3D10::D3DDevice^ get(void);
        }

        /// <summary>
        /// Retrieves the device as Direct3D 10.1 device.
        /// <para>(Also see DirectX SDK: IDXGIDeviceSubObject::GetDevice)</para>
        /// </summary>
        /// <returns>A Direct3D 10.1 Device.</returns>
        property Direct3D10::D3DDevice1^ AsDirect3D10Device1
        {
            Direct3D10::D3DDevice1^ get(void);
        }

        /// <summary>
        /// Retrieves the device as Direct3D 11 device.
        /// <para>(Also see DirectX SDK: IDXGIDeviceSubObject::GetDevice)</para>
        /// </summary>
        /// <returns>A Direct3D 11 Device.</returns>
        property Direct3D11::D3DDevice^ AsDirect3D11Device
        {
            Direct3D11::D3DDevice^ get (void);
        }

        /// <summary>
        /// Retrieves the device as Graphics device.
        /// <para>(Also see DirectX SDK: IDXGIDeviceSubObject::GetDevice)</para>
        /// </summary>
        property Device^ AsGraphicsDevice
        {
            Device^ get(void);
        }

    internal:

        DeviceSubObject(void)
        { }

		DeviceSubObject(IDXGIDeviceSubObject* pNativeIDXGIDeviceSubObject) : GraphicsObject(pNativeIDXGIDeviceSubObject)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(IDXGIDeviceSubObject)); }
        }
    };
} } } }
