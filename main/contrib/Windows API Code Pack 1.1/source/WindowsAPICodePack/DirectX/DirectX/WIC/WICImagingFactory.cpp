// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"

#include "WICImagingFactory.h"
#include "WICStreamWrapper.h"
#include "WICEnums.h"
#include "WICBitmap.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::WindowsImagingComponent;

extern "C"
{
    const GUID IID_IWICImagingFactory = {0xec5ec8a9,0xc395,0x4314,0x9c,0x77,0x54,0xd7,0xa9,0x35,0xff,0x70};
    const GUID CLSID_WICImagingFactory = {0xcacaf262, 0x9370, 0x4615, 0xa1, 0x3b, 0x9f, 0x55, 0x39, 0xda, 0x4c, 0xa};
}

ImagingFactory^ ImagingFactory::Create(void)
{
    IWICImagingFactory* pNativeFactory = NULL;

	Validate::VerifyResult(CoCreateInstance(
        CLSID_WICImagingFactory,
        NULL,
        CLSCTX_INPROC_SERVER,
        IID_IWICImagingFactory,
        (LPVOID*)&pNativeFactory ));

	return gcnew ImagingFactory(pNativeFactory);
}

BitmapDecoder^ ImagingFactory::CreateDecoderFromFileName( 
            String^ path, 
            DesiredAccess access, 
            DecodeMetadataCacheOption options )
{
    if (String::IsNullOrEmpty(path))
    {
        throw gcnew ArgumentNullException(path, "The path cannot be null or empty.");
    }

    marshal_context^ ctx = gcnew marshal_context();
    IWICBitmapDecoder* pBitmapDecoder = NULL;

    Validate::VerifyResult( 
        CastInterface<IWICImagingFactory>()->CreateDecoderFromFilename(
            ctx->marshal_as<const WCHAR*>(path),
            NULL,
            static_cast<DWORD>(access),
            static_cast<WICDecodeOptions>(options),
            &pBitmapDecoder) );

    return (pBitmapDecoder == NULL) ? nullptr : gcnew BitmapDecoder( pBitmapDecoder );
}

BitmapDecoder^ ImagingFactory::CreateDecoderFromStream( 
    System::IO::Stream^ stream,
    DecodeMetadataCacheOption options )
{
    if (stream == nullptr)
    {
        throw gcnew ArgumentNullException("stream", "The input stream cannot be null.");
    }

    IWICBitmapDecoder* pBitmapDecoder = NULL;
    
    StreamWrapper^ wrapper = gcnew StreamWrapper( stream );
    
    IntPtr pstream = Marshal::GetComInterfaceForObject( wrapper, System::Runtime::InteropServices::ComTypes::IStream::typeid );

    Validate::VerifyResult(
        CastInterface<IWICImagingFactory>()->CreateDecoderFromStream( 
        (::IStream*)pstream.ToPointer(),
        NULL,
        static_cast<WICDecodeOptions>(options),
        &pBitmapDecoder) );

    return (pBitmapDecoder == NULL) ? nullptr : gcnew BitmapDecoder( pBitmapDecoder );
}

FormatConverter^ ImagingFactory::CreateFormatConverter()
{
    IWICFormatConverter* pFormatConverter = NULL;
    Validate::VerifyResult( 
        CastInterface<IWICImagingFactory>()->CreateFormatConverter(
            &pFormatConverter ) );

    return (pFormatConverter == NULL) ? nullptr : gcnew FormatConverter( pFormatConverter );
}

ImagingBitmap^ ImagingFactory::CreateImagingBitmap(unsigned int width, unsigned int height, Guid pixelFormat, BitmapCreateCacheOption cacheOption )
{
    IWICBitmap *pIBitmap = NULL;
    cli::array<unsigned char>^ pixelFormatBytes = pixelFormat.ToByteArray();
    pin_ptr<unsigned char> pixelFormatGuidPtr = &pixelFormatBytes[0];

    Validate::VerifyResult(CastInterface<IWICImagingFactory>()->CreateBitmap(
        width, height, 
        (REFWICPixelFormatGUID)(*pixelFormatGuidPtr), 
        static_cast<WICBitmapCreateCacheOption>(cacheOption), 
        &pIBitmap));

    return pIBitmap  ? gcnew ImagingBitmap(pIBitmap) : nullptr;
}