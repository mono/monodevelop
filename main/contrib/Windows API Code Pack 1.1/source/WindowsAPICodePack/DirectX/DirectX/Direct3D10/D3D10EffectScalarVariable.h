//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10EffectVariable.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

	// REVIEW: it appears that any given instance of ID3D10EffectScalarVariable, the type is
	// predetermined. So in theory, this class could be sub-classed by type-specific versions.
    // In that design, the As... properties would return the type-specific versions, not the
    // actual value. Then the actual value can be retrieved from the type-specific versions.
    // The array types are a little tricky; there's no way to query the length of the array
    // from code. The caller just has to know the length used in the variable of the effect
    // code itself. But it might still be feasible to have a factory method that creates the
    // necessary sub-type with a given length, and then allow the sub-type to provide a fully
    // managed API to the data.
    // 
    // Such a design could lend a little extra type-safety and explicitness to the API.
    // Same thing applies to D3D10EffectVectorVariable.

    /// <summary>
    /// An effect-scalar-variable interface accesses scalar values.
    /// <para>(Also see DirectX SDK: ID3D10EffectScalarVariable)</para>
    /// </summary>
    public ref class EffectScalarVariable :
        public EffectVariable
    {
    public: 
        /// <summary>
        /// Get or set a boolean variable.
        /// <para>(Also see DirectX SDK: ID3D10EffectScalarVariable::GetBool, ID3D10EffectScalarVariable::SetBool)</para>
        /// </summary>
        property Boolean AsBoolean
        {
            Boolean get();
            void set(Boolean);
        }

        /// <summary>
        /// Get or set an integer variable.
        /// <para>(Also see DirectX SDK: ID3D10EffectScalarVariable::GetInt, ID3D10EffectScalarVariable::SetInt)</para>
        /// </summary>
        property Int32 AsInt32
        {
            Int32 get();
            void set(Int32);
        }

        /// <summary>
        /// Get or set a floating-point variable.
        /// <para>(Also see DirectX SDK: ID3D10EffectScalarVariable::GetFloat, ID3D10EffectScalarVariable::SetFloat)</para>
        /// </summary>
        property Single AsSingle
        {
            Single get();
            void set(Single);
        }


        // REVIEW: see comment for D3D10EffectVectorVariable for discussion of
        // theoretical possibility of supporting these as properties.

        /// <summary>
        /// Get an array of boolean variables.
        /// <para>(Also see DirectX SDK: ID3D10EffectScalarVariable::GetBoolArray)</para>
        /// </summary>
        /// <param name="count">The number of array elements to get.</param>
        /// <returns>The array of boolean variables.</returns>
        array<Boolean>^ GetBooleanArray(UInt32 count);

        /// <summary>
        /// Set an array of boolean variables.
        /// <para>(Also see DirectX SDK: ID3D10EffectScalarVariable::SetBoolArray)</para>
        /// </summary>
        /// <param name="data">The array of variables to set.</param>
        void SetBooleanArray(array<Boolean>^ data);

        /// <summary>
        /// Get an array of integer variables.
        /// <para>(Also see DirectX SDK: ID3D10EffectScalarVariable::GetIntArray)</para>
        /// </summary>
        /// <param name="count">The number of array elements to get.</param>
        /// <returns>The array of integer variables.</returns>
        array<Int32>^ GetInt32Array(UInt32 count);

        /// <summary>
        /// Set an array of integer variables.
        /// <para>(Also see DirectX SDK: ID3D10EffectScalarVariable::SetIntArray)</para>
        /// </summary>
        /// <param name="data">The array of variables to set.</param>
        void SetInt32Array(array<Int32>^ data);

        /// <summary>
        /// Get an array of floating-point variables.
        /// <para>(Also see DirectX SDK: ID3D10EffectScalarVariable::GetFloatArray)</para>
        /// </summary>
        /// <param name="count">The number of array elements to get.</param>
        /// <returns>The array of floating-point variables.</returns>
        array<Single>^ GetSingleArray(UInt32 count);

        /// <summary>
        /// Set an array of floating-point variables.
        /// <para>(Also see DirectX SDK: ID3D10EffectScalarVariable::SetFloatArray)</para>
        /// </summary>
        /// <param name="data">The array of variables to set.</param>
        void SetSingleArray(array<Single>^ data);

    internal:

        EffectScalarVariable(void)
        { }

        EffectScalarVariable(ID3D10EffectScalarVariable* pNativeID3D10EffectScalarVariable)
            : EffectVariable(pNativeID3D10EffectScalarVariable)
        { }
    };
} } } }
