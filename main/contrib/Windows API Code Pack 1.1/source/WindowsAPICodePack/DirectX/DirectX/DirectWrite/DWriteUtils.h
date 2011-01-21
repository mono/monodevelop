// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace  Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {

private ref class DWriteUtils
{
private:
    // Prohibit instantiation of static class
    DWriteUtils() { }

internal:
    // Special-purpose method for DirectWrite support.  Definitely not
    // something that should be in the "common" utility class.
    static IDictionary<System::Globalization::CultureInfo^, System::String^>^
        GetCultureNameDictionary(IDWriteLocalizedStrings* names);
};

} } } }