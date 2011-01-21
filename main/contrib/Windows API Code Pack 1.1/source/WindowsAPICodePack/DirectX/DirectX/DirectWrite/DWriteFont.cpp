// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DWriteFont.h"
#include "DWriteFontFace.h"
#include "DWriteFontFamily.h"
#include "DWriteUtils.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;

IDictionary<CultureInfo^, System::String^>^ DirectWrite::Font::FaceNames::get()
{
    if (m_faceNames == nullptr)
    {
        IDWriteLocalizedStrings* names = NULL;

        try
        {
            Validate::VerifyResult(CastInterface<IDWriteFont>()->GetFaceNames(&names));

            if (names)
                m_faceNames = DWriteUtils::GetCultureNameDictionary(names);

        }
        finally
        {
            if (names)
                names->Release();
        }
    }


    return m_faceNames;
}

IDictionary<CultureInfo^, System::String^>^ DirectWrite::Font::GetInformationalStrings(InformationalStringId informationalStringId)
{
    IDWriteLocalizedStrings* names = NULL;

    try
    {
        BOOL exists = FALSE;
        Validate::VerifyResult(CastInterface<IDWriteFont>()->GetInformationalStrings(
            static_cast<DWRITE_INFORMATIONAL_STRING_ID>(informationalStringId),
            &names,
            &exists));

        if (exists && names)
            return DWriteUtils::GetCultureNameDictionary(names);
        else
            return nullptr;
    }
    finally
    {

        if (names)
            names->Release();
    }
}


DirectWrite::FontFamily^ DirectWrite::Font::FontFamily::get()
{
    if (m_fontFamily == nullptr)
    {
        IDWriteFontFamily* fontFamily = NULL;
        Validate::VerifyResult(CastInterface<IDWriteFont>()->GetFontFamily(&fontFamily));

        m_fontFamily = fontFamily ? gcnew DirectWrite::FontFamily(fontFamily) : nullptr;
    }

    return m_fontFamily;
}

FontWeight DirectWrite::Font::Weight::get()
{
    return static_cast<FontWeight>(CastInterface<IDWriteFont>()->GetWeight());
}

FontStretch DirectWrite::Font::Stretch::get()
{
    return static_cast<FontStretch>(CastInterface<IDWriteFont>()->GetStretch());
}

FontStyle DirectWrite::Font::Style::get()
{
    return static_cast<FontStyle>(CastInterface<IDWriteFont>()->GetStyle());
}

Boolean DirectWrite::Font::IsSymbolFont::get()
{
    return CastInterface<IDWriteFont>()->IsSymbolFont() != 0;
}


FontSimulations DirectWrite::Font::Simulations::get()
{
    return static_cast<FontSimulations>(CastInterface<IDWriteFont>()->GetSimulations());
}

FontMetrics DirectWrite::Font::Metrics::get()
{
    DWRITE_FONT_METRICS metricsCopy;
    CastInterface<IDWriteFont>()->GetMetrics(&metricsCopy);

    FontMetrics metrics;
    metrics.CopyFrom(metricsCopy);

    return metrics;
}

Boolean DirectWrite::Font::HasCharacter(UInt32 unicodeValue)
{
    BOOL exists;
    Validate::VerifyResult(CastInterface<IDWriteFont>()->HasCharacter(unicodeValue, &exists));

    return exists != 0;
}

Boolean DirectWrite::Font::HasCharacter(System::Char character)
{
    BOOL exists;
    Validate::VerifyResult(CastInterface<IDWriteFont>()->HasCharacter(System::Convert::ToUInt32(character), &exists));

    return exists != 0;
}

FontFace^ DirectWrite::Font::CreateFontFace()
{
    IDWriteFontFace* fontFace = NULL;
    Validate::VerifyResult(CastInterface<IDWriteFont>()->CreateFontFace(&fontFace));

    return fontFace ? gcnew FontFace(fontFace) : nullptr;
}
