// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"

#include "WICBitmapSource.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::WindowsImagingComponent;
using namespace System::Runtime::InteropServices;

BitmapSize BitmapSource::Size::get() 
{ 
    UINT width;
    UINT height;
    Validate::VerifyResult( 
        CastInterface<IWICBitmapSource>()->GetSize( &width, & height ) );

    return BitmapSize( width, height );
}

Guid BitmapSource::PixelFormat::get()
{
    cli::array<unsigned char>^ bytes = gcnew cli::array<unsigned char>(16);
    pin_ptr<unsigned char> guid = &bytes[0];
    
    Validate::VerifyResult( 
        CastInterface<IWICBitmapSource>()->GetPixelFormat((WICPixelFormatGUID*) guid ) );
        
    return System::Guid( bytes );
}

BitmapResolution BitmapSource::Resolution::get()
{
    double dpiX;
    double dpiY;
    Validate::VerifyResult( 
        CastInterface<IWICBitmapSource>()->GetResolution( &dpiX, & dpiY ) );
    return BitmapResolution( dpiX, dpiY );
}

cli::array<unsigned char>^ BitmapSource::CopyPixels( BitmapRectangle rectangle )
{
    WICRect rect;

    rect.X = rectangle.X;
    rect.Y = rectangle.Y;
    rect.Width = rectangle.Width;
    rect.Height = rectangle.Height;

    // REVIEW: This assumes 32 bpp for the image. Should the code take the actual
    // pixel format into account?

    // Init stride assuming 4 bytes per pixel; stride should be DWORD aligned, but
    // since sizeof(DWORD) == 4, no additional calculation is necessary here. If this
    // method is changed to allow for other pixel formats, that will change.
    UINT stride = rect.Width * 4;
    UINT bufferSize = stride * rect.Height;
    
    cli::array<unsigned char>^ managedBuffer = gcnew cli::array<unsigned char>( bufferSize );
    pin_ptr<unsigned char> buffer = &managedBuffer[0];

    Validate::VerifyResult( 
        CastInterface<IWICBitmapSource>()->CopyPixels(
            &rect,
            stride,
            bufferSize,
            buffer) );

    return managedBuffer;
}

cli::array<unsigned char>^ BitmapSource::CopyPixels()
{
    BitmapSize size = Size;
    WICRect rect = {0};
    rect.X = 0;
    rect.Y = 0;
    rect.Width = size.Width;
    rect.Height = size.Height;

    // Force the stride to be a multiple of sizeof(DWORD)
    UINT stride = rect.Height * 4;
    stride = ((stride + sizeof(DWORD) - 1) / sizeof(DWORD)) * sizeof(DWORD);
    UINT bufferSize = stride * rect.Height;
    
    cli::array<unsigned char>^ managedBuffer = gcnew cli::array<unsigned char>( bufferSize );
    pin_ptr<unsigned char> buffer = &managedBuffer[0];

    Validate::VerifyResult( 
        CastInterface<IWICBitmapSource>()->CopyPixels(
            &rect,
            stride,
            bufferSize,
            buffer) );

    return managedBuffer;
}


IntPtr BitmapSource::CopyPixelsToMemory( BitmapRectangle rectangle )
{
    WICRect rect;
    rect.X = rectangle.X;
    rect.Y = rectangle.Y;
    rect.Width = rectangle.Width;
    rect.Height = rectangle.Height;

    // Force the stride to be a multiple of sizeof(DWORD)
    UINT stride = rect.Height * 4;
    stride = ((stride + sizeof(DWORD) - 1) / sizeof(DWORD)) * sizeof(DWORD);
    UINT bufferSize = stride * rect.Height;

    IntPtr buffer = Marshal::AllocHGlobal(bufferSize);

    Validate::VerifyResult( 
        CastInterface<IWICBitmapSource>()->CopyPixels(
            &rect,
            stride,
            bufferSize,
            (BYTE*)buffer.ToPointer()) );

    return buffer;
}

IntPtr BitmapSource::CopyPixelsToMemory()
{
    BitmapSize size = Size;
    WICRect rect = {0};
    rect.X = 0;
    rect.Y = 0;
    rect.Width = size.Width;
    rect.Height = size.Height;

    // Force the stride to be a multiple of sizeof(DWORD)
    UINT stride = rect.Height * 4;
    stride = ((stride + sizeof(DWORD) - 1) / sizeof(DWORD)) * sizeof(DWORD);
    UINT bufferSize = stride * rect.Height;

    IntPtr buffer = Marshal::AllocHGlobal(bufferSize);

    Validate::VerifyResult( 
        CastInterface<IWICBitmapSource>()->CopyPixels(
            &rect,
            stride,
            bufferSize,
            (BYTE*)buffer.ToPointer()) );

    return buffer;

}

