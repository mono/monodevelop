//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10DeviceChild.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
ref class D3DResource;

    /// <summary>
    /// A view interface specifies the parts of a resource the pipeline can access during rendering.
    /// <para>(Also see DirectX SDK: ID3D10View)</para>
    /// </summary>
    public ref class View :
        public DeviceChild
    {
    public: 
        /// <summary>
        /// Get the resource that is accessed through this view.
        /// <para>(Also see DirectX SDK: ID3D10View::GetResource)</para>
        /// </summary>
        property D3DResource^ Resource
        {
            D3DResource^ get(void);
        }

    internal:
        View()
        { }

        View(ID3D10View* pNativeID3D10View) : DeviceChild(pNativeID3D10View)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10View)); }
        }
    };
} } } }
