//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite { 

    /// <summary>
    /// Specifies the type of DirectWrite factory object.
    /// DirectWrite factory contains internal state such as font loader registration and cached font data.
    /// In most cases it is recommended to use the shared factory object, because it allows multiple components
    /// that use DirectWrite to share internal DirectWrite state and reduce memory usage.
    /// However, there are cases when it is desirable to reduce the impact of a component,
    /// such as a plug-in from an untrusted source, on the rest of the process by sandboxing and isolating it
    /// from the rest of the process components. In such cases, it is recommended to use an isolated factory for the sandboxed
    /// component.
    /// </summary>
    public enum class DWriteFactoryType
    {
        /// <summary>
        /// Shared factory allow for re-use of cached font data across multiple in process components.
        /// Such factories also take advantage of cross process font caching components for better performance.
        /// </summary>
        Shared = DWRITE_FACTORY_TYPE_SHARED,

        /// <summary>
        /// Objects created from the isolated factory do not interact with internal DirectWrite state from other components.
        /// </summary>
        Isolated = DWRITE_FACTORY_TYPE_ISOLATED
    };



    /// <summary>
    /// The font weight enumeration describes common values for degree of blackness or thickness of strokes of characters in a font.
    /// Font weight values less than 1 or greater than 999 are considered to be invalid, and they are rejected by font API functions.
    /// </summary>
    public enum class FontWeight
    {
        /// <summary>
        /// Predefined font weight : Thin (100).
        /// </summary>
        Thin = DWRITE_FONT_WEIGHT_THIN,

        /// <summary>
        /// Predefined font weight : Extra-light (200).
        /// </summary>
        ExtraLight = DWRITE_FONT_WEIGHT_EXTRA_LIGHT,

        /// <summary>
        /// Predefined font weight : Ultra-light (200).
        /// </summary>
        UltraLight = DWRITE_FONT_WEIGHT_ULTRA_LIGHT,

        /// <summary>
        /// Predefined font weight : Light (300).
        /// </summary>
        Light = DWRITE_FONT_WEIGHT_LIGHT,

        /// <summary>
        /// Predefined font weight : Normal (400).
        /// </summary>
        Normal = DWRITE_FONT_WEIGHT_NORMAL,

        /// <summary>
        /// Predefined font weight : Regular (400).
        /// </summary>
        Regular = DWRITE_FONT_WEIGHT_REGULAR,

        /// <summary>
        /// Predefined font weight : Medium (500).
        /// </summary>
        Medium = DWRITE_FONT_WEIGHT_MEDIUM,

        /// <summary>
        /// Predefined font weight : Demi-bold (600).
        /// </summary>
        DemiBold = DWRITE_FONT_WEIGHT_DEMI_BOLD,

        /// <summary>
        /// Predefined font weight : Semi-bold (600).
        /// </summary>
        SemiBold = DWRITE_FONT_WEIGHT_SEMI_BOLD,

        /// <summary>
        /// Predefined font weight : Bold (700).
        /// </summary>
        Bold = DWRITE_FONT_WEIGHT_BOLD,

        /// <summary>
        /// Predefined font weight : Extra-bold (800).
        /// </summary>
        ExtraBold = DWRITE_FONT_WEIGHT_EXTRA_BOLD,

        /// <summary>
        /// Predefined font weight : Ultra-bold (800).
        /// </summary>
        UltraBold = DWRITE_FONT_WEIGHT_ULTRA_BOLD,

        /// <summary>
        /// Predefined font weight : Black (900).
        /// </summary>
        Black = DWRITE_FONT_WEIGHT_BLACK,

        /// <summary>
        /// Predefined font weight : Heavy (900).
        /// </summary>
        Heavy = DWRITE_FONT_WEIGHT_HEAVY,

        /// <summary>
        /// Predefined font weight : Extra-black (950).
        /// </summary>
        ExtraBlack = DWRITE_FONT_WEIGHT_EXTRA_BLACK,

        /// <summary>
        /// Predefined font weight : Ultra-black (950).
        /// </summary>
        UltraBlack = DWRITE_FONT_WEIGHT_ULTRA_BLACK
    };

    /// <summary>
    /// The font stretch enumeration describes relative change from the normal aspect ratio
    /// as specified by a font designer for the glyphs in a font.
    /// Values less than 1 or greater than 9 are considered to be invalid, and they are rejected by font API functions.
    /// </summary>
    public enum class FontStretch
    {
        /// <summary>
        /// Predefined font stretch : Not known (0).
        /// </summary>
        Undefined = DWRITE_FONT_STRETCH_UNDEFINED,

        /// <summary>
        /// Predefined font stretch : Ultra-condensed (1).
        /// </summary>
        UltraCondensed = DWRITE_FONT_STRETCH_ULTRA_CONDENSED,

        /// <summary>
        /// Predefined font stretch : Extra-condensed (2).
        /// </summary>
        ExtraCondensed = DWRITE_FONT_STRETCH_EXTRA_CONDENSED,

        /// <summary>
        /// Predefined font stretch : Condensed (3).
        /// </summary>
        Condensed = DWRITE_FONT_STRETCH_CONDENSED,

        /// <summary>
        /// Predefined font stretch : Semi-condensed (4).
        /// </summary>
        SemiCondensed = DWRITE_FONT_STRETCH_SEMI_CONDENSED,

        /// <summary>
        /// Predefined font stretch : Normal (5).
        /// </summary>
        Normal = DWRITE_FONT_STRETCH_NORMAL,

        /// <summary>
        /// Predefined font stretch : Medium (5).
        /// </summary>
        Medium = DWRITE_FONT_STRETCH_MEDIUM,

        /// <summary>
        /// Predefined font stretch : Semi-expanded (6).
        /// </summary>
        SemiExpanded = DWRITE_FONT_STRETCH_SEMI_EXPANDED,

        /// <summary>
        /// Predefined font stretch : Expanded (7).
        /// </summary>
        Expanded = DWRITE_FONT_STRETCH_EXPANDED,

        /// <summary>
        /// Predefined font stretch : Extra-expanded (8).
        /// </summary>
        ExtraExpanded = DWRITE_FONT_STRETCH_EXTRA_EXPANDED,

        /// <summary>
        /// Predefined font stretch : Ultra-expanded (9).
        /// </summary>
        UltraExpanded = DWRITE_FONT_STRETCH_ULTRA_EXPANDED
    };

    /// <summary>
    /// The font style enumeration describes the slope style of a font face, such as Normal, Italic or Oblique.
    /// Values other than the ones defined in the enumeration are considered to be invalid, and they are rejected by font API functions.
    /// </summary>
    public enum class FontStyle
    {
        /// <summary>
        /// Font slope style : Normal.
        /// </summary>
        Normal = DWRITE_FONT_STYLE_NORMAL,

        /// <summary>
        /// Font slope style : Oblique.
        /// </summary>
        Oblique = DWRITE_FONT_STYLE_OBLIQUE,

        /// <summary>
        /// Font slope style : Italic.
        /// </summary>
        Italic = DWRITE_FONT_STYLE_ITALIC

    };

    /// <summary>
    /// Direction for how reading progresses.
    /// </summary>
    public enum class ReadingDirection
    {
        /// <summary>
        /// Reading progresses from left to right.
        /// </summary>
        LeftToRight = DWRITE_READING_DIRECTION_LEFT_TO_RIGHT,

        /// <summary>
        /// Reading progresses from right to left.
        /// </summary>
        RightToLeft = DWRITE_READING_DIRECTION_RIGHT_TO_LEFT
    };

    /// <summary>
    /// Direction for how lines of text are placed relative to one another.
    /// </summary>
    public enum class FlowDirection
    {
        /// <summary>
        /// Text lines are placed from top to bottom.
        /// </summary>
        TopToBottom = DWRITE_FLOW_DIRECTION_TOP_TO_BOTTOM
    };

    /// <summary>
    /// Alignment of paragraph text along the reading direction axis relative to 
    /// the leading and trailing edge of the layout box.
    /// </summary>
    public enum class TextAlignment
    {
        /// <summary>
        /// The leading edge of the paragraph text is aligned to the layout box's leading edge.
        /// </summary>
        Leading = DWRITE_TEXT_ALIGNMENT_LEADING,

        /// <summary>
        /// The trailing edge of the paragraph text is aligned to the layout box's trailing edge.
        /// </summary>
        Trailing = DWRITE_TEXT_ALIGNMENT_TRAILING,

        /// <summary>
        /// The center of the paragraph text is aligned to the center of the layout box.
        /// </summary>
        Center = DWRITE_TEXT_ALIGNMENT_CENTER
    };

    /// <summary>
    /// Alignment of paragraph text along the flow direction axis relative to the
    /// flow's beginning and ending edge of the layout box.
    /// </summary>
    public enum class ParagraphAlignment
    {
        /// <summary>
        /// The first line of paragraph is aligned to the flow's beginning edge of the layout box.
        /// </summary>
        Near = DWRITE_PARAGRAPH_ALIGNMENT_NEAR,

        /// <summary>
        /// The last line of paragraph is aligned to the flow's ending edge of the layout box.
        /// </summary>
        Far = DWRITE_PARAGRAPH_ALIGNMENT_FAR,

        /// <summary>
        /// The center of the paragraph is aligned to the center of the flow of the layout box.
        /// </summary>
        Center = DWRITE_PARAGRAPH_ALIGNMENT_CENTER
    };

    /// <summary>
    /// Word wrapping in multiline paragraph.
    /// </summary>
    public enum class WordWrapping
    {
        /// <summary>
        /// Words are broken across lines to avoid text overflowing the layout box.
        /// </summary>
        Wrap = DWRITE_WORD_WRAPPING_WRAP,

        /// <summary>
        /// Words are kept within the same line even when it overflows the layout box.
        /// This option is often used with scrolling to reveal overflow text. 
        /// </summary>
        NoWrap = DWRITE_WORD_WRAPPING_NO_WRAP
    };

    /// <summary>
    /// The method used for line spacing in layout.
    /// </summary>
    public enum class LineSpacingMethod
    {
        /// <summary>
        /// Line spacing depends solely on the content, growing to accomodate the size of fonts and inline objects.
        /// </summary>
        Default = DWRITE_LINE_SPACING_METHOD_DEFAULT,

        /// <summary>
        /// Lines are explicitly set to uniform spacing, regardless of contained font sizes.
        /// This can be useful to avoid the uneven appearance that can occur from font fallback.
        /// </summary>
        Uniform = DWRITE_LINE_SPACING_METHOD_UNIFORM
    };

    /// <summary>
    /// Text granularity used to trim text overflowing the layout box.
    /// </summary>
    public enum class TrimmingGranularity
    {
        /// <summary>
        /// No trimming occurs. Text flows beyond the layout width.
        /// </summary>
        None = DWRITE_TRIMMING_GRANULARITY_NONE,

        /// <summary>
        /// Trimming occurs at character cluster boundary.
        /// </summary>
        Character = DWRITE_TRIMMING_GRANULARITY_CHARACTER,

        /// <summary>
        /// Trimming occurs at word boundary.
        /// </summary>
        Word = DWRITE_TRIMMING_GRANULARITY_WORD    
    };

    /// <summary>
    /// The measuring method used for text layout.
    /// </summary>
    public enum class MeasuringMode
    {
        /// <summary>
        /// Text is measured using glyph ideal metrics whose values are independent to the current display resolution.
        /// </summary>
        Natural = DWRITE_MEASURING_MODE_NATURAL,

        /// <summary>
        /// Text is measured using glyph display compatible metrics whose values tuned for the current display resolution.
        /// </summary>
        GdiClassic = DWRITE_MEASURING_MODE_GDI_CLASSIC,

        /// <summary>
        /// Text is measured using the same glyph display metrics as text measured by GDI using a font
        /// created with CLEARTYPE_NATURAL_QUALITY.
        /// </summary>
        GdiNatural = DWRITE_MEASURING_MODE_GDI_NATURAL,
    };


    /// <summary>
    /// Represents the internal structure of a device pixel (i.e., the physical arrangement of red,
    /// green, and blue color components) that is assumed for purposes of rendering text.
    /// </summary>
    public enum class PixelGeometry
    {
        /// <summary>
        /// The red, green, and blue color components of each pixel are assumed to occupy the same point.
        /// </summary>
        Flat = DWRITE_PIXEL_GEOMETRY_FLAT,

        /// <summary>
        /// Each pixel comprises three vertical stripes, with red on the left, green in the center, and 
        /// blue on the right. This is the most common pixel geometry for LCD monitors.
        /// </summary>
        Rgb = DWRITE_PIXEL_GEOMETRY_RGB,

        /// <summary>
        /// Each pixel comprises three vertical stripes, with blue on the left, green in the center, and 
        /// red on the right.
        /// </summary>
        Bgr = DWRITE_PIXEL_GEOMETRY_BGR
    };

    /// <summary>
    /// Represents a method of rendering glyphs.
    /// </summary>
    public enum class RenderingMode
    {
        /// <summary>
        /// Specifies that the rendering mode is determined automatically based on the font and size.
        /// </summary>
        Default = DWRITE_RENDERING_MODE_DEFAULT,

        /// <summary>
        /// Specifies that no anti-aliasing is performed. Each pixel is either set to the foreground 
        /// color of the text or retains the color of the background.
        /// </summary>
        Aliased = DWRITE_RENDERING_MODE_ALIASED,

        /// <summary>
        /// Specifies ClearType rendering with the same metrics as aliased text. Glyphs can only
        /// be positioned on whole-pixel boundaries.
        /// </summary>
        ClearTypeGdiClassic = DWRITE_RENDERING_MODE_CLEARTYPE_GDI_CLASSIC,

        /// <summary>
        /// Specifies ClearType rendering with the same metrics as text rendering using GDI using a font
        /// created with CLEARTYPE_NATURAL_QUALITY. Glyph metrics are closer to their ideal values than 
        /// with aliased text, but glyphs are still positioned on whole-pixel boundaries.
        /// </summary>
        ClearTypeGdiNatural = DWRITE_RENDERING_MODE_CLEARTYPE_GDI_NATURAL,

        /// <summary>
        /// Specifies ClearType rendering with anti-aliasing in the horizontal dimension only. This is 
        /// typically used with small to medium font sizes (up to 16 ppem).
        /// </summary>
        ClearTypeNatural = DWRITE_RENDERING_MODE_CLEARTYPE_NATURAL,

        /// <summary>
        /// Specifies ClearType rendering with anti-aliasing in both horizontal and vertical dimensions. 
        /// This is typically used at larger sizes to makes curves and diagonal lines look smoother, at 
        /// the expense of some softness.
        /// </summary>
        ClearTypeNaturalSymmetric = DWRITE_RENDERING_MODE_CLEARTYPE_NATURAL_SYMMETRIC,

        /// <summary>
        /// Specifies that rendering should bypass the rasterizer and use the outlines directly. This is 
        /// typically used at very large sizes.
        /// </summary>
        Outline = DWRITE_RENDERING_MODE_OUTLINE
    };


    /// <summary>
    /// The informational string enumeration identifies a string in a font.
    /// </summary>
    public enum class InformationalStringId
    {
        /// <summary>
        /// Unspecified name ID.
        /// </summary>
        None = DWRITE_INFORMATIONAL_STRING_NONE,

        /// <summary>
        /// Copyright notice provided by the font.
        /// </summary>
        CopyrightNotice = DWRITE_INFORMATIONAL_STRING_COPYRIGHT_NOTICE,

        /// <summary>
        /// String containing a version number.
        /// </summary>
        VersionStrings = DWRITE_INFORMATIONAL_STRING_VERSION_STRINGS,

        /// <summary>
        /// Trademark information provided by the font.
        /// </summary>
        Trademark = DWRITE_INFORMATIONAL_STRING_TRADEMARK,

        /// <summary>
        /// Name of the font manufacturer.
        /// </summary>
        Manufacturer = DWRITE_INFORMATIONAL_STRING_MANUFACTURER,

        /// <summary>
        /// Name of the font designer.
        /// </summary>
        Designer = DWRITE_INFORMATIONAL_STRING_DESIGNER,

        /// <summary>
        /// URL of font designer (with protocol, e.g., http://, ftp://).
        /// </summary>
        DesignerUrl = DWRITE_INFORMATIONAL_STRING_DESIGNER_URL,

        /// <summary>
        /// Description of the font. Can contain revision information, usage recommendations, history, features, etc.
        /// </summary>
        Description = DWRITE_INFORMATIONAL_STRING_DESCRIPTION,

        /// <summary>
        /// URL of font vendor (with protocol, e.g., http://, ftp://). If a unique serial number is embedded in the URL, it can be used to register the font.
        /// </summary>
        FontVendorUrl = DWRITE_INFORMATIONAL_STRING_FONT_VENDOR_URL,

        /// <summary>
        /// Description of how the font may be legally used, or different example scenarios for licensed use. This field should be written in plain language, not legalese.
        /// </summary>
        LicenseDescription = DWRITE_INFORMATIONAL_STRING_LICENSE_DESCRIPTION,

        /// <summary>
        /// URL where additional licensing information can be found.
        /// </summary>
        LicenseInfoUrl = DWRITE_INFORMATIONAL_STRING_LICENSE_INFO_URL,

        /// <summary>
        /// GDI-compatible family name. Because GDI allows a maximum of four fonts per family, fonts in the same family may have different GDI-compatible family names
        /// (e.g., "Arial", "Arial Narrow", "Arial Black").
        /// </summary>
        Win32FamilyNames = DWRITE_INFORMATIONAL_STRING_WIN32_FAMILY_NAMES,

        /// <summary>
        /// GDI-compatible subfamily name.
        /// </summary>
        Win32SubfamilyNames = DWRITE_INFORMATIONAL_STRING_WIN32_SUBFAMILY_NAMES,

        /// <summary>
        /// Family name preferred by the designer. This enables font designers to group more than four fonts in a single family without losing compatibility with
        /// GDI. This name is typically only present if it differs from the GDI-compatible family name.
        /// </summary>
        PreferredFamilyNames = DWRITE_INFORMATIONAL_STRING_PREFERRED_FAMILY_NAMES,

        /// <summary>
        /// Subfamily name preferred by the designer. This name is typically only present if it differs from the GDI-compatible subfamily name. 
        /// </summary>
        PreferredSubfamilyNames = DWRITE_INFORMATIONAL_STRING_PREFERRED_SUBFAMILY_NAMES,

        /// <summary>
        /// Sample text. This can be the font name or any other text that the designer thinks is the best example to display the font in.
        /// </summary>
        SampleText = DWRITE_INFORMATIONAL_STRING_SAMPLE_TEXT
    };


    /// <summary>
    /// Specifies algorithmic style simulations to be applied to the font face.
    /// Bold and oblique simulations can be combined via bitwise OR operation.
    /// </summary>
    [Flags]
    public enum class FontSimulations
    {
        /// <summary>
        /// No simulations are performed.
        /// </summary>
        None = DWRITE_FONT_SIMULATIONS_NONE,

        /// <summary>
        /// Algorithmic emboldening is performed.
        /// </summary>
        Bold = DWRITE_FONT_SIMULATIONS_BOLD,

        /// <summary>
        /// Algorithmic italicization is performed.
        /// </summary>
        Oblique = DWRITE_FONT_SIMULATIONS_OBLIQUE
    };

    /// <summary>
    /// Condition at the edges of inline object or text used to determine
    /// line-breaking behavior.
    /// </summary>
    public enum class BreakCondition
    {
        /// <summary>
        /// Whether a break is allowed is determined by the condition of the
        /// neighboring text span or inline object.
        /// </summary>
        Neutral = DWRITE_BREAK_CONDITION_NEUTRAL,

        /// <summary>
        /// A break is allowed, unless overruled by the condition of the
        /// neighboring text span or inline object, either prohibited by a
        /// May Not or forced by a Must.
        /// </summary>
        CanBreak = DWRITE_BREAK_CONDITION_CAN_BREAK,

        /// <summary>
        /// There should be no break, unless overruled by a Must condition from
        /// the neighboring text span or inline object.
        /// </summary>
        MayNotBreak = DWRITE_BREAK_CONDITION_MAY_NOT_BREAK,

        /// <summary>
        /// The break must happen, regardless of the condition of the adjacent
        /// text span or inline object.
        /// </summary>
        MustBreak = DWRITE_BREAK_CONDITION_MUST_BREAK
    };


    /// <summary>
    /// Typographic feature of text supplied by the font.
    /// </summary>
    public enum class FontFeatureTag
    {
        /// <summary>
        /// Alternative Fractions
        /// </summary>
        AlternativeFractions  =  DWRITE_FONT_FEATURE_TAG_ALTERNATIVE_FRACTIONS,
        /// <summary>
        /// Petite Capitals From Capitals
        /// </summary>
        PetiteCapitalsFromCapitals  =  DWRITE_FONT_FEATURE_TAG_PETITE_CAPITALS_FROM_CAPITALS,
        /// <summary>
        /// Small Capitals From Capitals
        /// </summary>
        SmallCapitalsFromCapitals  =  DWRITE_FONT_FEATURE_TAG_SMALL_CAPITALS_FROM_CAPITALS,
        /// <summary>
        /// Contextual Alternates
        /// </summary>
        ContextualAlternates  =  DWRITE_FONT_FEATURE_TAG_CONTEXTUAL_ALTERNATES,
        /// <summary>
        /// Case Sensitive Forms
        /// </summary>
        CaseSensitiveForms  =  DWRITE_FONT_FEATURE_TAG_CASE_SENSITIVE_FORMS,
        /// <summary>
        /// Glyph Composition Decomposition
        /// </summary>
        GlyphCompositionDecomposition  =  DWRITE_FONT_FEATURE_TAG_GLYPH_COMPOSITION_DECOMPOSITION,
        /// <summary>
        /// Contextual Ligatures
        /// </summary>
        ContextualLigatures  =  DWRITE_FONT_FEATURE_TAG_CONTEXTUAL_LIGATURES,
        /// <summary>
        /// Capital Spacing
        /// </summary>
        CapitalSpacing  =  DWRITE_FONT_FEATURE_TAG_CAPITAL_SPACING,
        /// <summary>
        /// Contextual Swash
        /// </summary>
        ContextualSwash  =  DWRITE_FONT_FEATURE_TAG_CONTEXTUAL_SWASH,
        /// <summary>
        /// Cursive Positioning
        /// </summary>
        CursivePositioning  =  DWRITE_FONT_FEATURE_TAG_CURSIVE_POSITIONING,
        /// <summary>
        /// Discretionary Ligatures
        /// </summary>
        DiscretionaryLigatures  =  DWRITE_FONT_FEATURE_TAG_DISCRETIONARY_LIGATURES,
        /// <summary>
        /// Expert Forms
        /// </summary>
        ExpertForms  =  DWRITE_FONT_FEATURE_TAG_EXPERT_FORMS,
        /// <summary>
        /// Fractions
        /// </summary>
        Fractions  =  DWRITE_FONT_FEATURE_TAG_FRACTIONS,
        /// <summary>
        /// Full Width
        /// </summary>
        FullWidth  =  DWRITE_FONT_FEATURE_TAG_FULL_WIDTH,
        /// <summary>
        /// Half Forms
        /// </summary>
        HalfForms  =  DWRITE_FONT_FEATURE_TAG_HALF_FORMS,
        /// <summary>
        /// Halant Forms
        /// </summary>
        HalantForms  =  DWRITE_FONT_FEATURE_TAG_HALANT_FORMS,
        /// <summary>
        /// Alternate Half Width
        /// </summary>
        AlternateHalfWidth  =  DWRITE_FONT_FEATURE_TAG_ALTERNATE_HALF_WIDTH,
        /// <summary>
        /// Historical Forms 
        /// </summary>
        HistoricalForms  =  DWRITE_FONT_FEATURE_TAG_HISTORICAL_FORMS,
        /// <summary>
        /// Horizontal Kana Alternates
        /// </summary>
        HorizontalKanaAlternates  =  DWRITE_FONT_FEATURE_TAG_HORIZONTAL_KANA_ALTERNATES,
        /// <summary>
        /// Historical Ligatures
        /// </summary>
        HistoricalLigatures  =  DWRITE_FONT_FEATURE_TAG_HISTORICAL_LIGATURES,
        /// <summary>
        /// Half Width
        /// </summary>
        HalfWidth  =  DWRITE_FONT_FEATURE_TAG_HALF_WIDTH,
        /// <summary>
        /// Hojo Kanji Forms
        /// </summary>
        HojoKanjiForms  =  DWRITE_FONT_FEATURE_TAG_HOJO_KANJI_FORMS,
        /// <summary>
        /// Jis04 Forms
        /// </summary>
        Jis04Forms  =  DWRITE_FONT_FEATURE_TAG_JIS04_FORMS,
        /// <summary>
        /// Jis78 Forms
        /// </summary>
        Jis78Forms  =  DWRITE_FONT_FEATURE_TAG_JIS78_FORMS,
        /// <summary>
        /// Jis83 Forms
        /// </summary>
        Jis83Forms  =  DWRITE_FONT_FEATURE_TAG_JIS83_FORMS,
        /// <summary>
        /// Jis90 Forms
        /// </summary>
        Jis90Forms  =  DWRITE_FONT_FEATURE_TAG_JIS90_FORMS,
        /// <summary>
        /// Kerning
        /// </summary>
        Kerning  =  DWRITE_FONT_FEATURE_TAG_KERNING,
        /// <summary>
        /// Standard Ligatures
        /// </summary>
        StandardLigatures  =  DWRITE_FONT_FEATURE_TAG_STANDARD_LIGATURES,
        /// <summary>
        /// Lining Figures
        /// </summary>
        LiningFigures  =  DWRITE_FONT_FEATURE_TAG_LINING_FIGURES,
        /// <summary>
        /// Localized Forms
        /// </summary>
        LocalizedForms  =  DWRITE_FONT_FEATURE_TAG_LOCALIZED_FORMS,
        /// <summary>
        /// Mark Positioning
        /// </summary>
        MarkPositioning  =  DWRITE_FONT_FEATURE_TAG_MARK_POSITIONING,
        /// <summary>
        /// Mathematical Greek
        /// </summary>
        MathematicalGreek  =  DWRITE_FONT_FEATURE_TAG_MATHEMATICAL_GREEK,
        /// <summary>
        /// Mark To Mark Positioning
        /// </summary>
        MarkToMarkPositioning  =  DWRITE_FONT_FEATURE_TAG_MARK_TO_MARK_POSITIONING,
        /// <summary>
        /// Alternate Annotation Forms
        /// </summary>
        AlternateAnnotationForms  =  DWRITE_FONT_FEATURE_TAG_ALTERNATE_ANNOTATION_FORMS,
        /// <summary>
        /// Nlc Kanji Forms
        /// </summary>
        NLCKanjiForms  =  DWRITE_FONT_FEATURE_TAG_NLC_KANJI_FORMS,
        /// <summary>
        /// Old Style Figures
        /// </summary>
        OldStyleFigures  =  DWRITE_FONT_FEATURE_TAG_OLD_STYLE_FIGURES,
        /// <summary>
        /// Ordinals
        /// </summary>
        Ordinals  =  DWRITE_FONT_FEATURE_TAG_ORDINALS,
        /// <summary>
        /// Proportional Alternate Width
        /// </summary>
        ProportionalAlternateWidth  =  DWRITE_FONT_FEATURE_TAG_PROPORTIONAL_ALTERNATE_WIDTH,
        /// <summary>
        /// Petite Capitals
        /// </summary>
        PetiteCapitals  =  DWRITE_FONT_FEATURE_TAG_PETITE_CAPITALS,
        /// <summary>
        /// Proportional Figures
        /// </summary>
        ProportionalFigures  =  DWRITE_FONT_FEATURE_TAG_PROPORTIONAL_FIGURES,
        /// <summary>
        /// Proportional Widths
        /// </summary>
        ProportionalWidths  =  DWRITE_FONT_FEATURE_TAG_PROPORTIONAL_WIDTHS,
        /// <summary>
        /// Quarter Widths
        /// </summary>
        QuarterWidths  =  DWRITE_FONT_FEATURE_TAG_QUARTER_WIDTHS,
        /// <summary>
        /// Required Ligatures
        /// </summary>
        RequiredLigatures  =  DWRITE_FONT_FEATURE_TAG_REQUIRED_LIGATURES,
        /// <summary>
        /// Ruby Notation Forms
        /// </summary>
        RubyNotationForms  =  DWRITE_FONT_FEATURE_TAG_RUBY_NOTATION_FORMS,
        /// <summary>
        /// Stylistic Alternates
        /// </summary>
        StylisticAlternates  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_ALTERNATES,
        /// <summary>
        /// Scientific Inferiors
        /// </summary>
        ScientificInferiors  =  DWRITE_FONT_FEATURE_TAG_SCIENTIFIC_INFERIORS,
        /// <summary>
        /// Small Capitals
        /// </summary>
        SmallCapitals  =  DWRITE_FONT_FEATURE_TAG_SMALL_CAPITALS,
        /// <summary>
        /// Simplified Forms
        /// </summary>
        SimplifiedForms  =  DWRITE_FONT_FEATURE_TAG_SIMPLIFIED_FORMS,
        /// <summary>
        /// Stylistic Set 1
        /// </summary>
        StylisticSet01  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_1,
        /// <summary>
        /// Stylistic Set 2
        /// </summary>
        StylisticSet02  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_2,
        /// <summary>
        /// Stylistic Set 3
        /// </summary>
        StylisticSet03  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_3,
        /// <summary>
        /// Stylistic Set 4
        /// </summary>
        StylisticSet04  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_4,
        /// <summary>
        /// Stylistic Set 5
        /// </summary>
        StylisticSet05  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_5,
        /// <summary>
        /// Stylistic Set 6
        /// </summary>
        StylisticSet06  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_6,
        /// <summary>
        /// Stylistic Set 7
        /// </summary>
        StylisticSet07  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_7,
        /// <summary>
        /// Stylistic Set 8
        /// </summary>
        StylisticSet08  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_8,
        /// <summary>
        /// Stylistic Set 9
        /// </summary>
        StylisticSet09  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_9,
        /// <summary>
        /// Stylistic Set 10
        /// </summary>
        StylisticSet10  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_10,
        /// <summary>
        /// Stylistic Set 11
        /// </summary>
        StylisticSet11  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_11,
        /// <summary>
        /// Stylistic Set 12
        /// </summary>
        StylisticSet12  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_12,
        /// <summary>
        /// Stylistic Set 13
        /// </summary>
        StylisticSet13  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_13,
        /// <summary>
        /// Stylistic Set 14
        /// </summary>
        StylisticSet14  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_14,
        /// <summary>
        /// Stylistic Set 15
        /// </summary>
        StylisticSet15  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_15,
        /// <summary>
        /// Stylistic Set 16
        /// </summary>
        StylisticSet16  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_16,
        /// <summary>
        /// Stylistic Set 17
        /// </summary>
        StylisticSet17  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_17,
        /// <summary>
        /// Stylistic Set 18
        /// </summary>
        StylisticSet18  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_18,
        /// <summary>
        /// Stylistic Set 19
        /// </summary>
        StylisticSet19  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_19,
        /// <summary>
        /// Stylistic Set 20
        /// </summary>
        StylisticSet20  =  DWRITE_FONT_FEATURE_TAG_STYLISTIC_SET_20,
        /// <summary>
        /// Subscript
        /// </summary>
        Subscript  =  DWRITE_FONT_FEATURE_TAG_SUBSCRIPT,
        /// <summary>
        /// Superscript
        /// </summary>
        Superscript  =  DWRITE_FONT_FEATURE_TAG_SUPERSCRIPT,
        /// <summary>
        /// Swash
        /// </summary>
        Swash  =  DWRITE_FONT_FEATURE_TAG_SWASH,
        /// <summary>
        /// Titling
        /// </summary>
        Titling  =  DWRITE_FONT_FEATURE_TAG_TITLING,
        /// <summary>
        /// Traditional Name Forms  
        /// </summary>
        TraditionalNameForms  =  DWRITE_FONT_FEATURE_TAG_TRADITIONAL_NAME_FORMS,
        /// <summary>
        /// Tabular Figures 
        /// </summary>
        TabularFigures  =  DWRITE_FONT_FEATURE_TAG_TABULAR_FIGURES,
        /// <summary>
        /// Traditional Forms  
        /// </summary>
        TraditionalForms  =  DWRITE_FONT_FEATURE_TAG_TRADITIONAL_FORMS,
        /// <summary>
        /// Third Widths 
        /// </summary>
        ThirdWidths  =  DWRITE_FONT_FEATURE_TAG_THIRD_WIDTHS,
        /// <summary>
        /// Unicase
        /// </summary>
        Unicase  =  DWRITE_FONT_FEATURE_TAG_UNICASE,
        /// <summary>
        /// Slashed Zero
        /// </summary>
        SlashedZero  =  DWRITE_FONT_FEATURE_TAG_SLASHED_ZERO,
    }; 



    /// <summary>
    /// The type of a font represented by a single font file.
    /// Font formats that consist of multiple files, e.g. Type 1 .PFM and .PFB, have
    /// separate enum values for each of the file type.
    /// </summary>
    public enum class FontFileType
    {
        /// <summary>
        /// Font type is not recognized by the DirectWrite font system.
        /// </summary>
        Unknown = DWRITE_FONT_FILE_TYPE_UNKNOWN,

        /// <summary>
        /// OpenType font with Compact Font Format outlines.
        /// </summary>
        CompactFontFormat = DWRITE_FONT_FILE_TYPE_CFF,

        /// <summary>
        /// OpenType font with TrueType outlines.
        /// </summary>
        TrueType = DWRITE_FONT_FILE_TYPE_TRUETYPE,

        /// <summary>
        /// OpenType font that contains a TrueType collection.
        /// </summary>
        TrueTypeCollection = DWRITE_FONT_FILE_TYPE_TRUETYPE_COLLECTION,

        /// <summary>
        /// Type 1 PFM font.
        /// </summary>
        Type1PrinterFontMetric = DWRITE_FONT_FILE_TYPE_TYPE1_PFM,

        /// <summary>
        /// Type 1 PFB font.
        /// </summary>
        Type1PrinterFontBinary = DWRITE_FONT_FILE_TYPE_TYPE1_PFB,

        /// <summary>
        /// Vector .FON font.
        /// </summary>
        Vector = DWRITE_FONT_FILE_TYPE_VECTOR,

        /// <summary>
        /// Bitmap .FON font.
        /// </summary>
        Bitmap = DWRITE_FONT_FILE_TYPE_BITMAP
    };

    /// <summary>
    /// The file format of a complete font face.
    /// Font formats that consist of multiple files, e.g. Type 1 .PFM and .PFB, have
    /// a single enum entry.
    /// </summary>
    public enum class FontFaceType
    {
        /// <summary>
        /// OpenType font face with Compact Font Format outlines.
        /// </summary>
        CompactFontFormat = DWRITE_FONT_FACE_TYPE_CFF,

        /// <summary>
        /// OpenType font face with TrueType outlines.
        /// </summary>
        TrueType = DWRITE_FONT_FACE_TYPE_TRUETYPE,

        /// <summary>
        /// OpenType font face that is a part of a TrueType collection.
        /// </summary>
        TrueTypeCollection = DWRITE_FONT_FACE_TYPE_TRUETYPE_COLLECTION,

        /// <summary>
        /// A Type 1 font face.
        /// </summary>
        Type1 = DWRITE_FONT_FACE_TYPE_TYPE1,

        /// <summary>
        /// A vector .FON format font face.
        /// </summary>
        Vector = DWRITE_FONT_FACE_TYPE_VECTOR,

        /// <summary>
        /// A bitmap .FON format font face.
        /// </summary>
        Bitmap = DWRITE_FONT_FACE_TYPE_BITMAP,

        /// <summary>
        /// Font face type is not recognized by the DirectWrite font system.
        /// </summary>
        Unknown = DWRITE_FONT_FACE_TYPE_UNKNOWN
    };

} } } }