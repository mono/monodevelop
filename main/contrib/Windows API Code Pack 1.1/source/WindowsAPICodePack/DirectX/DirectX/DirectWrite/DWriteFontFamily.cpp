// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DWriteFontFamily.h"
#include "DWriteFont.h"
#include "DWriteUtils.h"

#include <msclr/lock.h>
using namespace msclr;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;

FontFamily::~FontFamily()
{
    if (m_names != nullptr)
    {
        delete m_names;
        m_names = nullptr;
    }

    if (m_fonts != nullptr)
    {
        for each (Font^ font in m_fonts)
        {
            if (font != nullptr)
            {
                delete font;
            }
        }
        m_fonts = nullptr;
    }
}

IDictionary<CultureInfo^, String^>^ FontFamily::FamilyNames::get()
{ 
    // REVIEW: false thread-safety!
    // this class is not generally thread-safe, including another cached-
    // names variable (m_faceNames), so why bother with this one?  Client
    // code should be handling synchronization if necessary.  On top of that,
    // synchronizing on "this" is not a good idea, _and_ not all uses of
    // "m_names" are synchronized (i.e. the destructor).

    lock l(this); // Make sure the collection is not being created on another thread

    if (m_names == nullptr)
    {
        IDWriteLocalizedStrings* names = NULL;

        try
        {
            Validate::VerifyResult(CastInterface<IDWriteFontFamily>()->GetFamilyNames(&names));
            
            if (names)
                m_names = DWriteUtils::GetCultureNameDictionary(names);
        }
        finally
        {
            if (names)
                names->Release();
        }
    }

    return m_names;
}



ReadOnlyCollection<DirectWrite::Font^>^ FontFamily::Fonts::get()
{
    lock l(this); // Make sure the collection is not being created on another thread

    if (m_fonts == nullptr)
    {
        List<DirectWrite::Font^>^ fontList = gcnew List<DirectWrite::Font^>();

        UINT count = CastInterface<IDWriteFontFamily>()->GetFontCount();

        for (UINT i = 0; i < count; i++)
        {
            IDWriteFont* font = NULL;
            CastInterface<IDWriteFontFamily>()->GetFont(i, &font);

            if (font)
            {
                fontList->Add(gcnew DirectWrite::Font(font));
            }
        }

        m_fonts = gcnew ReadOnlyCollection<DirectWrite::Font^>(fontList);
    }

    return m_fonts;
}