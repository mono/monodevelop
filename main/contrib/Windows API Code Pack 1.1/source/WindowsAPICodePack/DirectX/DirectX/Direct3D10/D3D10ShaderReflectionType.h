//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectObject.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

ref class ShaderReflectionType;

    /// <summary>
    /// This shader-reflection interface provides access to variable type. This class does not inherit from anything, but does declare the following methods:
    /// <para>(Also see DirectX SDK: ID3D10ShaderReflectionType)</para>
    /// </summary>
    public ref class ShaderReflectionType :
        public DirectObject
    {
    public: 
        /// <summary>
        /// Get the description of a shader-reflection-variable type.
        /// <para>(Also see DirectX SDK: ID3D10ShaderReflectionType::GetDesc)</para>
        /// </summary>
        property ShaderTypeDescription Description
        {
            ShaderTypeDescription get();
        }

        /// <summary>
        /// Get a shader-reflection-variable type by index.
        /// <para>(Also see DirectX SDK: ID3D10ShaderReflectionType::GetMemberTypeByIndex)</para>
        /// </summary>
        /// <param name="index">Zero-based index.</param>
        ShaderReflectionType^ GetMemberTypeByIndex(UInt32 index);

        /// <summary>
        /// Get a shader-reflection-variable type by name.
        /// <para>(Also see DirectX SDK: ID3D10ShaderReflectionType::GetMemberTypeByName)</para>
        /// </summary>
        /// <param name="name">Member name.</param>
        ShaderReflectionType^ GetMemberTypeByName(String^ name);

        /// <summary>
        /// Get a shader-reflection-variable type.
        /// <para>(Also see DirectX SDK: ID3D10ShaderReflectionType::GetMemberTypeName)</para>
        /// </summary>
        /// <param name="index">Zero-based index.</param>
        String^ GetMemberTypeName(UInt32 index);

    internal:
        ShaderReflectionType()
        { }

        ShaderReflectionType(ID3D10ShaderReflectionType* pNativeID3D10ShaderReflectionType) :
            DirectObject(pNativeID3D10ShaderReflectionType)
        { }
    };
} } } }
