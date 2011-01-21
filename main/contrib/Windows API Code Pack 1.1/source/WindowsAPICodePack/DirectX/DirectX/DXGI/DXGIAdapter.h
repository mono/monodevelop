//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DXGIObject.h"

using namespace System;
using namespace System::Collections::ObjectModel;
namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {
ref class Output;

    /// <summary>
    /// The Adapter interface represents a display sub-system (including one or more GPU's, DACs and video memory).
    /// <para>(Also see DirectX SDK: IDXGIAdapter)</para>
    /// </summary>
    public ref class Adapter :
        public GraphicsObject
    {
    public: 
        /// <summary>
        /// Checks to see if a device interface type for a graphics component is supported by the system.
        /// <para>(Also see DirectX SDK: IDXGIAdapter::CheckInterfaceSupport)</para>
        /// </summary>
        /// <param name="deviceType"> The device support checked.</param>
        /// <returns>
        /// An AdapterDriverVersion value containing the major and minor driver version numbers,
        /// if the device type is supported for the adapter. Otherwise, null.
        /// </returns>
        Nullable<AdapterDriverVersion> CheckDeviceSupport(DeviceType deviceType);

        /// <summary>
        /// Get a read-only collection of all adapter (video card) outputs available.
        /// <para>(Also see DirectX SDK: IDXGIAdapter::EnumOutputs)</para>
        /// </summary>
        property ReadOnlyCollection<Output^>^ Outputs
        {
            ReadOnlyCollection<Output^>^ get(void);
        }

        /// <summary>
        /// Get an adapter (video card) output.
        /// <para>(Also see DirectX SDK: IDXGIAdapter::EnumOutputs)</para>
        /// </summary>
        /// <param name="index">The index of the output requested.</param>
        /// <returns>An Output object</returns>
        Output^ GetOutput(UInt32 index);

        /// <summary>
        /// Gets a DXGI 1.0 description of an adapter (or video card).
        /// <para>(Also see DirectX SDK: IDXGIAdapter::GetDesc)</para>
        /// </summary>
        property AdapterDescription Description
        {
            AdapterDescription get();
        }

    internal:
		// REVIEW: why the empty constructor?
        Adapter()
        { }

        Adapter(IDXGIAdapter* pNativeIDXGIAdapter)
            : GraphicsObject(pNativeIDXGIAdapter)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(IDXGIAdapter)); }
        }

    private:

        Nullable<Graphics::AdapterDescription> description;
    };

} } } }
