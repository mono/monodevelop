//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectObject.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

ref class EffectType;

    /// <summary>
    /// The EffectType interface accesses effect variables by type.
    /// <para>(Also see DirectX SDK: ID3D10EffectType)</para>
    /// </summary>
    public ref class EffectType :
        public DirectObject
    {
    public: 
        /// <summary>
        /// Get an effect-type description.
        /// <para>(Also see DirectX SDK: ID3D10EffectType::GetDesc)</para>
        /// </summary>
        /// <param name="Description">A effect-type description. See EffectTypeDescription.</param>
        property EffectTypeDescription Description
        {
            EffectTypeDescription get();
        }

        /// <summary>
        /// Get the name of a member.
        /// <para>(Also see DirectX SDK: ID3D10EffectType::GetMemberName)</para>
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        String^ GetMemberName(UInt32 index);

        /// <summary>
        /// Get the semantic attached to a member.
        /// <para>(Also see DirectX SDK: ID3D10EffectType::GetMemberSemantic)</para>
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        String^ GetMemberSemantic(UInt32 index);

        /// <summary>
        /// Get a member type by index.
        /// <para>(Also see DirectX SDK: ID3D10EffectType::GetMemberTypeByIndex)</para>
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        EffectType^ GetMemberTypeByIndex(UInt32 index);

        /// <summary>
        /// Get an member type by name.
        /// <para>(Also see DirectX SDK: ID3D10EffectType::GetMemberTypeByName)</para>
        /// </summary>
        /// <param name="name">A member's name.</param>
        EffectType^ GetMemberTypeByName(String^ name);

        /// <summary>
        /// Get a member type by semantic.
        /// <para>(Also see DirectX SDK: ID3D10EffectType::GetMemberTypeBySemantic)</para>
        /// </summary>
        /// <param name="semantic">A semantic.</param>
        EffectType^ GetMemberTypeBySemantic(String^ semantic);

        /// <summary>
        /// Tests that the effect type is valid.
        /// <para>(Also see DirectX SDK: ID3D10EffectType::IsValid)</para>
        /// </summary>
        property Boolean IsValid
        {
            Boolean get();
        }

    internal:

        EffectType()
        { }

        EffectType(ID3D10EffectType* pNativeID3D10EffectType) 
           : DirectObject(pNativeID3D10EffectType)
        { }

    };
} } } }
