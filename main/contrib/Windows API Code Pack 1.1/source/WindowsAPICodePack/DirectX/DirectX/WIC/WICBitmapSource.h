//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "WICStructs.h"
#include "WICEnums.h"

using namespace System;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace WindowsImagingComponent {

/// <summary>
/// Exposes methods that refers to a source from which pixels are retrieved, but cannot be written back to. 
/// <para>(Also see IWICBitmapSource interface)</para>
/// </summary>
public ref class BitmapSource : public DirectUnknown
{
public:
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
    /// <param name="rectangle">The rectangle to copy. A null value specifies the entire bitmap.</param>
    /// <returns>An array of the pixel data.</returns>
    cli::array<unsigned char>^ CopyPixels( BitmapRectangle rectangle );

    /// <summary>
    /// Instructs the object to produce pixels using the entire bitmap.
    /// </summary>
    /// <returns>An array of the pixel data.</returns>
    cli::array<unsigned char>^ CopyPixels();

    /// <summary>
    /// Instructs the object to produce pixels.
    /// <para>Application is responsible for freeing the memory when the IntPtr is no longer needed by using Marshal.FreeHGlobal()</para>
    /// </summary>
    /// <param name="rectangle">The rectangle to copy. A null value specifies the entire bitmap.</param>
    /// <returns>A pointer to pixel data in memory.</returns>
    IntPtr CopyPixelsToMemory( BitmapRectangle rectangle );

    /// <summary>
    /// Instructs the object to produce pixels using the entire bitmap.
    /// <para>Application is responsible for freeing the memory when the IntPtr is no longer needed by using Marshal.FreeHGlobal()</para>
    /// </summary>
    /// <returns>A pointer to pixel data in memory.</returns>
    IntPtr CopyPixelsToMemory();

internal:
    BitmapSource()
    { } 

    BitmapSource(IWICBitmapSource* bitmapSource) : DirectUnknown(bitmapSource)
    { }    
};

} } } }
