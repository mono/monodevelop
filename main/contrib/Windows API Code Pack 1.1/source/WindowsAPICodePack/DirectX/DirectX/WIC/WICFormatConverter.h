//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "WICStructs.h"
#include "WICEnums.h"
#include "WICBitmapSource.h"

using namespace System;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace WindowsImagingComponent {

// REVIEW: the unmanaged IWICFormatConverter interface inherits from
// IWICBitmapSource. Why doesn't the managed FormatConverter inherit from
// BitmapSource? And if it did, couldn't it just use the BitmapSource
// implementations for CopyPixels(), etc.? Having the ToBitmapSource()
// method seems like an awkward way to expose the inheritance.

/// <summary>
/// Exposes methods that represent a pixel format converter. This class provides the means 
///    of converting from one pixel format to another, handling dithering and halftoning 
///    to indexed formats, palette translation and alpha thresholding. 
/// <para>(Also see IWICFormatConverter interface)</para>
/// </summary>
public ref class FormatConverter : public DirectUnknown
{
    public:
    /// <summary>
    /// Returns the BitmapSource of a particular FormatConverter.
    /// </summary>
    BitmapSource^ ToBitmapSource();

    /// <summary>
    /// Initializes the format converter.
    /// </summary>
    /// <param name="source">The input bitmap to convert</param>
    /// <param name="destinationFormat">The destination pixel format.</param>
    /// <param name="ditherType">The BitmapDitherType used for conversion.</param>
    /// <param name="paletteType">The palette translation type to use for conversion.</param>
    void Initialize( 
        BitmapSource^ source,
        Guid destinationFormat,
        BitmapDitherType ditherType,
        BitmapPaletteType paletteType);

    /// <summary>
    /// Retrieves the pixel width and height of the bitmap.
    /// </summary>
    property BitmapSize Size
    {
        BitmapSize get();
    }

    /// <summary>
    /// Retrieves the pixel format the pixels are stored in. 
    /// </summary>
    property Guid PixelFormat
    {
        Guid get();
    }
    
    /// <summary>
    /// Retrieves the sampling rate between pixels and physical world measurements.
    /// </summary>
    property BitmapResolution Resolution
    {
        BitmapResolution get(); 
    }

    /// <summary>
    /// Instructs the object to produce pixels.
    /// </summary>
    /// <returns>An array of the pixel data.</returns>
    cli::array<unsigned char>^ CopyPixels( );

    /// <summary>
    /// Instructs the object to produce pixels.
    /// <para>Application is responsible for freeing the memory when the IntPtr is no longer needed by calling Marshal.FreeHGlobal().</para>
    /// </summary>
    /// <returns>A pointer to the pixel data in memory.</returns>
    IntPtr CopyPixelsToMemory( );

internal:
    FormatConverter()
    { }

    FormatConverter(IWICFormatConverter* formatConverter) : DirectUnknown(formatConverter)
    { }    
};

} } } }
