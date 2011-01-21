//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "DWriteStructs.h"

using namespace Microsoft::WindowsAPICodePack::DirectX;
using namespace System::Globalization;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {

    ref class FontFamily;
    ref class FontFace;
    
    /// <summary>
    /// The Font represents a physical font in a font collection.
    /// <para>(Also see DirectX SDK: IDWriteFont)</para>
    /// </summary>
    public ref class Font : public DirectUnknown
    {
    public: 

    /// <summary>
    /// Gets localized face names for the font (e.g., Regular or Bold), indexed by culture.
    /// </summary>
    property IDictionary<CultureInfo^, String^>^ FaceNames
    {
        IDictionary<CultureInfo^, String^>^ get();
    }

    /// <summary>
    /// Gets a dictionary containing the specified informational strings, indexed by culture.
    /// </summary>
    /// <param name="informationalStringId">Identifies the string to get.</param>
    /// <returns>
    /// A dictionary containing the specified informational strings, indexed by culture. 
    /// If the font does not contain the specified string, the returned dictionary is null. 
    /// </returns>
    IDictionary<CultureInfo^, String^>^ 
        GetInformationalStrings(InformationalStringId informationalStringId);

    /// <summary>
    /// Gets the font family to which the specified font belongs.
    /// </summary>
    property DirectWrite::FontFamily^ FontFamily    
    {
        DirectWrite::FontFamily^ get();
    }

    /// <summary>
    /// Gets the weight of the specified font.
    /// </summary>
    property FontWeight Weight
    {
        FontWeight get();
    }

    /// <summary>
    /// Gets the stretch (aka. width) of the specified font.
    /// </summary>
    property FontStretch Stretch
    {
        FontStretch get();
    }

    /// <summary>
    /// Gets the style (aka. slope) of the specified font.
    /// </summary>
    property FontStyle Style
    {
        FontStyle get();
    }

    /// <summary>
    /// Returns TRUE if the font is a symbol font or FALSE if not.
    /// </summary>
    property Boolean IsSymbolFont
    {
        Boolean get();
    }

    /// <summary>
    /// Gets a value that indicates what simulation are applied to the specified font.
    /// </summary>
    property FontSimulations Simulations
    {
        FontSimulations get();
    }

    /// <summary>
    /// Gets the metrics for the font.
    /// </summary>
    /// <param name="fontMetrics">Receives the font metrics.</param>
    property FontMetrics Metrics
    {
        FontMetrics get();
    }

    /// <summary>
    /// Determines whether the font supports the specified character.
    /// </summary>
    /// <param name="unicodeValue">Unicode (UCS-4) character value.</param>
    /// <returns>
    /// True if the font supports the specified character or False if not.
    /// </returns>
    Boolean HasCharacter(UInt32 unicodeValue);

    /// <summary>
    /// Determines whether the font supports the specified character.
    /// </summary>
    /// <param name="character">That Unicode character.</param>
    /// <returns>
    /// True if the font supports the specified character or False if not.
    /// </returns>
    Boolean HasCharacter(System::Char character);

    /// <summary>
    /// Creates a font face object for the font.
    /// </summary>
    FontFace^ CreateFontFace();


    internal:
        Font() 
        { }
    
        Font(IDWriteFont* pNativeIDWriteFont) : DirectUnknown(pNativeIDWriteFont)
        { }

    private:
        IDictionary<CultureInfo^, String^>^ m_faceNames;
        DirectWrite::FontFamily^ m_fontFamily;

    };

} } } }