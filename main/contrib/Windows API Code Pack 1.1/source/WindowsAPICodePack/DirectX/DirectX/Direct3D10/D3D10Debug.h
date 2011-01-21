//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectUnknown.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {
ref class SwapChain;
}}}}

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;
using namespace Microsoft::WindowsAPICodePack::DirectX;

    /// <summary>
    /// A debug interface controls debug settings, validates pipeline state and can only be used if the debug layer is turned on.
    /// <para>(Also see DirectX SDK: ID3D10Debug)</para>
    /// </summary>
    public ref class D3DDebug :
        public DirectUnknown
    {
    public: 
        /// <summary>
        /// Gets or sets a bitfield of flags that indicates which debug features are on or off.
        /// <para>(Also see DirectX SDK: ID3D10Debug::GetFeatureMask)</para>
        /// </summary>
        property DebugFeatures FeatureMask
        {
            DebugFeatures get();
            void set(DebugFeatures value);
        }

        /// <summary>
        /// Gets or sets the number of milliseconds to sleep after SwapChain.Present is called.
        /// <para>(Also see DirectX SDK: ID3D10Debug::GetPresentPerRenderOpDelay)</para>
        /// </summary>
        property UInt32 PresentPerRenderOperationDelay
        {
            UInt32 get();
            void set(UInt32 value);
        }

        /// <summary>
        /// Get the swap chain that the runtime will use for automatically calling SwapChain.Present.
        /// <para>(Also see DirectX SDK: ID3D10Debug::GetSwapChain)</para>
        /// </summary>
        /// <param name="outSwapChain">Swap chain that the runtime will use for automatically calling SwapChain.Present.</param>
        property SwapChain^ RuntimeSwapChain
        {
            SwapChain^ get();
            void set(SwapChain^ value);
        }

        /// <summary>
        /// Check the validity of pipeline state.
        /// <para>(Also see DirectX SDK: ID3D10Debug::Validate)</para>
        /// </summary>
        void Validate();
    
    internal:
        D3DDebug()
        { }

        D3DDebug(ID3D10Debug* pNativeID3D10Debug) : DirectUnknown(pNativeID3D10Debug)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10Debug)); }
        }
    };
} } } }
