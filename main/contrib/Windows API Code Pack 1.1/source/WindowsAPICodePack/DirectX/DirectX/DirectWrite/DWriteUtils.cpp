//Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "DWriteUtils.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;

IDictionary<System::Globalization::CultureInfo^, System::String^>^ DWriteUtils::GetCultureNameDictionary(IDWriteLocalizedStrings* names)
{

    Dictionary<System::Globalization::CultureInfo^, System::String^>^ namesDictionary =
        gcnew Dictionary<System::Globalization::CultureInfo^, System::String^>();

    UINT count = names->GetCount();
    for (UINT i = 0 ; i < count ; i++)
    {
        UINT localeLength;
        Validate::VerifyResult(names->GetLocaleNameLength(i, &localeLength));

        UINT nameLength;
        Validate::VerifyResult(names->GetStringLength(i, &nameLength));

#pragma warning(push)
    // The two buffers here are only allocated within the 'try' clause and
    // the 'finally' clause will ensure they are cleaned up.
    // cl /analysis appears to not understand the C++/CLI 'finally' syntax
#pragma warning(disable:6211)

        WCHAR* locale = NULL;
        WCHAR* name = NULL;

        try
        {
            locale = new WCHAR[localeLength + 1];
            name = new WCHAR[nameLength + 1];

            Validate::VerifyResult(names->GetLocaleName(i, locale, localeLength + 1));
            Validate::VerifyResult(names->GetString(i, name, nameLength + 1));

            // REVIEW: why not use CultureInfo::GetCultureInfo() method here?
            namesDictionary->Add(gcnew System::Globalization::CultureInfo(gcnew String(locale)), gcnew String(name));
        }
        finally
        {
            delete [] locale;
            delete [] name;
        }

#pragma warning(pop)

    }

    return namesDictionary;
}