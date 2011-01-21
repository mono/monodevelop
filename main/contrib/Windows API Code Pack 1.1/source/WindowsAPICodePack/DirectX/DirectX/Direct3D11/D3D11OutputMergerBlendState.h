//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    // REVIEW: is a ColorRgba really the correct abstraction for the blend factors?
    // It's true that it maps data-wise to the blend factor array the unmanaged API
    // expects, but blend factors aren't really colors per se.

    /// <summary>
    /// Stores the blend state, including blend factors and mask, for the merger pipeline stage
    /// </summary>
    public ref class OutputMergerBlendState
    {
    public:
        /// <summary>
        /// Gets the BlendState object for this merger pipeline stage blend state
        /// </summary>
        property BlendState^ BlendState
        {
            Direct3D11::BlendState^ get(void) { return blendState; }
        }

        /// <summary>
        /// Gets the blend factors for this merger pipeline stage blend state
        /// </summary>
        property ColorRgba BlendFactor
        {
            ColorRgba get(void) { return blendFactor; }
        }

        /// <summary>
        /// Gets the sample mask for this merger pipeline stage blend state
        /// </summary>
        property UInt32 SampleMask
        {
            UInt32 get(void) { return sampleMask; }
        }

        OutputMergerBlendState(void)
        {
            SetDefault();
        }

        OutputMergerBlendState(Direct3D11::BlendState^ blendState)
        {
            SetDefault();
            this->blendState = blendState;
        }

        OutputMergerBlendState(Direct3D11::BlendState^ blendState, ColorRgba blendFactor, UInt32 sampleMask)
        {
            this->blendState = blendState;
            this->blendFactor = blendFactor;
            this->sampleMask = sampleMask;
        }

    private:

        void SetDefault()
        {
            blendState = nullptr;
            blendFactor = ColorRgba(1.0, 1.0, 1.0, 1.0);
            sampleMask = 0xffffffff;
        }

        Direct3D11::BlendState^ blendState;
        ColorRgba blendFactor;
        UInt32 sampleMask;
    };
} } } }