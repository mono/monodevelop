//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "D3D11DeviceChild.h"
#include "DXGI/DXGISurface.h"
#include "DXGI/DXGISurface1.h"

using namespace System;

// REVIEW: It's not clear to me why this file doesn't just include the headers for
// Surface and Surface1.

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {
    ref class Surface;
    ref class Surface1;
} } } }

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

    /// <summary>
    /// A resource interface provides common actions on all resources.
    /// <para>(Also see DirectX SDK: ID3D11Resource)</para>
    /// </summary>
    public ref class D3DResource :
        public DeviceChild
    {
    public: 
        /// <summary>
        /// Gets or sets the eviction priority of a resource.
        /// <para>(Also see DirectX SDK: ID3D11Resource::GetEvictionPriority, ID3D11Resource::SetEvictionPriority)</para>
        /// </summary>
        property UInt32 EvictionPriority
        {
            UInt32 get();
            void set(UInt32 value);
        }

        /// <summary>
        /// Get the type of the resource.
        /// <para>(Also see DirectX SDK: ID3D11Resource::GetType)</para>
        /// </summary>
        property ResourceDimension ResourceDimension
        {
            Direct3D11::ResourceDimension get();
        }

        /// <summary>
        /// Get associated Graphics surface.
        /// </summary>
        property Surface^ GraphicsSurface
        {
            Surface^ get(void);
        }

        /// <summary>
        /// Get associated Graphics.Surface1.
        /// </summary>
        property Surface1^ GraphicsSurface1
        {
            Surface1^ get(void);
        }

    internal:

        D3DResource(void)
        { }

		D3DResource(ID3D11Resource* pNativeID3D11Resource) : DeviceChild(pNativeID3D11Resource)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11Resource)); }
        }

    };

} } } }