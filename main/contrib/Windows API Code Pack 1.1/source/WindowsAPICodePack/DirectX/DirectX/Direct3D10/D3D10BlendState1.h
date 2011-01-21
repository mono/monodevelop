//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10BlendState.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;

    /// <summary>
    /// This blend-state interface accesses blending state for a Direct3D 10.1 device for the output-merger stage.
    /// <para>(Also see DirectX SDK: ID3D10BlendState1)</para>
    /// </summary>
    public ref class BlendState1 :
        public BlendState
    {
    public: 
        /// <summary>
        /// Get the blend state.
        /// <para>(Also see DirectX SDK: ID3D10BlendState1::GetDesc1)</para>
        /// </summary>
        property BlendDescription1 Description1
        {
            BlendDescription1 get();
        }

    internal:
        BlendState1()
        { }

        BlendState1(ID3D10BlendState1* pNativeID3D10BlendState1) : BlendState(pNativeID3D10BlendState1)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10BlendState1)); }
        }
    };
} } } }
