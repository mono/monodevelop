//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "WICBitmapFrameDecode.h"

using namespace System;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace WindowsImagingComponent {

    /// <summary>
    /// Exposes methods that represent a decoder. Provides access to a decoder's frames.
    /// <para>(Also see IWICBitmapDecoder interface)</para>
    /// </summary>
    public ref class BitmapDecoder : public DirectUnknown
    {
    public:
        /// <summary>
        /// The total number of frames in the image.
        /// </summary>
        property unsigned int FrameCount
        {
            unsigned int get();
        }

        /// <summary>
        /// Retrieves the specified frame of the image.
        /// </summary>
        /// <param name="index">The particular frame to retrieve.</param>
        BitmapFrameDecode^ GetFrame( unsigned int index );
    
    internal:
        BitmapDecoder()
        { }

        BitmapDecoder(IWICBitmapDecoder* pNativeIWICBitmapDecoder) : DirectUnknown(pNativeIWICBitmapDecoder)
        { }

    };
} } } }