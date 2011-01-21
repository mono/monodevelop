//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "DWriteStructs.h"

#ifdef GetGlyphIndexes // make sure the ::GetGlyphIndexes function is undefined
#undef GetGlyphIndexes
#endif

using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {

    /// <summary>
    /// Represents an absolute reference to a font face.
    /// It contains font face type, appropriate file references and face identification data.
    /// Various font data such as metrics, names and glyph outlines is obtained from FontFace.
    /// </summary>
    public ref class FontFace : public DirectUnknown
    {
    public: 

        /// <summary>
        /// The file format type of a font face.
        /// </summary>
        property FontFaceType FaceType
        {
            FontFaceType get();
        }

        /// <summary>
        /// The zero-based index of the font face in its font file or files. If the font files contain a single face,
        /// the return value is zero.
        /// </summary>
        property UINT32 Index
        {
            UINT32 get();
        }

        /// <summary>
        /// The algorithmic style simulation flags of a font face.
        /// </summary>
        property FontSimulations Simulations
        {
            FontSimulations get();
        }

        /// <summary>
        /// Determines whether the font is a symbol font.
        /// </summary>
        property Boolean IsSymbolFont
        {
            Boolean get();
        }

        /// <summary>
        /// Obtains design units and common metrics for the font face.
        /// These metrics are applicable to all the glyphs within a fontface and are used by applications for layout calculations.
        /// </summary>
        property FontMetrics Metrics
        {
            FontMetrics get();
        }

        /// <summary>
        /// The number of glyphs in the font face.
        /// </summary>
        property UINT16 GlyphCount
        {
            UINT16 get();
        }

        /// <summary>
        /// Obtains ideal glyph metrics in font design units. Design glyphs metrics are used for glyph positioning.
        /// </summary>
        /// <param name="glyphIndexes">An array of glyph indices to compute the metrics for.</param>
        /// <param name="isSideways">Indicates whether the font is being used in a sideways run.
        /// This can affect the glyph metrics if the font has oblique simulation
        /// because sideways oblique simulation differs from non-sideways oblique simulation.</param>
        /// <returns>
        /// Array of <see cref="GlyphMetrics"/> structures filled by this function.
        /// The metrics returned by this function are in font design units.
        /// </returns>
        array<GlyphMetrics>^ GetDesignGlyphMetrics(
            array<UINT16>^ glyphIndexes,
            BOOL isSideways);

        /// <summary>
        /// Obtains ideal glyph metrics in font design units. Design glyphs metrics are used for glyph positioning.
        /// This function assume Sideways is false.
        /// </summary>
        /// <param name="glyphIndexes">An array of glyph indices to compute the metrics for.</param>
        /// <returns>
        /// Array of <see cref="GlyphMetrics"/> structures filled by this function.
        /// The metrics returned by this function are in font design units.
        /// </returns>
        array<GlyphMetrics>^ GetDesignGlyphMetrics(array<UINT16>^ glyphIndexes);

        /// <summary>
        /// Returns the nominal mapping of UCS4 Unicode code points to glyph indices as defined by the font 'CMAP' table.
        /// Note that this mapping is primarily provided for line layout engines built on top of the physical font API.
        /// Because of OpenType glyph substitution and line layout character substitution, the nominal conversion does not always correspond
        /// to how a Unicode string will map to glyph indices when rendering using a particular font face.
        /// Also, note that Unicode Variant Selectors provide for alternate mappings for character to glyph.
        /// This call will always return the default variant.
        /// </summary>
        /// <param name="codePoints">An array of USC4 code points to obtain nominal glyph indices from.</param>
        /// <returns>
        /// Array of nominal glyph indices filled by this function.
        /// </returns>
        array<UINT16>^ GetGlyphIndexes(array<UINT32>^ codePoints);

        /// <summary>
        /// Returns the nominal mapping of UCS4 Unicode code points to glyph indices as defined by the font 'CMAP' table.
        /// Note that this mapping is primarily provided for line layout engines built on top of the physical font API.
        /// Because of OpenType glyph substitution and line layout character substitution, the nominal conversion does not always correspond
        /// to how a Unicode string will map to glyph indices when rendering using a particular font face.
        /// Also, note that Unicode Variant Selectors provide for alternate mappings for character to glyph.
        /// This call will always return the default variant.
        /// </summary>
        /// <param name="characterArray">An array of characters to obtain nominal glyph indices from.</param>
        /// <returns>
        /// Array of nominal glyph indices filled by this function.
        /// </returns>
        array<UINT16>^ GetGlyphIndexes(array<System::Char>^ characterArray);

        /// <summary>
        /// Returns the nominal mapping of UCS4 Unicode code points to glyph indices as defined by the font 'CMAP' table.
        /// Note that this mapping is primarily provided for line layout engines built on top of the physical font API.
        /// Because of OpenType glyph substitution and line layout character substitution, the nominal conversion does not always correspond
        /// to how a Unicode string will map to glyph indices when rendering using a particular font face.
        /// Also, note that Unicode Variant Selectors provide for alternate mappings for character to glyph.
        /// This call will always return the default variant.
        /// </summary>
        /// <param name="text">The string to obtain nominal glyph indices for its characters.</param>
        /// <returns>
        /// Array of nominal glyph indices filled by this function.
        /// </returns>
        array<UINT16>^ GetGlyphIndexes(System::String^ text);

    internal:
        FontFace() 
        { }

        FontFace(IDWriteFontFace* pNativeIDWriteFontFace) : DirectUnknown(pNativeIDWriteFontFace)
        { }

    };

} } } }