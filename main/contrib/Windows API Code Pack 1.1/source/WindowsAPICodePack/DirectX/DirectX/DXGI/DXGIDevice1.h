//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DXGIDevice.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {

using namespace System;

    /// <summary>
    /// Implements a derived class for Graphics 1.1 objects that produce image data.
    /// <para>(Also see DirectX SDK: IDXGIDevice1)</para>
    /// </summary>
    public ref class Device1 :
        public Device
    {
    public: 
        /// <summary>
        /// Gets or sets the number of frames that the system is allowed to queue for rendering.
        /// <para>(Also see DirectX SDK: IDXGIDevice1::GetMaximumFrameLatency, IDXGIDevice1::SetMaximumFrameLatency)</para>
        /// </summary>
        property UInt32 MaximumFrameLatency
        {
            UInt32 get();
            void set (UInt32 value);
        }

    internal:

        Device1(void)
        { }

		Device1(IDXGIDevice1* pNativeIDXGIDevice1) : Device(pNativeIDXGIDevice1)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(IDXGIDevice1)); }
        }
    };
} } } }
