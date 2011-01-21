//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectObject.h"
#include "D3D10StateBlockMask.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

ref class EffectVariable;

    /// <summary>
    /// A pass interface encapsulates state assignments within a technique.
    /// <para>(Also see DirectX SDK: ID3D10EffectPass)</para>
    /// </summary>
    public ref class EffectPass :
        public DirectObject
    {
    public: 
        /// <summary>
        /// Set the state contained in a pass to the device.
        /// <para>(Also see DirectX SDK: ID3D10EffectPass::Apply)</para>
        /// </summary>
        void Apply();

        /// <summary>
        /// Generate a mask for allowing/preventing state changes.
        /// <para>(Also see DirectX SDK: ID3D10EffectPass::ComputeStateBlockMask)</para>
        /// </summary>
        /// <returns>A state-block mask (see <see cref="StateBlockMask"/>)<seealso cref="StateBlockMask"/>.</returns>
        StateBlockMask^ ComputeStateBlockMask();

        /// <summary>
        /// Get an annotation by index.
        /// <para>(Also see DirectX SDK: ID3D10EffectPass::GetAnnotationByIndex)</para>
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        /// <returns>An EffectVariable instance.</returns>
        EffectVariable^ GetAnnotationByIndex(UInt32 index);

        /// <summary>
        /// Get an annotation by name.
        /// <para>(Also see DirectX SDK: ID3D10EffectPass::GetAnnotationByName)</para>
        /// </summary>
        /// <param name="name">The name of the annotation.</param>
        /// <returns>An EffectVariable instance.</returns>
        EffectVariable^ GetAnnotationByName(String^ name);

        /// <summary>
        /// Get a pass description.
        /// <para>(Also see DirectX SDK: ID3D10EffectPass::GetDesc)</para>
        /// </summary>
        property PassDescription Description
        {
            PassDescription get();
        }

        /// <summary>
        /// Get a geometry-shader description.
        /// <para>(Also see DirectX SDK: ID3D10EffectPass::GetGeometryShaderDesc)</para>
        /// </summary>
        property PassShaderDescription GeometryShaderDescription
        {
            PassShaderDescription get();
        }

        /// <summary>
        /// Get a pixel-shader description.
        /// <para>(Also see DirectX SDK: ID3D10EffectPass::GetPixelShaderDesc)</para>
        /// </summary>
        property PassShaderDescription PixelShaderDescription
        {
            PassShaderDescription get();
        }

        /// <summary>
        /// Get a vertex-shader description.
        /// <para>(Also see DirectX SDK: ID3D10EffectPass::GetVertexShaderDesc)</para>
        /// </summary>
        property PassShaderDescription VertexShaderDescription
        {
            PassShaderDescription get();
        }

        /// <summary>
        /// Test a pass to see if it contains valid syntax.
        /// <para>(Also see DirectX SDK: ID3D10EffectPass::IsValid)</para>
        /// </summary>
        property Boolean IsValid
        {
            Boolean get();
        }
    internal:

        EffectPass(void)
        { }

        EffectPass(ID3D10EffectPass* pNativeID3D10EffectPass) : 
            DirectObject(pNativeID3D10EffectPass)
        { }
    };
} } } }
