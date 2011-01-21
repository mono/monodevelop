//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "DWriteEnums.h"
#include "DWriteStructs.h"

using namespace System::Globalization;
using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {
    
    ref class Font;

    /// <summary>
    /// The FontFamily represents a set of fonts that share the same design but are differentiated
    /// by weight, stretch, and style.
    /// <para>(Also see DirectX SDK: IDWriteFontFamily)</para>
    /// </summary>
    public ref class FontFamily : public DirectUnknown
    {

    public: 

    /// <summary>
    /// Gets the family names for the font family, indexed by culture info.
    /// </summary>
    property IDictionary<CultureInfo^, String^>^ FamilyNames
    {
        IDictionary<CultureInfo^, System::String^>^ get();
    }

    /// <summary>
    /// Gets the collection of <see cref="DirectWrite::Font"/>s of this family.
    /// </summary>
    property ReadOnlyCollection<DirectWrite::Font^>^ Fonts
    {
        ReadOnlyCollection<DirectWrite::Font^>^ get();
    }

    internal:
        FontFamily() 
        { }
    
        FontFamily(IDWriteFontFamily* pNativeIDWriteFontFamily) : DirectUnknown(pNativeIDWriteFontFamily)
        { }

        ~FontFamily();

    private:
        IDictionary<CultureInfo^, String^>^ m_names;
        ReadOnlyCollection<Font^>^ m_fonts;
    };

} } } }