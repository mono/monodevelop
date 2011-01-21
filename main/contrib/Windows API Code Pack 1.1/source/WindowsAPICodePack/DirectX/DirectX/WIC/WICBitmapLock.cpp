// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "WICBitmapLock.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::WindowsImagingComponent;
using namespace System::Runtime::InteropServices;


ImagingBitmapLock::ImagingBitmapLock(IWICBitmapLock* _bitmapLock) : DirectUnknown(_bitmapLock)
{
    UINT width;
    UINT height;

    Validate::VerifyResult(CastInterface<IWICBitmapLock>()->GetSize(&width, &height));
    bitmapSize = BitmapSize(width, height);

    cli::array<unsigned char>^ bytes = gcnew cli::array<unsigned char>(16);
    pin_ptr<unsigned char> guid = &bytes[0];
    
    Validate::VerifyResult(CastInterface<IWICBitmapLock>()->GetPixelFormat((WICPixelFormatGUID *)guid));
        
    pixelFormat = System::Guid(bytes);

	UINT tempStride;

    Validate::VerifyResult(CastInterface<IWICBitmapLock>()->GetStride(&tempStride));

	stride = safe_cast<UInt32>(tempStride);
}

IntPtr ImagingBitmapLock::DataPointer::get(void)
{
	if (!dataPointer.HasValue)
	{
		InitDataPointerAndSize();
	}

	return dataPointer.Value;
}

UInt32 ImagingBitmapLock::DataSize::get(void)
{
	if (!dataSize.HasValue)
	{
		InitDataPointerAndSize();
	}

	return dataSize.Value;
}

// Data pointer and size are initialized lazily, because they aren't available in
// multi-threaded apartment applications (so say the docs...more likely, they aren't
// available from the multi-threaded apartment, but whatever). The other values can
// be initialized on construction, but this allows the application to simply avoid
// trying to retrieve these values when inappropriate, but still make use of the class.

void ImagingBitmapLock::InitDataPointerAndSize(void)
{
    BYTE *data = NULL;
    UINT size;

    Validate::VerifyResult(CastInterface<IWICBitmapLock>()->GetDataPointer(&size, &data));

	dataPointer = Nullable<IntPtr>(IntPtr(data));
    dataSize = Nullable<UInt32>(size);
}

array<unsigned char>^ ImagingBitmapLock::CopyData()
{
    BYTE * data = (BYTE *)DataPointer.ToPointer();
    UINT bufferSize = static_cast<UINT>(DataSize);

    if (bufferSize > 0 && data != NULL)
    {
        array<unsigned char>^ pixels = gcnew array<unsigned char>(bufferSize);
        pin_ptr<unsigned char> pixelsPtr = &pixels[0];
        ::memcpy(pixelsPtr, data, bufferSize);

        return pixels;
    }

    return nullptr;

    
}