//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectUnknown.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {
ref class SwapChain;
}}}}

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

ref class DeviceContext;

    /// <summary>
    /// A debug interface controls debug settings, validates pipeline state and can only be used if the debug layer is turned on.
    /// <para>(Also see DirectX SDK: ID3D11Debug)</para>
    /// </summary>
    public ref class D3DDebug :
        public DirectUnknown
    {
    public: 
        /// <summary>
        /// Gets or sets a bitfield of flags that indicates which debug features are on or off.
        /// <para>(Also see DirectX SDK: ID3D11Debug::GetFeatureMask)</para>
        /// </summary>
        property DebugFeatures FeatureMask
        {
            DebugFeatures get();
            void set(DebugFeatures value);
        }

        /// <summary>
        /// Gets or sets the number of milliseconds to sleep after SwapChain.Present is called.
        /// <para>(Also see DirectX SDK: ID3D11Debug::GetPresentPerRenderOpDelay)</para>
        /// </summary>
        property UInt32 PresentPerRenderOperationDelay
        {
            UInt32 get();
            void set(UInt32 value);
        }

        /// <summary>
        /// Get or set the swap chain that the runtime will use for automatically calling SwapChain.Present.
        /// <para>(Also see DirectX SDK: ID3D11Debug::GetSwapChain)</para>
        /// </summary>
        property SwapChain^ RuntimeSwapChain
        {
            SwapChain^ get();
            void set(SwapChain^ value);
        }

        /// <summary>
        /// Check to see if the pipeline state is valid.
        /// <para>(Also see DirectX SDK: ID3D11Debug::ValidateContext)</para>
        /// </summary>
        /// <param name="context">The DeviceContext, that represents a device context.</param>
        void ValidateContext(DeviceContext^ context);

    internal:

        D3DDebug(void)
        { }

		D3DDebug(ID3D11Debug* pNativeID3D11Debug) : DirectUnknown(pNativeID3D11Debug)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D11Debug)); }
        }
    };
} } } }
