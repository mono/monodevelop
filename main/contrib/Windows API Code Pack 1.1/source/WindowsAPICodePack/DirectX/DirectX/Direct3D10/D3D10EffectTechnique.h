//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectObject.h"
#include "D3D10StateBlockMask.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

ref class EffectVariable;
ref class EffectPass;

    /// <summary>
    /// An EffectTechnique interface is a collection of passes.
    /// <para>(Also see DirectX SDK: ID3D10EffectTechnique)</para>
    /// </summary>
    public ref class EffectTechnique :
        public DirectObject
    {
    public: 
        /// <summary>
        /// Compute a state-block mask to allow/prevent state changes.
        /// <para>(Also see DirectX SDK: ID3D10EffectTechnique::ComputeStateBlockMask)</para>
        /// </summary>
        /// <returns>A state-block mask (see <see cref="StateBlockMask"/>)<seealso cref="StateBlockMask"/>.</returns>
        StateBlockMask^ ComputeStateBlockMask();

        /// <summary>
        /// Get an annotation by index.
        /// <para>(Also see DirectX SDK: ID3D10EffectTechnique::GetAnnotationByIndex)</para>
        /// </summary>
        /// <param name="index">The zero-based index of the interface pointer.</param>
        EffectVariable^ GetAnnotationByIndex(UInt32 index);

        /// <summary>
        /// Get an annotation by name.
        /// <para>(Also see DirectX SDK: ID3D10EffectTechnique::GetAnnotationByName)</para>
        /// </summary>
        /// <param name="name">Name of the annotation.</param>
        EffectVariable^ GetAnnotationByName(String^ name);

        /// <summary>
        /// Get a technique description.
        /// <para>(Also see DirectX SDK: ID3D10EffectTechnique::GetDesc)</para>
        /// </summary>
        property TechniqueDescription Description
        {
            TechniqueDescription get();
        }

        /// <summary>
        /// Get a pass by index.
        /// <para>(Also see DirectX SDK: ID3D10EffectTechnique::GetPassByIndex)</para>
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        EffectPass^ GetPassByIndex(UInt32 index);

        /// <summary>
        /// Get a pass by name.
        /// <para>(Also see DirectX SDK: ID3D10EffectTechnique::GetPassByName)</para>
        /// </summary>
        /// <param name="name">The name of the pass.</param>
        EffectPass^ GetPassByName(String^ name);

        /// <summary>
        /// Test a technique to see if it contains valid syntax.
        /// <para>(Also see DirectX SDK: ID3D10EffectTechnique::IsValid)</para>
        /// </summary>
        property Boolean IsValid
        {
            Boolean get();
        }
    internal:
        EffectTechnique()
        { }

        EffectTechnique(ID3D10EffectTechnique* pNativeID3D10EffectTechnique) : 
            DirectObject(pNativeID3D10EffectTechnique)
        { }

        EffectTechnique(ID3D10EffectTechnique* pNativeID3D10EffectTechnique, bool deletable) :
            DirectObject(pNativeID3D10EffectTechnique, deletable)
        {  }

    };
} } } }
