//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectObject.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

ref class ShaderReflectionVariable;

    /// <summary>
    /// This shader-reflection interface provides access to a constant buffer. This class does not inherit from anything, but does declare the following methods:
    /// <para>(Also see DirectX SDK: ID3D10ShaderReflectionConstantBuffer)</para>
    /// </summary>
    public ref class ShaderReflectionConstantBuffer :
        public DirectObject
    {
    public: 
        /// <summary>
        /// Get a constant-buffer description.
        /// <para>(Also see DirectX SDK: ID3D10ShaderReflectionConstantBuffer::GetDesc)</para>
        /// </summary>
        property ShaderBufferDescription Description
        {
            ShaderBufferDescription get();
        }

        /// <summary>
        /// Get a shader-reflection variable by index.
        /// <para>(Also see DirectX SDK: ID3D10ShaderReflectionConstantBuffer::GetVariableByIndex)</para>
        /// </summary>
        /// <param name="index">Zero-based index.</param>
        ShaderReflectionVariable^ GetVariableByIndex(UInt32 index);

        /// <summary>
        /// Get a shader-reflection variable by name.
        /// <para>(Also see DirectX SDK: ID3D10ShaderReflectionConstantBuffer::GetVariableByName)</para>
        /// </summary>
        /// <param name="name">Variable name.</param>
        ShaderReflectionVariable^ GetVariableByName(String^ name);
    internal:
        ShaderReflectionConstantBuffer()
        { }

        ShaderReflectionConstantBuffer(ID3D10ShaderReflectionConstantBuffer* pNativeID3D10ShaderReflectionConstantBuffer) :
            DirectObject(pNativeID3D10ShaderReflectionConstantBuffer)
        { }
    };
} } } }
