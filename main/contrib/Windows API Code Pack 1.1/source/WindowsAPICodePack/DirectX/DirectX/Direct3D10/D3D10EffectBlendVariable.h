//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10EffectVariable.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
ref class BlendState;

    /// <summary>
    /// The blend-variable interface accesses blend state.
    /// <para>(Also see DirectX SDK: ID3D10EffectBlendVariable)</para>
    /// </summary>
    public ref class EffectBlendVariable :
        public EffectVariable
    {
    public: 
        // REVIEW: Do we really need this at all? It's an alternative to retrieving a full
        // ID3D10EffectBlendVariable object, but in the managed environment does that really
        // make much sense as a supported usage? Also, the MSDN docs claim that the unmanaged
        // API has two "in" parameters and no "out". This is probably a doc bug, but should
        // double-check. Finally, if we do keep this method, probably should be renamed to
        // "GetBlendDescription".

        /// <summary>
        /// Get a blend-state variable.
        /// <para>(Also see DirectX SDK: ID3D10EffectBlendVariable::GetBackingStore)</para>
        /// </summary>
        /// <param name="index">Index into an array of blend-state descriptions. If there is only one blend-state variable in the effect, use 0.</param>
        /// <returns>A blend-state description (see <see cref="BlendDescription"/>)<seealso cref="BlendDescription"/>.</returns>
        BlendDescription GetBackingStore(UInt32 index);

        /// <summary>
        /// Get a blend-state object.
        /// <para>(Also see DirectX SDK: ID3D10EffectBlendVariable::GetBlendState)</para>
        /// </summary>
        /// <param name="index">index into an array of blend-state interfaces. If there is only one blend-state interface, use 0.</param>
        /// <returns>A blend-state interface (see BlendState Object).</returns>
        BlendState^ GetBlendState(UInt32 index);

    internal:

        EffectBlendVariable(void)
        { }

        EffectBlendVariable(ID3D10EffectBlendVariable* pNativeID3D10EffectBlendVariable) : EffectVariable(pNativeID3D10EffectBlendVariable)
        { }
    };
} } } }
