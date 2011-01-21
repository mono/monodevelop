// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"

#include "WICBitmapDecoder.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::WindowsImagingComponent;

unsigned int BitmapDecoder::FrameCount::get( )
{
    unsigned int frameCount = 0;
    Validate::VerifyResult( 
        CastInterface<IWICBitmapDecoder>()->GetFrameCount( &frameCount ) );
    return frameCount;
}

BitmapFrameDecode^ BitmapDecoder::GetFrame( unsigned int index )
{
    IWICBitmapFrameDecode* pFrameDecode = NULL;
    Validate::VerifyResult( 
        CastInterface<IWICBitmapDecoder>()->GetFrame( index, &pFrameDecode ) );
    return pFrameDecode == NULL ? nullptr : gcnew BitmapFrameDecode(pFrameDecode);
}
