//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10EffectVariable.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
ref class SamplerState;

    /// <summary>
    /// A sampler interface accesses sampler state.
    /// <para>(Also see DirectX SDK: ID3D10EffectSamplerVariable)</para>
    /// </summary>
    public ref class EffectSamplerVariable :
        public EffectVariable
    {
    public: 
        /// <summary>
        /// Get a variable that contains sampler state.
        /// <para>(Also see DirectX SDK: ID3D10EffectSamplerVariable::GetBackingStore)</para>
        /// </summary>
        /// <param name="index">Index into an array of sampler descriptions. If there is only one sampler variable in the effect, use 0.</param>
        /// <returns>A sampler description (see <see cref="SamplerDescription"/>)<seealso cref="SamplerDescription"/>.</returns>
        SamplerDescription GetBackingStore(UInt32 index);

        /// <summary>
        /// Get a sampler object.
        /// <para>(Also see DirectX SDK: ID3D10EffectSamplerVariable::GetSampler)</para>
        /// </summary>
        /// <param name="index">Index into an array of sampler interfaces. If there is only one sampler interface, use 0.</param>
        /// <returns>A sampler interface (see SamplerState).</returns>
        SamplerState^ GetSampler(UInt32 index);

    internal:

        EffectSamplerVariable(void)
        { }

        EffectSamplerVariable(ID3D10EffectSamplerVariable* pNativeID3D10EffectSamplerVariable)
            : EffectVariable(pNativeID3D10EffectSamplerVariable)
        { }
    };
} } } }
