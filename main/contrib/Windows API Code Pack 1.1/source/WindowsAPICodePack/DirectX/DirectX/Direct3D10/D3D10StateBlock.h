//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectUnknown.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

ref class D3DDevice;

    /// <summary>
    /// A state-block interface encapsulates render states.
    /// <para>(Also see DirectX SDK: ID3D10StateBlock)</para>
    /// </summary>
    public ref class StateBlock :
        public DirectUnknown
    {
    public: 
        /// <summary>
        /// Apply the state block to the current device state.
        /// <para>(Also see DirectX SDK: ID3D10StateBlock::Apply)</para>
        /// </summary>
        void Apply();

        /// <summary>
        /// Capture the current value of states that are included in a stateblock.
        /// <para>(Also see DirectX SDK: ID3D10StateBlock::Capture)</para>
        /// </summary>
        void Capture();

        /// <summary>
        /// Get the device.
        /// <para>(Also see DirectX SDK: ID3D10StateBlock::GetDevice)</para>
        /// </summary>
        property D3DDevice^ Device
        {
            D3DDevice^ get(void);
        }

        /// <summary>
        /// Release all references to device objects.
        /// <para>(Also see DirectX SDK: ID3D10StateBlock::ReleaseAllDeviceObjects)</para>
        /// </summary>
        void ReleaseAllDeviceObjects();

    internal:

        StateBlock(void)
        { }

        // REVIEW: this type has no CodePack support. It can only be created by
        // the client code creating an unmanaged instance of ID3D10StateBlock itself
        // and then using DirectHelpers::CreateIUnknownWrapper<T>() to wrap the
        // unmanaged interface. Really ought to provide something in CodePack to
        // support this type.

        CA_SUPPRESS_MESSAGE("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")
		StateBlock(ID3D10StateBlock* pNativeID3D10StateBlock) : 
            DirectUnknown(pNativeID3D10StateBlock)
        { }
    };
} } } }
