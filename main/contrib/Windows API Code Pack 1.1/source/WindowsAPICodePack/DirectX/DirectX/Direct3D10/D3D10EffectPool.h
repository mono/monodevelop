//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectUnknown.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

ref class Effect;

    /// <summary>
    /// A pool interface represents a common memory space (or pool) for sharing variables between effects.
    /// <para>(Also see DirectX SDK: ID3D10EffectPool)</para>
    /// </summary>
    public ref class EffectPool :
        public DirectUnknown
    {
    public: 
        /// <summary>
        /// Get the effect that created the effect pool.
        /// <para>(Also see DirectX SDK: ID3D10EffectPool::AsEffect)</para>
        /// </summary>
        property Effect^ AsEffect
        {
            Effect^ get(void);
        }

    internal:

        EffectPool(void)
        { }

		// REVIEW: appears to be no supported way to create one of these objects (other than
		// the kludge of using DirectHelper to wrap an existing interface the client code had
		// to get themselves). The library should wrap D3DX10CreateEffectPoolFromFile. This
		// involves adding several enumerations and supporting the ID3DX10ThreadPump interface.
		//
		// Ironically, there are at least eight methods in other classes (D3DDevice, D3DDevice1)
		// that take an instance of this as an argument. If the client code can't create one of
		// these, it also can't call those methods.

		CA_SUPPRESS_MESSAGE("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")
		EffectPool(ID3D10EffectPool* pNativeID3D10EffectPool)
			: DirectUnknown(pNativeID3D10EffectPool)
        { }
    };
} } } }
