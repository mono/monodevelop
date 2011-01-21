//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10EffectVariable.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// A string-variable interface accesses a string variable.
    /// <para>(Also see DirectX SDK: ID3D10EffectStringVariable)</para>
    /// </summary>
    public ref class EffectStringVariable :
        public EffectVariable
    {
    public: 
        /// <summary>
        /// Get the string.
        /// <para>(Also see DirectX SDK: ID3D10EffectStringVariable::GetString)</para>
        /// </summary>
        property String^ StringValue
        {
            String^ get();
        }

        /// <summary>
        /// Get an array of strings.
        /// <para>(Also see DirectX SDK: ID3D10EffectStringVariable::GetStringArray)</para>
        /// </summary>
        /// <param name="offset">The offset (in number of strings) between the start of the array and the first string to get.</param>
        /// <param name="count">The number of strings requested in the returned array.</param>
        /// <returns>The string array.</returns>
        array<String^>^ GetStringArray(UInt32 offset, UInt32 count);

    internal:

        EffectStringVariable(void)
        { }

        EffectStringVariable(ID3D10EffectStringVariable* pNativeID3D10EffectStringVariable)
            : EffectVariable(pNativeID3D10EffectStringVariable)
        { }
    };
} } } }
