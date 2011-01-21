//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectObject.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

ref class ShaderReflectionType;

    /// <summary>
    /// This shader-reflection interface provides access to a variable. This class does not inherit from anything, but does declare the following methods:
    /// <para>(Also see DirectX SDK: ID3D10ShaderReflectionVariable)</para>
    /// </summary>
    public ref class ShaderReflectionVariable :
        public DirectObject
    {
    public: 
        /// <summary>
        /// Get a shader-variable description.
        /// <para>(Also see DirectX SDK: ID3D10ShaderReflectionVariable::GetDesc)</para>
        /// </summary>
        property ShaderVariableDescription Description
        {
            ShaderVariableDescription get();
        }

        /// <summary>
        /// Get a shader-variable type.
        /// <para>(Also see DirectX SDK: ID3D10ShaderReflectionVariable::GetType)</para>
        /// </summary>
        property ShaderReflectionType^ ShaderReflectionType
        {
            Direct3D10::ShaderReflectionType^ get();
        }

    internal:
        ShaderReflectionVariable()
        {
        }

        ShaderReflectionVariable(ID3D10ShaderReflectionVariable* pNativeID3D10ShaderReflectionVariable) :
            DirectObject(pNativeID3D10ShaderReflectionVariable)
        { }
    };
} } } }
