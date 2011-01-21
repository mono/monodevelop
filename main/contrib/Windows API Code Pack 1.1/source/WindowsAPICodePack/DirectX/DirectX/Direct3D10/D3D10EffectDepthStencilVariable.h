//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10EffectVariable.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
ref class DepthStencilState;

    /// <summary>
    /// A depth-stencil-variable interface accesses depth-stencil state.
    /// <para>(Also see DirectX SDK: ID3D10EffectDepthStencilVariable)</para>
    /// </summary>
    public ref class EffectDepthStencilVariable :
        public EffectVariable
    {
    public: 
        /// <summary>
        /// Get a variable that contains depth-stencil state.
        /// <para>(Also see DirectX SDK: ID3D10EffectDepthStencilVariable::GetBackingStore)</para>
        /// </summary>
        /// <param name="index">Index into an array of depth-stencil-state descriptions. If there is only one depth-stencil variable in the effect, use 0.</param>
        /// <returns>A depth-stencil-state description (see <see cref="DepthStencilDescription"/>)<seealso cref="DepthStencilDescription"/>.</returns>
        DepthStencilDescription GetBackingStore(UInt32 index);

        /// <summary>
        /// Get a depth-stencil object.
        /// <para>(Also see DirectX SDK: ID3D10EffectDepthStencilVariable::GetDepthStencilState)</para>
        /// </summary>
        /// <param name="index">Index into an array of depth-stencil interfaces. If there is only one depth-stencil interface, use 0.</param>
        /// <returns>A blend-state interface (see DepthStencilState Object).</returns>
        DepthStencilState^ GetDepthStencilState(UInt32 index);

    internal:

        EffectDepthStencilVariable(void)
        { }

        EffectDepthStencilVariable(ID3D10EffectDepthStencilVariable* pNativeID3D10EffectDepthStencilVariable): 
            EffectVariable(pNativeID3D10EffectDepthStencilVariable)
        { }
    };
} } } }
