//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    // NOTE: While the MSDN documentation claims ID3D11SwitchToRef inherits ID3D11DeviceChild,
    // that is not what the DirectX headers say. Headers trump docs.

    /// <summary>
    /// A switch-to-reference interface enables an application to switch between a hardware and software device.
    /// <para>(Also see DirectX SDK: ID3D11SwitchToRef)</para>
    /// </summary>
    public ref class SwitchToRef :
        DirectUnknown
    {
    public: 
        /// <summary>
        /// Get or sets a boolean value that indicates the type of device being used.
        /// Set this property to true to change to a software device or to false to change to a hardware device.
        /// <para>(Also see DirectX SDK: ID3D10SwitchToRef::GetUseRef, ID3D10SwitchToRef::SetUseRef)</para>
        /// </summary>
        property Boolean UseRef
        {
            Boolean get(void);
            void set(Boolean);
        }

    internal:

        SwitchToRef(void)
        { }

		SwitchToRef(ID3D11SwitchToRef* pNativeID3D11SwitchToRef) : DirectUnknown(pNativeID3D11SwitchToRef)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11SwitchToRef)); }
        }
    };
} } } }
