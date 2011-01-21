// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DXGISurface1.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

IntPtr Surface1::GetDC(Boolean discard)
{
    HDC tempHdc;
    Validate::VerifyResult(CastInterface<IDXGISurface1>()->GetDC(safe_cast<BOOL>(discard), &tempHdc));
    
    return IntPtr(tempHdc);
}

void Surface1::ReleaseDC(D3DRect dirtyRect)
{
    RECT rect = (RECT)dirtyRect;

    Validate::VerifyResult(CastInterface<IDXGISurface1>()->ReleaseDC(&rect));
}

