//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10EffectVariable.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
ref class RasterizerState;

    /// <summary>
    /// A rasterizer-variable interface accesses rasterizer state.
    /// <para>(Also see DirectX SDK: ID3D10EffectRasterizerVariable)</para>
    /// </summary>
    public ref class EffectRasterizerVariable :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D10::EffectVariable
    {
    public: 
        /// <summary>
        /// Get a variable that contains rasteriser state.
        /// <para>(Also see DirectX SDK: ID3D10EffectRasterizerVariable::GetBackingStore)</para>
        /// </summary>
        /// <param name="index">Index into an array of rasteriser-state descriptions. If there is only one rasteriser variable in the effect, use 0.</param>
        /// <returns>A rasteriser-state description (see <see cref="RasterizerDescription"/>)<seealso cref="RasterizerDescription"/>.</returns>
        RasterizerDescription GetBackingStore(UInt32 index);

        /// <summary>
        /// Get a rasterizer object.
        /// <para>(Also see DirectX SDK: ID3D10EffectRasterizerVariable::GetRasterizerState)</para>
        /// </summary>
        /// <param name="index">Index into an array of rasterizer interfaces. If there is only one rasterizer interface, use 0.</param>
        /// <returns>A rasterizer interface (see RasterizerState Object).</returns>
        RasterizerState^ GetRasterizerState(UInt32 index);

    internal:

		EffectRasterizerVariable(void)
        { }

		EffectRasterizerVariable(ID3D10EffectRasterizerVariable* pNativeID3D10EffectRasterizerVariable)
			: EffectVariable(pNativeID3D10EffectRasterizerVariable)
        { }

    };
} } } }
