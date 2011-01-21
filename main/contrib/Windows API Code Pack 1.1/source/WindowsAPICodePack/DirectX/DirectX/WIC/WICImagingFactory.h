//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "WICBitmapDecoder.h"
#include "WICFormatConverter.h"
#include "WICEnums.h"
#include "WICPixelFormats.h"

using namespace System;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace WindowsImagingComponent {

    ref class ImagingBitmap;

    /// <summary>
    /// Exposes methods used to create components for the Microsoft Windows Imaging 
    /// Component (WIC) such as decoders, encoders and pixel format converters.
    /// <para>(Also see IWICImagingFactory interface)</para>
    /// </summary>
    public ref class ImagingFactory : public DirectUnknown
    {
    public:
        /// <summary>
        /// Creates a new instance of the WICBitmapDecoder class based on the given file.
        /// </summary>
        /// <param name="path">The path to the file to create or open.</param>
        /// <param name="access">The access to the file object, which can be read, write, or both. </param>
        /// <param name="options">The DecodeMetadataCacheOption to use when creating the decoder.</param>
        /// <returns>A new Instance of the WICBitmapDecoder class.</returns>
        BitmapDecoder^ CreateDecoderFromFileName( 
            String^ path, 
            DesiredAccess access, 
            DecodeMetadataCacheOption options );

        /// <summary>
        /// Creates a new instance of the WICBitmapDecoder class based on the given data stream.
        /// </summary>
        /// <param name="stream">A stream containing the image data to read.</param>
        /// <param name="options">The <see cref="DecodeMetadataCacheOption"/> to use when creating the decoder.</param>
        /// <returns>A new Instance of the <see cref="BitmapDecoder"/> class.</returns>
        BitmapDecoder^ CreateDecoderFromStream( 
            System::IO::Stream^ stream,
            DecodeMetadataCacheOption options );

        /// <summary>
        /// Creates a new instance of the <see cref="FormatConverter"/> class.
        /// </summary>
        /// <returns>A new Instance of the <see cref="FormatConverter"/> class.</returns>
        FormatConverter^ CreateFormatConverter();

        /// <summary>
        /// Creates an empty <see cref="ImagingBitmap"/>.
        /// </summary>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <param name="pixelFormat">One of the predefined pixel formats in <see cref="PixelFormats"/> to use when creating the bitmap.</param>
        /// <param name="cacheOption">The bitmap cache creation option.</param>
        /// <returns>A new Instance of <see cref="ImagingBitmap"/>.</returns>
        ImagingBitmap^ CreateImagingBitmap(unsigned int width, unsigned int height, Guid pixelFormat, BitmapCreateCacheOption cacheOption );

        /// <summary>
        /// Creates a new ImagingFactory
        /// </summary>
		static ImagingFactory^ Create(void);

    internal:

        ImagingFactory(IWICImagingFactory *factory) : DirectUnknown(factory)
        { }    
    };
} } } }
