//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10EffectVariable.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
ref class GeometryShader;
ref class PixelShader;
ref class VertexShader;

    /// <summary>
    /// A shader-variable interface accesses a shader variable.
    /// <para>(Also see DirectX SDK: ID3D10EffectShaderVariable)</para>
    /// </summary>
    public ref class EffectShaderVariable :
        public EffectVariable
    {
    public: 
        /// <summary>
        /// Get a geometry shader.
        /// <para>(Also see DirectX SDK: ID3D10EffectShaderVariable::GetGeometryShader)</para>
        /// </summary>
        /// <param name="shaderIndex">A zero-based index.</param>
        /// <returns>A GeometryShader Object.</returns>
        GeometryShader^ GetGeometryShader(UInt32 shaderIndex);

        /// <summary>
        /// Get an input-signature description.
        /// <para>(Also see DirectX SDK: ID3D10EffectShaderVariable::GetInputSignatureElementDesc)</para>
        /// </summary>
        /// <param name="shaderIndex">A zero-based shader index.</param>
        /// <param name="elementIndex">A zero-based shader-element index.</param>
        /// <returns>A parameter description (see <see cref="SignatureParameterDescription"/>)<seealso cref="SignatureParameterDescription"/>.</returns>
        SignatureParameterDescription GetInputSignatureElementDescription(UInt32 shaderIndex, UInt32 elementIndex);

        /// <summary>
        /// Get an output-signature description.
        /// <para>(Also see DirectX SDK: ID3D10EffectShaderVariable::GetOutputSignatureElementDesc)</para>
        /// </summary>
        /// <param name="shaderIndex">A zero-based shader index.</param>
        /// <param name="elementIndex">A zero-based element index.</param>
        /// <returns>A parameter description (see <see cref="SignatureParameterDescription"/>)<seealso cref="SignatureParameterDescription"/>.</returns>
        SignatureParameterDescription GetOutputSignatureElementDescription(UInt32 shaderIndex, UInt32 elementIndex);

        /// <summary>
        /// Get a pixel shader.
        /// <para>(Also see DirectX SDK: ID3D10EffectShaderVariable::GetPixelShader)</para>
        /// </summary>
        /// <param name="shaderIndex">A zero-based index.</param>
        /// <returns>A PixelShader Object.</returns>
        PixelShader^ GetPixelShader(UInt32 shaderIndex);

        /// <summary>
        /// Get a shader description.
        /// <para>(Also see DirectX SDK: ID3D10EffectShaderVariable::GetShaderDesc)</para>
        /// </summary>
        /// <param name="shaderIndex">A zero-based index.</param>
        /// <returns>A shader description (see <see cref="EffectShaderDescription"/>)<seealso cref="EffectShaderDescription"/>.</returns>
        EffectShaderDescription GetShaderDescription(UInt32 shaderIndex);

        /// <summary>
        /// Get a vertex shader.
        /// <para>(Also see DirectX SDK: ID3D10EffectShaderVariable::GetVertexShader)</para>
        /// </summary>
        /// <param name="shaderIndex">A zero-based index.</param>
        /// <returns>A VertexShader Object.</returns>
        VertexShader^ GetVertexShader(UInt32 shaderIndex);

    internal:

        EffectShaderVariable(void)
        { }

        EffectShaderVariable(ID3D10EffectShaderVariable* pNativeID3D10EffectShaderVariable)
            : EffectVariable(pNativeID3D10EffectShaderVariable)
        { }
    };
} } } }
