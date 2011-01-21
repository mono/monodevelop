// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DWriteStructs.h"
#include "DWriteFontFace.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::DirectWrite;

void GlyphRun::CopyTo(DWRITE_GLYPH_RUN *pNativeStruct)
{
    if (glyphAdvances->Length != glyphIndexes->Length || glyphIndexes->Length != glyphOffsets->Length)
    {
        // REVIEW: seems like this is more of an InvalidOperation or Argument exception. To me,
        // "NotSupported" implies there could be a valid way to support what's "not supported".
        throw gcnew NotSupportedException("GlyphAdvances, GlyphIndexes and GlyphOffsets arrays must all have equal lengths.");
    }

    pNativeStruct->bidiLevel = BidiLevel;
    pNativeStruct->isSideways = IsSideways ? 1 : 0;
    pNativeStruct->fontEmSize = FontEmSize;
    pNativeStruct->fontFace = this->FontFace->CastInterface<IDWriteFontFace>();
    pNativeStruct->glyphCount = glyphAdvances->Length;

    if (pNativeStruct->glyphCount > 0)
    {    
        pin_ptr<FLOAT> pGlyphAdvances = &glyphAdvances[0];
        pNativeStruct->glyphAdvances = new FLOAT[pNativeStruct->glyphCount];
        memcpy((void*)pNativeStruct->glyphAdvances, pGlyphAdvances, sizeof(FLOAT) * pNativeStruct->glyphCount);

        pin_ptr<UINT16> pGlyphIndexes = &glyphIndexes[0];
        pNativeStruct->glyphIndices = new UINT16[pNativeStruct->glyphCount];
        memcpy((void*)pNativeStruct->glyphIndices, pGlyphIndexes, sizeof(UINT16) * pNativeStruct->glyphCount);

        pin_ptr<GlyphOffset> pGlyphOffsets = &glyphOffsets[0];
        pNativeStruct->glyphOffsets = new DWRITE_GLYPH_OFFSET[pNativeStruct->glyphCount];
        memcpy((void*)pNativeStruct->glyphOffsets, pGlyphOffsets, sizeof(DWRITE_GLYPH_OFFSET) * pNativeStruct->glyphCount);
    }

}
