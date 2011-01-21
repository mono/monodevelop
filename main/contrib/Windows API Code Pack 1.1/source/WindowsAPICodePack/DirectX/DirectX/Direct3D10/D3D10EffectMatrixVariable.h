//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10EffectVariable.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A matrix-variable interface accesses a matrix.
    /// <para>(Also see DirectX SDK: ID3D10EffectMatrixVariable)</para>
    /// </summary>
    public ref class EffectMatrixVariable :
        public EffectVariable
    {
    public: 
        /// <summary>
        /// Get a 4x4 floating point matrix.
        /// <para>(Also see DirectX SDK: ID3D10EffectMatrixVariable::GetMatrix, ID3D10EffectMatrixVariable::SetMatrix)</para>
        /// </summary>
        property Matrix4x4F Matrix
        {
            Matrix4x4F get();
            void set(Matrix4x4F);
        }

        /// <summary>
        /// Set or get a 4x4 floating point matrix transpose.
        /// <para>(Also see DirectX SDK: ID3D10EffectMatrixVariable::GetMatrixTranspose, ID3D10EffectMatrixVariable::SetMatrixTranspose)</para>
        /// </summary>
        property Matrix4x4F MatrixTranspose
        {
            Matrix4x4F get();
            void set(Matrix4x4F);
        }

        /// <summary>
        /// Get an array of 4x4 matrices.
        /// <para>(Also see DirectX SDK: ID3D10EffectMatrixVariable::GetMatrixArray)</para>
        /// </summary>
        /// <param name="offset">The offset (in number of matrices) between the start of the array and the first matrix returned.</param>
        /// <param name="count">The number of matrices requested in the returned collection.</param>
        /// <returns>A two dimentional array of matrices.</returns>
        array<Matrix4x4F>^ GetMatrixArray(UInt32 offset, UInt32 count);

        /// <summary>
        /// Transpose and get an array of 4x4 floating-point matrices.
        /// <para>(Also see DirectX SDK: ID3D10EffectMatrixVariable::GetMatrixTransposeArray)</para>
        /// </summary>
        /// <param name="offset">The offset (in number of matrices) between the start of the array and the first matrix to get.</param>
        /// <param name="count">The number of matrices in the array to get.</param>
        /// <returns>A two dimentional array of tranposed matrices.</returns>
        array<Matrix4x4F>^ GetMatrixTransposeArray(UInt32 offset, UInt32 count);

        /// <summary>
        /// Set an array of 4x4 floating-point matrices.
        /// <para>(Also see DirectX SDK: ID3D10EffectMatrixVariable::SetMatrixArray)</para>
        /// </summary>
        /// <param name="data">The matrix two dimentional array.</param>
        /// <param name="offset">The number of matrix elements to skip from the start of the array.</param>
        void SetMatrixArray(array<Matrix4x4F>^ data, UInt32 offset);

        /// <summary>
        /// Transpose and set an array of 4x4 floating-point matrices.
        /// <para>(Also see DirectX SDK: ID3D10EffectMatrixVariable::SetMatrixTransposeArray)</para>
        /// </summary>
        /// <param name="data">A two dimentional array of matrices.</param>
        /// <param name="offset">The offset (in number of matrices) between the start of the array and the first matrix to set.</param>
        void SetMatrixTransposeArray(array<Matrix4x4F>^ data, UInt32 offset);

    internal:

        EffectMatrixVariable(void)
        { }

        EffectMatrixVariable(ID3D10EffectMatrixVariable* pNativeID3D10EffectMatrixVariable) : 
            EffectVariable(pNativeID3D10EffectMatrixVariable)
        { }
    };
} } } }
