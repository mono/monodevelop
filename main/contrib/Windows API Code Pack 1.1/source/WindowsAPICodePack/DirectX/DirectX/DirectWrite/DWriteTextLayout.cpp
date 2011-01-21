// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DWriteTextLayout.h"
#include "ICustomInlineObject.h"
#include "DWriteInlineObject.h"
#include "DWriteTypography.h"
#include <vector>

using namespace std;

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;

#ifndef E_INSUFFICIENT_BUFFER
#define E_INSUFFICIENT_BUFFER 0x8007007A
#endif

String^ TextLayout::Text::get()
{
    return _text;
}

FLOAT TextLayout::MaxWidth::get()
{
    return CastInterface<IDWriteTextLayout>()->GetMaxWidth();
}

void TextLayout::MaxWidth::set(FLOAT value)
{
    Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->SetMaxWidth(value));
}

FLOAT TextLayout::MaxHeight::get()
{
    return CastInterface<IDWriteTextLayout>()->GetMaxHeight();
}

void TextLayout::MaxHeight::set(FLOAT value)
{
    Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->SetMaxHeight(value));
}


void TextLayout::SetFontFamilyName(
    System::String^  fontFamilyName,
    TextRange textRange
    )
{
    pin_ptr<const wchar_t> name = PtrToStringChars(fontFamilyName);
    
    DWRITE_TEXT_RANGE tempRange;
    textRange.CopyTo(&tempRange);
    
    Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->SetFontFamilyName(name, tempRange));
}

void TextLayout::SetFontWeight(
    DirectWrite::FontWeight fontWeight,
    TextRange textRange
    )
{
    DWRITE_TEXT_RANGE tempRange;
    textRange.CopyTo(&tempRange);

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->SetFontWeight(
            static_cast<DWRITE_FONT_WEIGHT>(fontWeight), tempRange));
}

void TextLayout::SetFontStyle(
    DirectWrite::FontStyle fontStyle,
    TextRange textRange
    )
{
    DWRITE_TEXT_RANGE tempRange;
    textRange.CopyTo(&tempRange);

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->SetFontStyle(
            static_cast<DWRITE_FONT_STYLE>(fontStyle), tempRange));
}

void TextLayout::SetFontStretch(
    DirectWrite::FontStretch fontStretch,
    TextRange textRange
    )
{
    DWRITE_TEXT_RANGE tempRange;
    textRange.CopyTo(&tempRange);

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->SetFontStretch(
        static_cast<DWRITE_FONT_STRETCH>(fontStretch), tempRange));
}

void TextLayout::SetFontSize(
    FLOAT fontSize,
    TextRange textRange
    )
{
    DWRITE_TEXT_RANGE tempRange;
    textRange.CopyTo(&tempRange);

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->SetFontSize(
        fontSize, tempRange));
}

void TextLayout::SetUnderline(
    Boolean hasUnderline,
    TextRange textRange
    )
{
    DWRITE_TEXT_RANGE tempRange;
    textRange.CopyTo(&tempRange);

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->SetUnderline(
        hasUnderline ? TRUE : FALSE, tempRange));
}

void TextLayout::SetStrikethrough(
    Boolean hasStrikethrough,
    TextRange textRange
    )
{
    DWRITE_TEXT_RANGE tempRange;
    textRange.CopyTo(&tempRange);

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->SetStrikethrough(
        hasStrikethrough ? TRUE : FALSE, tempRange));
}


void TextLayout::SetInlineObject(
    ICustomInlineObject^ inlineObject,
    TextRange textRange
    )
{
    AutoInlineObject nativeInline(inlineObject);

    DWRITE_TEXT_RANGE tempRange;
    textRange.CopyTo(&tempRange);

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->SetInlineObject(nativeInline.GetNativeInline(), tempRange));
}

void TextLayout::SetTypography(
    TypographySettingCollection^ typography,
    TextRange textRange
    )
{
    DWRITE_TEXT_RANGE tempRange;
    textRange.CopyTo(&tempRange);

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->SetTypography(typography->CastInterface<::IDWriteTypography>(), tempRange
        ));
}


void TextLayout::SetCultureInfo(        
    Globalization::CultureInfo^ cultureInfo,
    TextRange textRange
    )
{
    pin_ptr<const wchar_t> name = PtrToStringChars(cultureInfo->Name);
    
    DWRITE_TEXT_RANGE tempRange;
    textRange.CopyTo(&tempRange);
    
    Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->SetLocaleName(name, tempRange));
}

TextRangeOf<System::String^> TextLayout::GetFontFamilyNameForRange(UINT32 currentPosition)
{
    // First, get the length
    UINT32 len;
    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetFontFamilyNameLength(currentPosition, &len));

    wchar_t* name = new wchar_t[len + 1];

    try
    {
        TextRange range;
        DWRITE_TEXT_RANGE tempRange;

        Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->GetFontFamilyName(currentPosition, name, len, &tempRange));

        range.CopyFrom(tempRange);

        return TextRangeOf<String^>(range, gcnew String(name));
    }
    finally
    {
        delete [] name;
    }
}

System::String^ TextLayout::GetFontFamilyName(
    UINT32 currentPosition
    )
{
    // First, get the length
    UINT32 len;
    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetFontFamilyNameLength(currentPosition, &len));

    wchar_t* name = new wchar_t[len + 1];

    try
    {        
        Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->GetFontFamilyName(currentPosition, name, len));
        return gcnew String(name);
    }
    finally
    {
        delete [] name;
    }}

TextRangeOf<DirectWrite::FontWeight> TextLayout::GetFontWeightForRange(UINT32 currentPosition)
{
    TextRange range;
    DWRITE_TEXT_RANGE tempRange;
    DWRITE_FONT_WEIGHT returnValue;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetFontWeight(
        currentPosition, &returnValue, &tempRange
        ));

    range.CopyFrom(tempRange);

    return TextRangeOf<DirectWrite::FontWeight>(range, static_cast<DirectWrite::FontWeight>(returnValue));
}

DirectWrite::FontWeight TextLayout::GetFontWeight(
    UINT32 currentPosition
    )
{
    DWRITE_FONT_WEIGHT returnValue;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetFontWeight(
        currentPosition, &returnValue
        ));

    return static_cast<DirectWrite::FontWeight>(returnValue);
}

TextRangeOf<DirectWrite::FontStyle> TextLayout::GetFontStyleForRange(UINT32 currentPosition)
{
    TextRange range;
    DWRITE_TEXT_RANGE tempRange;
    DWRITE_FONT_STYLE returnValue;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetFontStyle(
        currentPosition, &returnValue, &tempRange
        ));

    range.CopyFrom(tempRange);

    return TextRangeOf<DirectWrite::FontStyle>(range, static_cast<DirectWrite::FontStyle>(returnValue));
}

DirectWrite::FontStyle TextLayout::GetFontStyle(
    UINT32 currentPosition
    )
{
    DWRITE_FONT_STYLE returnValue;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetFontStyle(
        currentPosition, &returnValue
        ));

    return static_cast<DirectWrite::FontStyle>(returnValue);
}

TextRangeOf<DirectWrite::FontStretch> TextLayout::GetFontStretchForRange(UINT32 currentPosition)
{
    TextRange range;
    DWRITE_TEXT_RANGE tempRange;
    DWRITE_FONT_STRETCH returnValue;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetFontStretch(
        currentPosition, &returnValue, &tempRange
        ));

    range.CopyFrom(tempRange);

    return TextRangeOf<DirectWrite::FontStretch>(range, static_cast<DirectWrite::FontStretch>(returnValue));
}

FontStretch TextLayout::GetFontStretch(
    UINT32 currentPosition
    )
{
    DWRITE_FONT_STRETCH returnValue;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetFontStretch(
        currentPosition, &returnValue
        ));

    return static_cast<DirectWrite::FontStretch>(returnValue);
}

TextRangeOf<Single> TextLayout::GetFontSizeForRange(UINT32 currentPosition)
{
    TextRange range;
    DWRITE_TEXT_RANGE tempRange;
    FLOAT returnValue;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetFontSize(
        currentPosition, &returnValue, &tempRange
        ));

    range.CopyFrom(tempRange);

    return TextRangeOf<Single>(range, safe_cast<Single>(returnValue));
}

FLOAT TextLayout::GetFontSize(
    UINT32 currentPosition
    )
{
    FLOAT returnValue;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetFontSize(
        currentPosition, &returnValue
        ));

    return returnValue;
}

TextRangeOf<Boolean> TextLayout::GetUnderlineForRange(UINT32 currentPosition)
{
    TextRange range;
    DWRITE_TEXT_RANGE tempRange;
    BOOL returnValue;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetUnderline(
        currentPosition, &returnValue, &tempRange
        ));

    range.CopyFrom(tempRange);

    return TextRangeOf<Boolean>(range, returnValue != FALSE);
}


Boolean TextLayout::GetUnderline(
    UINT32 currentPosition
    )
{
    BOOL returnValue;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetUnderline(
        currentPosition, &returnValue
        ));

    return returnValue != 0;
}

TextRangeOf<Boolean> TextLayout::GetStrikethroughForRange(UINT32 currentPosition)
{
    TextRange range;
    DWRITE_TEXT_RANGE tempRange;
    BOOL returnValue;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetStrikethrough(
        currentPosition, &returnValue, &tempRange
        ));

    range.CopyFrom(tempRange);

    return TextRangeOf<Boolean>(range, returnValue != FALSE);
}

Boolean TextLayout::GetStrikethrough(
    UINT32 currentPosition
    )
{
    BOOL returnValue;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetStrikethrough(
        currentPosition, &returnValue
        ));

    return returnValue != 0;
}

TextRangeOf<Globalization::CultureInfo^> TextLayout::GetCultureInfoForRange(UINT32 currentPosition)
{
    // First, get the length
    UINT32 len;
    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetLocaleNameLength(currentPosition, &len));

    wchar_t* name = new wchar_t[len + 1];

    try
    {        
        DWRITE_TEXT_RANGE tempRange;
        TextRange range;

        Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->GetLocaleName(currentPosition, name, len, &tempRange));

        range.CopyFrom(tempRange);

        return TextRangeOf<Globalization::CultureInfo^>(range, gcnew Globalization::CultureInfo(gcnew String(name)));
    }
    finally
    {
        delete [] name;
    }
}

Globalization::CultureInfo^ TextLayout::GetCultureInfo(
    UINT32 currentPosition
    )
{
    // First, get the length
    UINT32 len;
    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->GetLocaleNameLength(currentPosition, &len));

    wchar_t* name = new wchar_t[len + 1];

    try
    {        
        Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->GetLocaleName(currentPosition, name, len));
        return gcnew Globalization::CultureInfo(gcnew String(name));
    }
    finally
    {
        delete [] name;
    }
}


TextRangeOf<TypographySettingCollection^> TextLayout::GetTypographyForRange(UINT32 currentPosition)
{
    TextRange range;
    DWRITE_TEXT_RANGE tempRange;
    IDWriteTypography * typography = NULL;

    Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->GetTypography(currentPosition, &typography, &tempRange));

    range.CopyFrom(tempRange);

    return TextRangeOf<TypographySettingCollection^>(range,
        typography != NULL ? gcnew TypographySettingCollection(typography) : nullptr);
}

TypographySettingCollection^ TextLayout::GetTypography(
    UINT32 currentPosition
    )
{
    IDWriteTypography * typography = NULL;
    Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->GetTypography(currentPosition, &typography));

    return typography != NULL ? gcnew TypographySettingCollection(typography) : nullptr;
}

IEnumerable<DirectWrite::LineMetrics>^ TextLayout::LineMetrics::get(void)
{
    UINT32 actualLineCount;

    HRESULT hr = 
        CastInterface<IDWriteTextLayout>()->GetLineMetrics(NULL, 0, &actualLineCount);

    if (hr != E_INSUFFICIENT_BUFFER)
        Validate::VerifyResult(hr);

    if (actualLineCount > 0)
    {
        array<DirectWrite::LineMetrics>^ metricsArray = gcnew array<DirectWrite::LineMetrics>(actualLineCount);
        pin_ptr<DirectWrite::LineMetrics> metricsPtr = &metricsArray[0];

        Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->GetLineMetrics(
            (DWRITE_LINE_METRICS*)metricsPtr, actualLineCount, &actualLineCount));

        return Array::AsReadOnly(metricsArray);
    }

    // If no line metrics return nullptr
    return nullptr;
}


TextMetrics TextLayout::Metrics::get()
{
    DWRITE_TEXT_METRICS tempMetrics;
    Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->GetMetrics(&tempMetrics));

    TextMetrics returnValue;
    returnValue.CopyFrom(tempMetrics);
    return returnValue;
}

DirectWrite::OverhangMetrics TextLayout::OverhangMetrics::get()
{
    DWRITE_OVERHANG_METRICS tempMetrics;
    Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->GetOverhangMetrics(&tempMetrics));

    DirectWrite::OverhangMetrics returnValue;
    returnValue.CopyFrom(tempMetrics);
    return returnValue;
}


IEnumerable<DirectWrite::ClusterMetrics>^ TextLayout::ClusterMetrics::get(void)
{
    UINT32 actualClusterCount;

    HRESULT hr = CastInterface<IDWriteTextLayout>()->GetClusterMetrics(NULL, 0, &actualClusterCount);

    if (hr != E_INSUFFICIENT_BUFFER)
        Validate::VerifyResult(hr);

    if (actualClusterCount > 0)
    {

        vector<DWRITE_CLUSTER_METRICS> metricsVector(actualClusterCount);

        Validate::VerifyResult(
            CastInterface<IDWriteTextLayout>()->GetClusterMetrics(
            &metricsVector[0], actualClusterCount, &actualClusterCount));

        array<DirectWrite::ClusterMetrics>^ metricsArray = gcnew array<DirectWrite::ClusterMetrics>(actualClusterCount);

        for (UINT i =0; i < actualClusterCount; i++)
        {
            metricsArray[i] =  DirectWrite::ClusterMetrics(
                metricsVector[i].width,
                metricsVector[i].length,
                metricsVector[i].canWrapLineAfter != 0,
                metricsVector[i].isWhitespace != 0,
                metricsVector[i].isNewline != 0,
                metricsVector[i].isSoftHyphen != 0,
                metricsVector[i].isRightToLeft != 0);
        }

        return Array::AsReadOnly(metricsArray);
    }

    return nullptr;
}

FLOAT TextLayout::DetermineMinWidth(
    )
{
    FLOAT returnValue;
    Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->DetermineMinWidth(&returnValue));
    return returnValue;
}

HitTestInfo TextLayout::HitTestPoint(Point2F location)
{
    DWRITE_HIT_TEST_METRICS metrics;
    BOOL tempTrailing, tempInside;

    Validate::VerifyResult(
        CastInterface<IDWriteTextLayout>()->HitTestPoint(
            safe_cast<FLOAT>(location.X), safe_cast<FLOAT>(location.Y), &tempTrailing, &tempInside, &metrics));

    HitTestMetrics returnValue;

    returnValue.CopyFrom(metrics);

    return HitTestInfo(returnValue, location, tempTrailing != FALSE, tempInside != FALSE);
}

HitTestInfo TextLayout::HitTestTextPosition(UINT32 textPosition, Boolean isTrailingHit)
{
    DWRITE_HIT_TEST_METRICS metrics;
    FLOAT tempPointX, tempPointY;

    Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->HitTestTextPosition(
        textPosition, isTrailingHit ? TRUE : FALSE, &tempPointX, &tempPointY, &metrics));

    HitTestMetrics returnMetrics;

    returnMetrics.CopyFrom(metrics);

    return HitTestInfo(returnMetrics,
        Point2F(safe_cast<Single>(tempPointX), safe_cast<Single>(tempPointY)),
        isTrailingHit, false);
}

array<HitTestMetrics>^ TextLayout::HitTestTextRange(
    UINT32 textPosition,
    UINT32 textLength,
    FLOAT originX,
    FLOAT originY
    )
{

    UINT32 actualCount;

    HRESULT hr = CastInterface<IDWriteTextLayout>()->HitTestTextRange(
        textPosition, textLength, originX, originY, 
        NULL, 0, &actualCount);

    if (hr != E_INSUFFICIENT_BUFFER)
        Validate::VerifyResult(hr);

    if (actualCount > 0)
    {
        array<HitTestMetrics>^ metricsArray = gcnew array<HitTestMetrics>(actualCount);
        pin_ptr<HitTestMetrics> metricsPtr = &metricsArray[0];

        Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->HitTestTextRange(
            textPosition, textLength, originX, originY, 
            (DWRITE_HIT_TEST_METRICS*)metricsPtr,
            actualCount, &actualCount));

        return metricsArray;
    }

    return nullptr;
}

FontFamilyCollection^ TextLayout::GetFontCollection(UInt32 currentPosition)
{
    IDWriteFontCollection *collection = NULL;

    Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->GetFontCollection(currentPosition, &collection));

    return collection != NULL ? gcnew FontFamilyCollection(collection) : nullptr;
}

TextRangeOf<FontFamilyCollection^> TextLayout::GetFontCollectionForRange(UInt32 currentPosition)
{
    IDWriteFontCollection *collection = NULL;
    DWRITE_TEXT_RANGE nativeRange;
    TextRange range;

    Validate::VerifyResult(CastInterface<IDWriteTextLayout>()->GetFontCollection(currentPosition, &collection, &nativeRange));

    range.CopyFrom(nativeRange);

    return TextRangeOf<FontFamilyCollection^>(range,
        collection != NULL ? gcnew FontFamilyCollection(collection) : nullptr);
}
