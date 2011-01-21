//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;
using namespace Microsoft::WindowsAPICodePack::DirectX;

    /// <summary>
    /// A swith-to-reference interface (see the switch-to-reference layer) enables an application to switch between a hardware and software device.
    /// <para>(Also see DirectX SDK: ID3D10SwitchToRef)</para>
    /// </summary>
    public ref class SwitchToRef :
        public DirectUnknown
    {
    public: 
        /// <summary>
        /// Get or set a boolean value that indicates the type of device being used.
        /// Set this to True to change to a software device, set this to False to change to a hardware device.
        /// <para>(Also see DirectX SDK: ID3D10SwitchToRef::GetUseRef, ID3D10SwitchToRef::SetUseRef)</para>
        /// </summary>
        property Boolean UseRef
        {
            Boolean get();
            void set(Boolean value);
        }

    internal:
        SwitchToRef()
        { }

        SwitchToRef(ID3D10SwitchToRef* pNativeID3D10SwitchToRef) :
            DirectUnknown(pNativeID3D10SwitchToRef)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10SwitchToRef)); }
        }
    };
} } } }
