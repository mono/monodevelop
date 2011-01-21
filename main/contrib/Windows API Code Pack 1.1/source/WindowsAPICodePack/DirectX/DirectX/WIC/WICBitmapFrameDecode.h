//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include <wincodec.h>

#include "WICBitmapSource.h"

using namespace System;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace WindowsImagingComponent {

    // REVIEW: IWICBitmapFrameDecode inherits IWICBitmapSource. See FormatConverter
    // for more comments.

    /// <summary>
    /// Defines methods for decoding individual image frames of an encoded file.
    /// <para>(Also see IWICBitmapFrameDecode interface)</para>
    /// </summary>
    public ref class BitmapFrameDecode : public DirectUnknown
    {
    public:
        /// <summary>
        /// Returns the BitmapSource of a particular BitmapFrameDecode.
        /// </summary>
        BitmapSource^ ToBitmapSource();

    internal:
        BitmapFrameDecode() 
        { }

        BitmapFrameDecode( IWICBitmapFrameDecode* nativeIWICBitmapFrameDecode) : DirectUnknown(nativeIWICBitmapFrameDecode)
        { }
    };
} } } }