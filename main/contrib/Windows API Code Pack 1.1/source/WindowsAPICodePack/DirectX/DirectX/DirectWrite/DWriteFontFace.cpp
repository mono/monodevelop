// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DWriteFontFace.h"
#include "DWriteFontFamily.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;

FontFaceType FontFace::FaceType::get()
{
    return static_cast<FontFaceType>(CastInterface<IDWriteFontFace>()->GetType());
}

UINT32 FontFace::Index::get()
{
    return CastInterface<IDWriteFontFace>()->GetIndex();
}

FontSimulations FontFace::Simulations::get()
{
    return static_cast<FontSimulations>(CastInterface<IDWriteFontFace>()->GetSimulations());
}

Boolean FontFace::IsSymbolFont::get()
{
    return CastInterface<IDWriteFontFace>()->IsSymbolFont() != 0;
}

FontMetrics FontFace::Metrics::get()
{
    DWRITE_FONT_METRICS metrics;
    CastInterface<IDWriteFontFace>()->GetMetrics(&metrics);

    return FontMetrics(metrics);
}

UINT16 FontFace::GlyphCount::get()
{
    return CastInterface<IDWriteFontFace>()->GetGlyphCount();
}

array<GlyphMetrics>^ FontFace::GetDesignGlyphMetrics(array<UINT16>^ glyphIndexes, BOOL isSideways)
{
    array<GlyphMetrics>^ glyphMetrics = gcnew array<GlyphMetrics>(glyphIndexes->Length);
    pin_ptr<UINT16> glyphIndexesPtr = &glyphIndexes[0];

    pin_ptr<GlyphMetrics> glyphMetricsPtr = &glyphMetrics[0];
    
    Validate::VerifyResult(CastInterface<IDWriteFontFace>()->GetDesignGlyphMetrics(glyphIndexesPtr, glyphIndexes->Length, (DWRITE_GLYPH_METRICS*)glyphMetricsPtr, isSideways ? 1 : 0));

    return glyphMetrics;
}

array<GlyphMetrics>^ FontFace::GetDesignGlyphMetrics(array<UINT16>^ glyphIndexes)
{
    return GetDesignGlyphMetrics(glyphIndexes, false);
}

array<UINT16>^ FontFace::GetGlyphIndexes(array<UINT32>^ codePoints)
{
    array<UINT16>^ glyphIndexes = gcnew array<UINT16>(codePoints->Length);
    pin_ptr<UINT16> glyphIndexesPtr = &glyphIndexes[0];

    pin_ptr<UINT32> codePointsPtr = &codePoints[0];    
    Validate::VerifyResult(CastInterface<IDWriteFontFace>()->GetGlyphIndicesW(codePointsPtr, codePoints->Length, glyphIndexesPtr));

    return glyphIndexes;
}

array<UINT16>^ FontFace::GetGlyphIndexes(array<System::Char>^ characterArray)
{
    array<UINT16>^ glyphIndexes = gcnew array<UINT16>(characterArray->Length);
    pin_ptr<UINT16> glyphIndexesPtr = &glyphIndexes[0];

    array<UINT32>^ codePoints = gcnew array<UINT32>(characterArray->Length);
    for (int i = 0; i < characterArray->Length; i++)
    {
        codePoints[i] = System::Convert::ToUInt32(characterArray[i]);
    }

    pin_ptr<UINT32> codePointsPtr = &codePoints[0];    
    Validate::VerifyResult(CastInterface<IDWriteFontFace>()->GetGlyphIndicesW(codePointsPtr, codePoints->Length, glyphIndexesPtr));

    return glyphIndexes;
}

array<UINT16>^ FontFace::GetGlyphIndexes(System::String^ text)
{
    array<UINT16>^ glyphIndexes = gcnew array<UINT16>(text->Length);
    pin_ptr<UINT16> glyphIndexesPtr = &glyphIndexes[0];

    array<UINT32>^ codePoints = gcnew array<UINT32>(text->Length);
    for (int i = 0; i < text->Length; i++)
    {
        codePoints[i] = System::Convert::ToUInt32(text[i]);
    }

    pin_ptr<UINT32> codePointsPtr = &codePoints[0];    
    Validate::VerifyResult(CastInterface<IDWriteFontFace>()->GetGlyphIndicesW(codePointsPtr, codePoints->Length, glyphIndexesPtr));

    return glyphIndexes;
}