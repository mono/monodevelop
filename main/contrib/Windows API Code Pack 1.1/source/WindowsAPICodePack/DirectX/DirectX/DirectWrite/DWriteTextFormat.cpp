// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DWriteTextFormat.h"
#include "ICustomInlineObject.h"
#include "DWriteInlineObject.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::DirectWrite;

DirectWrite::TextAlignment TextFormat::TextAlignment::get() 
{
    return static_cast<DirectWrite::TextAlignment>(CastInterface<IDWriteTextFormat>()->GetTextAlignment());
}

void TextFormat::TextAlignment::set(DirectWrite::TextAlignment value) 
{
    CastInterface<IDWriteTextFormat>()->SetTextAlignment(static_cast<DWRITE_TEXT_ALIGNMENT>(value));
}

DirectWrite::ParagraphAlignment TextFormat::ParagraphAlignment::get() 
{
    return static_cast<DirectWrite::ParagraphAlignment>(CastInterface<IDWriteTextFormat>()->GetParagraphAlignment());
}

void TextFormat::ParagraphAlignment::set(DirectWrite::ParagraphAlignment value) 
{
    CastInterface<IDWriteTextFormat>()->SetParagraphAlignment(static_cast<DWRITE_PARAGRAPH_ALIGNMENT>(value));
}


DirectWrite::WordWrapping TextFormat::WordWrapping::get() 
{
    return static_cast<DirectWrite::WordWrapping>(CastInterface<IDWriteTextFormat>()->GetWordWrapping());
}

void TextFormat::WordWrapping::set(DirectWrite::WordWrapping value) 
{
    CastInterface<IDWriteTextFormat>()->SetWordWrapping(static_cast<DWRITE_WORD_WRAPPING>(value));
}


DirectWrite::ReadingDirection TextFormat::ReadingDirection::get() 
{
    return static_cast<DirectWrite::ReadingDirection>(CastInterface<IDWriteTextFormat>()->GetReadingDirection());
}

void TextFormat::ReadingDirection::set(DirectWrite::ReadingDirection value) 
{
    CastInterface<IDWriteTextFormat>()->SetReadingDirection(static_cast<DWRITE_READING_DIRECTION>(value));
}


DirectWrite::FlowDirection TextFormat::FlowDirection::get() 
{
    return static_cast<DirectWrite::FlowDirection>(CastInterface<IDWriteTextFormat>()->GetFlowDirection());
}

void TextFormat::FlowDirection::set(DirectWrite::FlowDirection value) 
{
    CastInterface<IDWriteTextFormat>()->SetFlowDirection(static_cast<DWRITE_FLOW_DIRECTION>(value));
}


Single TextFormat::IncrementalTabStop::get() 
{
    return CastInterface<IDWriteTextFormat>()->GetIncrementalTabStop();
}

void TextFormat::IncrementalTabStop::set(Single value) 
{
    CastInterface<IDWriteTextFormat>()->SetIncrementalTabStop(value);
}


DirectWrite::LineSpacing TextFormat::LineSpacing::get() 
{
    DWRITE_LINE_SPACING_METHOD method;
    FLOAT lineSpacing;
    FLOAT baseline;

    Validate::VerifyResult(
        CastInterface<IDWriteTextFormat>()->GetLineSpacing(&method, &lineSpacing, &baseline));

    return DirectWrite::LineSpacing(static_cast<LineSpacingMethod>(method), lineSpacing, baseline);
}

void TextFormat::LineSpacing::set(DirectWrite::LineSpacing value) 
{
    Validate::VerifyResult(
        CastInterface<IDWriteTextFormat>()->SetLineSpacing(static_cast<DWRITE_LINE_SPACING_METHOD>(value.LineSpacingMethod), value.Height, value.Baseline));
}

DirectWrite::Trimming TextFormat::Trimming::get()
{
    DWRITE_TRIMMING trimmingCopy;
    ::IDWriteInlineObject* inlineObject = NULL;
    try 
    {
        Validate::VerifyResult(
            CastInterface<IDWriteTextFormat>()->GetTrimming(&trimmingCopy, &inlineObject));
    }
    finally
    {
        if (inlineObject)
        {
            inlineObject->Release();
        }
    }
    
    DirectWrite::Trimming returnValue;
    returnValue.CopyFrom(trimmingCopy);
    return returnValue;
}

void TextFormat::Trimming::set(DirectWrite::Trimming value)
{
    DWRITE_TRIMMING trimmingCopy;
    ::IDWriteInlineObject* inlineObject = NULL;

    try
    {
        // First get a copy of the inline object,
        // The method may fail, so don't throw.
        CastInterface<IDWriteTextFormat>()->GetTrimming(&trimmingCopy, &inlineObject);

        // Copy the trimming
        value.CopyTo(&trimmingCopy);

        Validate::VerifyResult(
            CastInterface<IDWriteTextFormat>()->SetTrimming(&trimmingCopy, inlineObject));    
    }
    finally
    {
        if (inlineObject) // We won't need it, so release
        {
            inlineObject->Release();
        }
    }
}

DirectWrite::TrimmingWithSign TextFormat::TrimmingWithSign::get(void)
{
    DWRITE_TRIMMING trimmingCopy;

    IDWriteInlineObject* inlineObject = NULL;

    Validate::VerifyResult(
        CastInterface<IDWriteTextFormat>()->GetTrimming(&trimmingCopy, &inlineObject));

    DirectWrite::Trimming returnValue;

    returnValue.CopyFrom(trimmingCopy);

    return DirectWrite::TrimmingWithSign(returnValue,
        inlineObject != NULL ? gcnew InlineObject(inlineObject) : nullptr);
}

void TextFormat::TrimmingWithSign::set(DirectWrite::TrimmingWithSign trimmingWithSign)
{
    AutoInlineObject sign(trimmingWithSign.Sign);
    DWRITE_TRIMMING tempOptions;

    trimmingWithSign.Trimming.CopyTo(&tempOptions);

    Validate::VerifyResult(CastInterface<IDWriteTextFormat>()->SetTrimming(&tempOptions, sign.GetNativeInline()));
}

String^ TextFormat::FontFamilyName::get() 
{
    UINT32 len = CastInterface<IDWriteTextFormat>()->GetFontFamilyNameLength() + 1;
    wchar_t* name = new wchar_t[len];

    try
    {        
        Validate::VerifyResult(CastInterface<IDWriteTextFormat>()->GetFontFamilyName(name, len));
        return gcnew String(name);
    }
    finally
    {
        delete [] name;
    }
}


DirectWrite::FontWeight TextFormat::FontWeight::get() 
{
    return static_cast<DirectWrite::FontWeight>(CastInterface<IDWriteTextFormat>()->GetFontWeight());
}


DirectWrite::FontStyle TextFormat::FontStyle::get() 
{
    return static_cast<DirectWrite::FontStyle>(CastInterface<IDWriteTextFormat>()->GetFontStyle());
}


DirectWrite::FontStretch TextFormat::FontStretch::get() 
{
    return static_cast<DirectWrite::FontStretch>(CastInterface<IDWriteTextFormat>()->GetFontStretch());
}


Single TextFormat::FontSize::get() 
{
    return CastInterface<IDWriteTextFormat>()->GetFontSize();
}


System::Globalization::CultureInfo^ TextFormat::CultureInfo::get() 
{
    UINT32 len = CastInterface<IDWriteTextFormat>()->GetLocaleNameLength() + 1;
    wchar_t* name = new wchar_t[len];

    try
    {        
        Validate::VerifyResult(CastInterface<IDWriteTextFormat>()->GetLocaleName(name, len));
        return gcnew System::Globalization::CultureInfo(gcnew String(name));
    }
    finally
    {
        delete [] name;
    }
}

