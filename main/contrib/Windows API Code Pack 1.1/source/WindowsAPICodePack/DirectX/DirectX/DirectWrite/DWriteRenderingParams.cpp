// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DWriteRenderingParams.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::DirectWrite;

FLOAT RenderingParams::Gamma::get()
{ 
    return CastInterface<IDWriteRenderingParams>()->GetGamma();
}

FLOAT RenderingParams::EnhancedContrast::get()
{ 
    return CastInterface<IDWriteRenderingParams>()->GetEnhancedContrast();
}

FLOAT RenderingParams::ClearTypeLevel::get()
{ 
    return CastInterface<IDWriteRenderingParams>()->GetClearTypeLevel();
}

DirectWrite::PixelGeometry RenderingParams::PixelGeometry::get()
{ 
    return static_cast<DirectWrite::PixelGeometry>(CastInterface<IDWriteRenderingParams>()->GetPixelGeometry());
}

DirectWrite::RenderingMode RenderingParams::RenderingMode::get()
{ 
    return static_cast<DirectWrite::RenderingMode>(CastInterface<IDWriteRenderingParams>()->GetRenderingMode());
} 
