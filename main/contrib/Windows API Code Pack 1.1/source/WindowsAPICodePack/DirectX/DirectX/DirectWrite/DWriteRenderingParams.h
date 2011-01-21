//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "DWriteFactory.h"
#include "DWriteEnums.h"


using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {

    /// <summary>
    /// Represents text rendering settings such as ClearType level, enhanced contrast, 
    /// and gamma correction for glyph rasterization and filtering.
    /// <para>(Also see DirectX SDK: IDWriteRenderingParams)</para>
    /// </summary>
    /// <remarks>Create a rendering params object by classing one of DWriteFactory's RenderingParam creation methods:
    /// <see cref="DWriteFactory::CreateRenderingParams()"/>, <see cref="DWriteFactory::CreateMonitorRenderingParams(IntPtr)"/>, or <see cref="DWriteFactory::CreateCustomRenderingParams(FLOAT,FLOAT,FLOAT,PixelGeometry,RenderingMode)"/></remarks>
    public ref class RenderingParams : public DirectUnknown
    {
    public: 

        /// <summary>
        /// Gets the gamma value used for gamma correction. Valid values must be
        /// greater than zero and cannot exceed 256.
        /// </summary>
        property FLOAT Gamma
        { 
            FLOAT get();
        }

        /// <summary>
        /// Gets the amount of contrast enhancement. Valid values are greater than
        /// or equal to zero.
        /// </summary>
        property FLOAT EnhancedContrast
        { 
            FLOAT get();
        }

        /// <summary>
        /// Gets the ClearType level. Valid values range from 0.0f (no ClearType) 
        /// to 1.0f (full ClearType).
        /// </summary>
        property FLOAT ClearTypeLevel
        { 
            FLOAT get();
        }

        /// <summary>
        /// Gets the pixel geometry.
        /// </summary>
        property DirectWrite::PixelGeometry PixelGeometry
        { 
            DirectWrite::PixelGeometry get();
        }

        /// <summary>
        /// Gets the rendering mode.
        /// </summary>
        property DirectWrite::RenderingMode RenderingMode
        { 
            DirectWrite::RenderingMode get();
        }    

    internal:
        RenderingParams() 
        { }
    
        RenderingParams(IDWriteRenderingParams* pNativeIDWriteRenderingParams) : DirectUnknown(pNativeIDWriteRenderingParams)
        { }

    };
} } } }
