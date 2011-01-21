// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "WICBitmap.h"
#include "WICBitmapLock.h"
#include "WICImagingfactory.h"

extern "C"
{
    const GUID GUID_WICPixelFormatDontCare = {0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x00};
}

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::WindowsImagingComponent;
using namespace System::Runtime::InteropServices;


void ImagingBitmap::Resolution::set(BitmapResolution resolution)
{
    Validate::VerifyResult( 
        CastInterface<IWICBitmap>()->SetResolution(resolution.DpiX, resolution.DpiY) );
}

ImagingBitmapLock^ ImagingBitmap::Lock(BitmapRectangle lockRectangle, BitmapLockOptions options)
{
    pin_ptr<BitmapRectangle> rect = &lockRectangle;
    IWICBitmapLock * lock = NULL;

    Validate::VerifyResult( 
        CastInterface<IWICBitmap>()->Lock((WICRect*) rect, static_cast<DWORD>(options), &lock));

    return lock ? gcnew ImagingBitmapLock(lock) : nullptr;
}

void ImagingBitmap::SaveToFile(ImagingFactory^ imagingFactory, System::Guid containerFormat, System::String^ fileName)
{
    cli::array<unsigned char>^ containerFormatBytes = containerFormat.ToByteArray();
    pin_ptr<unsigned char> containerFormatGuidPtr = &containerFormatBytes[0];

    IWICBitmapEncoder *pEncoder = NULL;
    IWICBitmapFrameEncode *pFrameEncode = NULL;
    IWICStream *pStream = NULL;
    
    IWICImagingFactory* pWICFactory = imagingFactory->CastInterface<IWICImagingFactory>();
    IWICBitmap* bitmap = CastInterface<IWICBitmap>();
    
    try
    {
        unsigned int sc_bitmapWidth, sc_bitmapHeight;
        bitmap->GetSize(&sc_bitmapWidth, &sc_bitmapHeight);

		Validate::VerifyResult(pWICFactory->CreateStream(&pStream));

        WICPixelFormatGUID format = GUID_WICPixelFormatDontCare;
        pin_ptr<const WCHAR> filename = PtrToStringChars(fileName);
        
        Validate::VerifyResult(pStream->InitializeFromFilename(filename, GENERIC_WRITE));

        GUID guid =  *((GUID*)containerFormatGuidPtr);
        Validate::VerifyResult(pWICFactory->CreateEncoder(guid, NULL, &pEncoder));

        Validate::VerifyResult(pEncoder->Initialize(pStream, WICBitmapEncoderNoCache));

        Validate::VerifyResult(pEncoder->CreateNewFrame(&pFrameEncode, NULL));

        Validate::VerifyResult(pFrameEncode->Initialize(NULL));

        Validate::VerifyResult(pFrameEncode->SetSize(sc_bitmapWidth, sc_bitmapHeight));

        Validate::VerifyResult(pFrameEncode->SetPixelFormat(&format));

        Validate::VerifyResult(pFrameEncode->WriteSource(bitmap, NULL));

        Validate::VerifyResult(pFrameEncode->Commit());

        Validate::VerifyResult(pEncoder->Commit());
    }
    finally
    {
        if (pEncoder) 
            pEncoder->Release();

        if (pFrameEncode)
            pFrameEncode->Release();

        if (pStream)
            pStream->Release();
    }
}