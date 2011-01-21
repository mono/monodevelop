//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10EffectVariable.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
ref class D3DBuffer;
ref class ShaderResourceView;

    /// <summary>
    /// A constant-buffer interface accesses constant buffers or texture buffers.
    /// <para>(Also see DirectX SDK: ID3D10EffectConstantBuffer)</para>
    /// </summary>
    public ref class EffectConstantBuffer :
        public EffectVariable
    {
    public: 
        /// <summary>
        /// Gets or sets a constant-buffer.
        /// <para>(Also see DirectX SDK: ID3D10EffectConstantBuffer::GetConstantBuffer, ID3D10EffectConstantBuffer::SetConstantBuffer)</para>
        /// </summary>
        property D3DBuffer^ ConstantBuffer
        {
            D3DBuffer^ get(void);
            void set(D3DBuffer^ constantBuffer);
        }

        /// <summary>
        /// Gets or sets a texture-buffer.
        /// <para>(Also see DirectX SDK: ID3D10EffectConstantBuffer::GetTextureBuffer, ID3D10EffectConstantBuffer::SetTextureBuffer)</para>
        /// </summary>
        property ShaderResourceView^  TextureBuffer
        {
            ShaderResourceView^  get(void);
            void set(ShaderResourceView^ textureBuffer);
        }

    internal:
        EffectConstantBuffer()
        { }

        EffectConstantBuffer(ID3D10EffectConstantBuffer* pNativeID3D10EffectConstantBuffer): 
            EffectVariable(pNativeID3D10EffectConstantBuffer)
        { }


        EffectConstantBuffer(ID3D10EffectConstantBuffer* pNativeID3D10EffectConstantBuffer, bool deletable): 
            EffectVariable(pNativeID3D10EffectConstantBuffer, deletable)
        { }

    };
} } } }
