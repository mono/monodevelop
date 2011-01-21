// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"

#include "WICFormatConverter.h"
#include "WICPixelFormats.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::WindowsImagingComponent;
using namespace System::Runtime::InteropServices;

BitmapSource^ FormatConverter::ToBitmapSource()
{
    IWICBitmapSource* pBitmapSource = NULL;
    Validate::VerifyResult(
        CastInterface<IUnknown>()->QueryInterface(__uuidof(IWICBitmapSource), (void **)&pBitmapSource));
    return pBitmapSource ? gcnew BitmapSource( pBitmapSource ) : nullptr;
}

void FormatConverter::Initialize( 
        BitmapSource^ source,
        Guid destinationFormat,
        BitmapDitherType ditherType,
        BitmapPaletteType paletteType)
{
    GUID guidDestination;
    Marshal::Copy( destinationFormat.ToByteArray(), 0, IntPtr( &guidDestination ), 16 );

    Validate::VerifyResult( 
        CastInterface<IWICFormatConverter>()->Initialize(
            source->CastInterface<IWICBitmapSource>(),
            guidDestination,
            static_cast<WICBitmapDitherType>(ditherType),
            NULL,
            0.0,
            static_cast<WICBitmapPaletteType>(paletteType)));
}

BitmapSize FormatConverter::Size::get() 
{ 
    UINT width;
    UINT height;

    Validate::VerifyResult( 
        CastInterface<IWICFormatConverter>()->GetSize( &width, & height ) );

    return BitmapSize( width, height );
}

Guid FormatConverter::PixelFormat::get()
{
    cli::array<unsigned char>^ bytes = gcnew cli::array<unsigned char>(16);
    pin_ptr<unsigned char> guid = &bytes[0];
    
    Validate::VerifyResult( 
        CastInterface<IWICFormatConverter>()->GetPixelFormat((WICPixelFormatGUID*) guid ) );
    
    return System::Guid( bytes );
}

BitmapResolution FormatConverter::Resolution::get()
{
    double dpiX;
    double dpiY;
    Validate::VerifyResult( 
        CastInterface<IWICFormatConverter>()->GetResolution( &dpiX, & dpiY ) );
    return BitmapResolution( dpiX, dpiY );
}


cli::array<unsigned char>^ FormatConverter::CopyPixels( )
{
    WICRect rect = {0};
    BitmapSize size = Size;
    rect.Width = size.Width;
    rect.Height = size.Height;

    // Force the stride to be a multiple of sizeof(DWORD)
    UINT stride = rect.Width * 4;
    stride = ((stride + sizeof(DWORD) - 1) / sizeof(DWORD)) * sizeof(DWORD);
    UINT bufferSize = stride * rect.Height;

    cli::array<unsigned char>^ managedBuffer = gcnew cli::array<unsigned char>( bufferSize );
    pin_ptr<unsigned char> buffer = &managedBuffer[0];

    Validate::VerifyResult( 
        CastInterface<IWICFormatConverter>()->CopyPixels(
            &rect,
            stride,
            bufferSize,
            buffer) );


    return managedBuffer;
}


IntPtr FormatConverter::CopyPixelsToMemory( )
{
    WICRect rect = {0};
    BitmapSize size = Size;
    rect.Width = size.Width;
    rect.Height = size.Height;

    // Force the stride to be a multiple of sizeof(DWORD)
    UINT stride = rect.Width * 4;
    stride = ((stride + sizeof(DWORD) - 1) / sizeof(DWORD)) * sizeof(DWORD);
    UINT bufferSize = stride * rect.Height;

    IntPtr buffer = Marshal::AllocHGlobal(bufferSize);

    Validate::VerifyResult( 
        CastInterface<IWICFormatConverter>()->CopyPixels(
            &rect,
            stride,
            bufferSize,
            (BYTE*)buffer.ToPointer()) );


    return buffer;
}