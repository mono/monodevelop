//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectUnknown.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

ref class D3DDevice; 
ref class D3DDevice1;
ref class EffectConstantBuffer;
ref class EffectTechnique;
ref class EffectVariable;

    /// <summary>
    /// An Effect interface manages a set of state objects, resources and shaders for implementing a rendering effect.
    /// <para>(Also see DirectX SDK: ID3D10Effect)</para>
    /// </summary>
    public ref class Effect :
        public DirectUnknown
    {
    public: 
        /// <summary>
        /// Get a constant buffer by index.
        /// <para>(Also see DirectX SDK: ID3D10Effect::GetConstantBufferByIndex)</para>
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        EffectConstantBuffer^ GetConstantBufferByIndex(UInt32 index);

        /// <summary>
        /// Get a constant buffer by name.
        /// <para>(Also see DirectX SDK: ID3D10Effect::GetConstantBufferByName)</para>
        /// </summary>
        /// <param name="name">The constant-buffer name.</param>
        EffectConstantBuffer^ GetConstantBufferByName(String^ name);

        /// <summary>
        /// Get an effect description.
        /// <para>(Also see DirectX SDK: ID3D10Effect::GetDesc)</para>
        /// </summary>
        property EffectDescription Description
        {
            EffectDescription get();
        }

        /// <summary>
        /// Get the device that created the effect as 10.0 device.
        /// <para>(Also see DirectX SDK: ID3D10Effect::GetDevice)</para>
        /// </summary>
        property D3DDevice^ Device
        {
            D3DDevice^ get(void);
        }

        /// <summary>
        /// Get a technique by index.
        /// <para>(Also see DirectX SDK: ID3D10Effect::GetTechniqueByIndex)</para>
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        EffectTechnique^ GetTechniqueByIndex(UInt32 index);

        /// <summary>
        /// Get a technique by name.
        /// <para>(Also see DirectX SDK: ID3D10Effect::GetTechniqueByName)</para>
        /// </summary>
        /// <param name="name">The name of the technique.</param>
        EffectTechnique^ GetTechniqueByName(String^ name);

        /// <summary>
        /// Get a variable by index.
        /// <para>(Also see DirectX SDK: ID3D10Effect::GetVariableByIndex)</para>
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        EffectVariable^ GetVariableByIndex(UInt32 index);

        /// <summary>
        /// Get a variable by name.
        /// <para>(Also see DirectX SDK: ID3D10Effect::GetVariableByName)</para>
        /// </summary>
        /// <param name="name">The variable name.</param>
        EffectVariable^ GetVariableByName(String^ name);

        /// <summary>
        /// Get a variable by semantic.
        /// <para>(Also see DirectX SDK: ID3D10Effect::GetVariableBySemantic)</para>
        /// </summary>
        /// <param name="semantic">The semantic name.</param>
        EffectVariable^ GetVariableBySemantic(String^ semantic);

        /// <summary>
        /// Test an effect to see if the reflection metadata has been removed from memory.
        /// <para>(Also see DirectX SDK: ID3D10Effect::IsOptimized)</para>
        /// </summary>
        property Boolean IsOptimized
        {
            Boolean get();
        }

        /// <summary>
        /// Test an effect to see if it is part of a memory pool.
        /// <para>(Also see DirectX SDK: ID3D10Effect::IsPool)</para>
        /// </summary>
        property Boolean IsPool
        {
            Boolean get();
        }

        /// <summary>
        /// Test an effect to see if it contains valid syntax.
        /// <para>(Also see DirectX SDK: ID3D10Effect::IsValid)</para>
        /// </summary>
        property Boolean IsValid
        {
            Boolean get();
        }

        /// <summary>
        /// Minimize the amount of memory required for an effect.
        /// <para>(Also see DirectX SDK: ID3D10Effect::Optimize)</para>
        /// </summary>
        void Optimize();

    internal:
        Effect() 
        {}

        Effect(ID3D10Effect* pNativeID3D10Effect) : 
            DirectUnknown(pNativeID3D10Effect)
        { }

    };
} } } }
