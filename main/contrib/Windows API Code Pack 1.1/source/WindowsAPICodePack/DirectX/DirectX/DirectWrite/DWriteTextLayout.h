//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DWriteTextFormat.h"
#include "DWriteFontFamilyCollection.h"

using namespace Microsoft::WindowsAPICodePack::DirectX;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct2D1;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {

interface class ICustomInlineObject;
ref class InlineObject;
ref class TypographySettingCollection;

/// <summary>
/// Represents a block of text after it has been fully analyzed and formatted.
/// <para>All coordinates are in device independent pixels (DIPs).</para>
/// <para>(Also see DirectX SDK: IDWriteTextLayout)</para>
/// </summary>
public ref class TextLayout : public TextFormat
{
public: 
    /// <summary>
    /// The text that was used to create this TextLayout.
    /// </summary>
    property String^ Text
    {
        String^ get();
    }

    /// <summary>
    /// Get or set layout maximum width
    /// </summary>
    property FLOAT MaxWidth
    {
        FLOAT get();
        void set(FLOAT);
    }
        
    /// <summary>
    /// Get or set layout maximum height
    /// </summary>
    property FLOAT MaxHeight
    {
        FLOAT get();
        void set(FLOAT);
    }

    /// <summary>
    /// Set font family name.
    /// </summary>
    /// <param name="fontFamilyName">Font family name</param>
    /// <param name="textRange">Text range to which this change applies.</param>
    void SetFontFamilyName(
        System::String^  fontFamilyName,
        TextRange textRange
        );

    /// <summary>
    /// Set font weight.
    /// </summary>
    /// <param name="fontWeight">Font weight</param>
    /// <param name="textRange">Text range to which this change applies.</param>
    void SetFontWeight(
        DirectWrite::FontWeight fontWeight,
        TextRange textRange
        );

    /// <summary>
    /// Set font style.
    /// </summary>
    /// <param name="fontStyle">Font style</param>
    /// <param name="textRange">Text range to which this change applies.</param>
    void SetFontStyle(
        DirectWrite::FontStyle fontStyle,
        TextRange textRange
        );

    /// <summary>
    /// Set font stretch.
    /// </summary>
    /// <param name="fontStretch">font stretch</param>
    /// <param name="textRange">Text range to which this change applies.</param>
    void SetFontStretch(
        DirectWrite::FontStretch fontStretch,
        TextRange textRange
        );

    /// <summary>
    /// Set font em height.
    /// </summary>
    /// <param name="fontSize">Font em height</param>
    /// <param name="textRange">Text range to which this change applies.</param>
    void SetFontSize(
        FLOAT fontSize,
        TextRange textRange
        );

    /// <summary>
    /// Set underline.
    /// </summary>
    /// <param name="hasUnderline">The Boolean flag indicates whether underline takes place</param>
    /// <param name="textRange">Text range to which this change applies.</param>
    void SetUnderline(
        Boolean hasUnderline,
        TextRange textRange
        );

    /// <summary>
    /// Set strikethrough.
    /// </summary>
    /// <param name="hasStrikethrough">The Boolean flag indicates whether strikethrough takes place</param>
    /// <param name="textRange">Text range to which this change applies.</param>
    void SetStrikethrough(
        Boolean hasStrikethrough,
        TextRange textRange
        );


    /// <summary>
    /// Set a custom inline object.
    /// </summary>
    /// <param name="inlineObject">An application-implemented inline object.</param>
    /// <param name="textRange">Text range to which this change applies.</param>
    /// <remarks>
    /// This inline object applies to the specified range and will be passed back
    /// to the application via the DrawInlineObject callback when the range is drawn.
    /// Any text in that range will be suppressed.
    /// </remarks>
    void SetInlineObject(
        ICustomInlineObject^ inlineObject,
        TextRange textRange
        );

    /// <summary>
    /// Set CultureInfo.
    /// </summary>
    /// <param name="cultureInfo">Culture Info</param>
    /// <param name="textRange">Text range to which this change applies.</param>
    void SetCultureInfo(
        Globalization::CultureInfo^ cultureInfo,
        TextRange textRange
        );

    /// <summary>
    /// Sets font typography features for text within a specified text range.
    /// </summary>
    /// <param name="typography">Font typography settings collection.</param>
    /// <param name="textRange">Text range to which this change applies.</param>
    void SetTypography(
        TypographySettingCollection^ typography,
        TextRange textRange
        );    
    
    /// <summary>
    /// Gets the font family name for the given position and text range for identically-formatted text.
    /// </summary>
    /// <param name="currentPosition">The text position for which to retrieve the font family name.</param>
    /// <returns>
    /// The font family name for the given position, and the range of text having
    /// exactly the same formatting, include font family name, as the given position.
    /// </returns>
    TextRangeOf<System::String^> GetFontFamilyNameForRange(UINT32 currentPosition);

    /// <summary>
    /// Get the font family name for the given position.
    /// </summary>
    /// <param name="currentPosition">The current text position.</param>
    /// <returns>The current font family name</returns>
    System::String^ GetFontFamilyName(
        UINT32 currentPosition
        );

    /// <summary>
    /// Gets the font weight for the given position and text range for identically-formatted text.
    /// </summary>
    /// <param name="currentPosition">The text position for which to retrieve font weight.</param>
    /// <returns>
    /// The font weight for the given position, and the range of text having
    /// exactly the same formatting, including font weight, as the given position.
    /// </returns>
    TextRangeOf<DirectWrite::FontWeight> GetFontWeightForRange(UINT32 currentPosition);

    /// <summary>
    /// Get the font weight for the given position.
    /// </summary>
    /// <param name="currentPosition">The current text position.</param>
    /// <returns>The current font weight</returns>
    DirectWrite::FontWeight GetFontWeight(
        UINT32 currentPosition
        );

    /// <summary>
    /// Gets the font style for the given position and text range for identically-formatted text.
    /// </summary>
    /// <param name="currentPosition">The text position for which to retrieve font style.</param>
    /// <returns>
    /// The font style for the given position, and the range of text having
    /// exactly the same formatting, including font style, as the given position.
    /// </returns>
    TextRangeOf<DirectWrite::FontStyle> GetFontStyleForRange(UINT32 currentPosition);

    /// <summary>
    /// Get the font style for the given position.
    /// </summary>
    /// <param name="currentPosition">The current text position.</param>
    /// <returns>
    /// The current font style.
    /// </returns>
    DirectWrite::FontStyle GetFontStyle(
        UINT32 currentPosition
        );

    /// <summary>
    /// Gets the font stretch for the given position and text range for identically-formatted text.
    /// </summary>
    /// <param name="currentPosition">The text position for which to retrieve the font stretch.</param>
    /// <returns>
    /// The font stretch for the given position, and the range of text having
    /// exactly the same formatting, including font stretch, as the given position.
    /// </returns>
    TextRangeOf<DirectWrite::FontStretch> GetFontStretchForRange(UINT32 currentPosition);


    /// <summary>
    /// Get the font stretch for the given position.
    /// </summary>
    /// <param name="currentPosition">The current text position.</param>
    /// <returns>
    /// The current font stretch.
    /// </returns>
    DirectWrite::FontStretch GetFontStretch(
        UINT32 currentPosition
        );

    /// <summary>
    /// Gets the font height for the given position and text range for identically-formatted text.
    /// </summary>
    /// <param name="currentPosition">The text position for which to retrieve the font height.</param>
    /// <returns>
    /// The font height for the given position, and the range of text having
    /// exactly the same formatting, including font height, as the given position.
    /// </returns>
    TextRangeOf<Single> GetFontSizeForRange(UINT32 currentPosition);

    /// <summary>
    /// Get the font em height for the given position.
    /// </summary>
    /// <param name="currentPosition">The current text position.</param>
    /// <returns>
    /// The current font em height.
    /// </returns>
    FLOAT GetFontSize(
        UINT32 currentPosition
        );

    /// <summary>
    /// Gets the underline presence for the given position and text range for identically-formatted text.
    /// </summary>
    /// <param name="currentPosition">The text position for which to retrieve the underline presence.</param>
    /// <returns>
    /// The underline presence for the given position, and the range of text having
    /// exactly the same formatting, including underline presence, as the given position.
    /// </returns>
    TextRangeOf<Boolean> GetUnderlineForRange(UINT32 currentPosition);


    /// <summary>
    /// Get the underline presence for the given position.
    /// </summary>
    /// <param name="currentPosition">The current text position.</param>
    /// <returns>
    /// The Boolean flag indicates whether text is underlined.
    /// </returns>
    Boolean GetUnderline(
        UINT32 currentPosition
        );

    /// <summary>
    /// Gets the strikethrough property for the given position and text range for identically-formatted text.
    /// </summary>
    /// <param name="currentPosition">The text position for which to retrieve the strikethrough property.</param>
    /// <returns>
    /// The strikethrough property for the given position, and the range of text having
    /// exactly the same formatting, including the strikethrough property value, as the given position.
    /// </returns>
    TextRangeOf<Boolean> GetStrikethroughForRange(UINT32 currentPosition);

    /// <summary>
    /// Get the strikethrough presence for the given position.
    /// </summary>
    /// <param name="currentPosition">The current text position.</param>
    /// <returns>
    /// The Boolean flag indicates whether text has strikethrough.
    /// </returns>
    Boolean GetStrikethrough(
        UINT32 currentPosition
        );

    /// <summary>
    /// Gets the culture information for the given position and text range for identically-formatted text.
    /// </summary>
    /// <param name="currentPosition">The text position for which to retrieve culture information.</param>
    /// <returns>
    /// The culture information for the given position, and the range of text having
    /// exactly the same formatting, including culture, as the given position.
    /// </returns>
    TextRangeOf<Globalization::CultureInfo^> GetCultureInfoForRange(UINT32 currentPosition);

    /// <summary>
    /// Get the culture info for the given position.
    /// </summary>
    /// <param name="currentPosition">The current text position.</param>
    /// <returns>
    /// The current culture info.
    /// </returns>
    Globalization::CultureInfo^ GetCultureInfo(UINT32 currentPosition);

    /// <summary>
    /// Gets the typography setting for the given position and text range for identically-formatted text.
    /// </summary>
    /// <param name="currentPosition">The text position for which to retrieve the typography setting.</param>
    /// <returns>
    /// The typography setting for the given position, and the range of text having
    /// exactly the same formatting, including typography setting, as the given position.
    /// </returns>
    TextRangeOf<TypographySettingCollection^> GetTypographyForRange(UINT32 currentPosition);

    /// <summary>
    /// Gets the typography setting of the text at the specified position. 
    /// </summary>
    /// <param name="currentPosition">The current text position.</param>
    /// <returns>
    /// The current typography collection, or null if no typography has been set.
    /// </returns>
    TypographySettingCollection^ GetTypography(
        UINT32 currentPosition
        );

    /// <summary>
    /// Retrieves the information about each individual text line of the text string. 
    /// </summary>
    /// <returns>Properties of each line.</returns>
    property IEnumerable<DirectWrite::LineMetrics>^ LineMetrics
    {
        IEnumerable<DirectWrite::LineMetrics>^ get(void);
    }

    /// <summary>
    /// Retrieves overall metrics for the formatted string.
    /// </summary>
    /// <remarks>
    /// Drawing effects like underline and strikethrough do not contribute
    /// to the text size, which is essentially the sum of advance widths and
    /// line heights. Additionally, visible swashes and other graphic
    /// adornments may extend outside the returned width and height.
    /// </remarks>
    property TextMetrics Metrics
    {
        TextMetrics get();
    }

    /// <summary>
    /// GetOverhangMetrics returns the overhangs (in DIPs) of the layout and all
    /// objects contained in it, including text glyphs and inline objects.
    /// </summary>
    /// <remarks>
    /// Any underline and strikethrough do not contribute to the black box
    /// determination, since these are actually drawn by the renderer, which
    /// is allowed to draw them in any variety of styles.
    /// </remarks>
    property DirectWrite::OverhangMetrics OverhangMetrics
    {
        DirectWrite::OverhangMetrics get();
    }


    /// <summary>
    /// Retrieves logical properties and measurements of each glyph cluster. 
    /// </summary>
    /// <returns>
    /// Array of Cluster Metrics such as line-break or total advance width, for each glyph cluster.
    /// </returns>
    property IEnumerable<ClusterMetrics>^ ClusterMetrics
    {
        IEnumerable<DirectWrite::ClusterMetrics>^ get(void);
    }

    /// <summary>
    /// Determines the minimum possible width the layout can be set to without
    /// emergency breaking between the characters of whole words.
    /// </summary>
    /// <returns>
    /// Minimum width.
    /// </returns>
    FLOAT DetermineMinWidth(void);

    /// <summary>
    /// Given a coordinate (in DIPs) relative to the top-left of the layout box,
    /// this returns the corresponding hit-test metrics of the text string where
    /// the hit-test has occurred. This is useful for mapping mouse clicks to caret
    /// positions. When the given coordinate is outside the text string, the function
    /// sets the output value *isInside to false but returns the nearest character
    /// position.
    /// </summary>
    /// <param name="location">The coordinates of the location to hit-test, relative to the top-left location of the layout box.</param>
    /// <returns>
    /// A <see cref="HitTestInfo" /> value that includes the output geometry containing or nearest
    /// the hit-test location, and flags providing more information about the location tested.
    /// </returns>
    HitTestInfo HitTestPoint(Point2F location);

    /// <summary>
    /// Given a text position and whether the caret is on the leading or trailing
    /// edge of that position, this returns the corresponding coordinate (in DIPs)
    /// relative to the top-left of the layout box. This is most useful for drawing
    /// the caret's current position, but it could also be used to anchor an IME to the
    /// typed text or attach a floating menu near the point of interest. It may also be
    /// used to programmatically obtain the geometry of a particular text position
    /// for UI automation.
    /// </summary>
    /// <param name="textPosition">Text position to get the coordinate of.</param>
    /// <param name="isTrailingHit">Flag indicating whether the location is of the leading or the trailing side of the specified text position. </param>
    /// <returns>
    /// A <see cref="HitTestInfo" /> value that includes the output geometry containing
    /// the given text position, and the coordinates of the given text position.
    /// </returns>
    /// <remarks>
    /// When drawing a caret at the returned X,Y, it should should be centered on X
    /// and drawn from the Y coordinate down. The height will be the size of the
    /// hit-tested text (which can vary in size within a line).
    /// Reading direction also affects which side of the character the caret is drawn.
    /// However, the returned X coordinate will be correct for either case.
    /// You can get a text length back that is larger than a single character.
    /// This happens for complex scripts when multiple characters form a single cluster,
    /// when diacritics join their base character, or when you test a surrogate pair.
    /// </remarks>
    HitTestInfo HitTestTextPosition(UINT32 textPosition, Boolean isTrailingHit);

    /// <summary>
    /// The application calls this function to get a set of hit-test metrics
    /// corresponding to a range of text positions. The main usage for this
    /// is to draw highlighted selection of the text string.
    /// </summary>
    /// <param name="textPosition">First text position of the specified range.</param>
    /// <param name="textLength">Number of positions of the specified range.</param>
    /// <param name="originX">Offset of the X origin (left of the layout box) which is added to each of the hit-test metrics returned.</param>
    /// <param name="originY">Offset of the Y origin (top of the layout box) which is added to each of the hit-test metrics returned.</param>
    /// <returns>
    /// Aarray of the output geometry fully enclosing the specified position range.
    /// </returns>
    /// <remarks>
    /// There are no gaps in the returned metrics. While there could be visual gaps,
    /// depending on bidi ordering, each range is contiguous and reports all the text,
    /// including any hidden characters and trimmed text.
    /// The height of each returned range will be the same within each line, regardless
    /// of how the font sizes vary.
    /// </remarks>
    array<HitTestMetrics>^ HitTestTextRange(
        UINT32 textPosition,
        UINT32 textLength,
        FLOAT originX,
        FLOAT originY
        );

    /// <summary>
    /// Gets the font family collection used for the given position.
    /// </summary>
    /// <param name="currentPosition">The text position for which to retrieve the font family collection.</param>
    /// <returns>
    /// The font family collection for the given position.
    /// </returns>
    FontFamilyCollection^ GetFontCollection(UInt32 currentPosition);

    /// <summary>
    /// Gets the font family collection used for the given position and text range for identically-formatted text.
    /// </summary>
    /// <param name="currentPosition">The text position for which to retrieve the font family collection.</param>
    /// <returns>
    /// The font family collection for the given position, and the range of text having
    /// exactly the same formatting, include font family collection, as the given position.
    /// </returns>
    TextRangeOf<FontFamilyCollection^> GetFontCollectionForRange(UInt32 currentPosition);

private:
    String^ _text;

internal:
    TextLayout() : TextFormat()
    { }

    TextLayout(IDWriteTextLayout* pNativeIDWriteTextLayout, String^ text) : TextFormat(pNativeIDWriteTextLayout), _text(text)
    { }
};
} } } }