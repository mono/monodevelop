//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "DWriteEnums.h"
#include "DWriteStructs.h"

using namespace System::Globalization;
using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {

    ref class TextFormat;
    ref class TextLayout;
    ref class RenderingParams;
    ref class FontFamily;
    ref class InlineObject;
    ref class TypographySettingCollection;
    ref class FontFamilyCollection;
    ref class FontFace;
    interface class ICustomInlineObject;

    /// <summary>
    /// The root factory interface for all DWrite objects.
    /// <para>(Also see DirectX SDK: IDWriteFactory)</para>
    /// </summary>
    public ref class DWriteFactory : public DirectUnknown
    {
    public: 
        /// <summary>
        /// Creates a DirectWrite factory object that is used for subsequent creation of individual DirectWrite objects.
        /// This method uses Shared FactoryType.
        /// <para>(Also see DirectX SDK: DWriteCreateFactory)</para>
        /// </summary>
        /// <returns>The newly created DirectWrite factory object</returns>
        static DWriteFactory^ CreateFactory();

        /// <summary>
        /// Creates a DirectWrite factory object that is used for subsequent creation of individual DirectWrite objects.
        /// <para>(Also see DirectX SDK: DWriteCreateFactory)</para>
        /// </summary>
        /// <param name="factoryType">Specifies whether the factory object will be shared or isolated.</param>
        /// <returns>The newly created DirectWrite factory object</returns>
        static DWriteFactory^ CreateFactory(DWriteFactoryType factoryType);

        /// <summary>
        /// Create a text format object used for text layout.
        /// </summary>
        /// <param name="fontFamilyName">Name of the font family</param>
        /// <param name="fontSize">Logical size of the font in DIP units. A DIP ("device-independent pixel") equals 1/96 inch.</param>
        /// <param name="fontWeight">Font weight</param>
        /// <param name="fontStyle">Font style</param>
        /// <param name="fontStretch">Font stretch</param>
        /// <param name="cultureInfo">Culture info</param>
        /// <returns>Newly created text format object, or null in case of failure.</returns>
        TextFormat^ CreateTextFormat(
            String^ fontFamilyName,
            Single fontSize,
            FontWeight fontWeight,
            FontStyle fontStyle,
            FontStretch fontStretch,
            CultureInfo^ cultureInfo);

        /// <summary>
        /// Create a text format object used for text layout
        /// </summary>
        /// <param name="fontFamilyName">Name of the font family</param>
        /// <param name="fontSize">Logical size of the font in DIP units. A DIP ("device-independent pixel") equals 1/96 inch.</param>
        /// <param name="fontWeight">Font weight</param>
        /// <param name="fontStyle">Font style</param>
        /// <param name="fontStretch">Font stretch</param>
        /// <returns>Newly created text format object, or null in case of failure.</returns>
        TextFormat^ CreateTextFormat(
            String^ fontFamilyName,
            Single fontSize,
            FontWeight fontWeight,
            FontStyle fontStyle,
            FontStretch fontStretch);

        /// <summary>
        /// Create a text format object used for text layout using default font weight, style and stretch.
        /// </summary>
        /// <param name="fontFamilyName">Name of the font family</param>
        /// <param name="fontSize">Logical size of the font in DIP units. A DIP ("device-independent pixel") equals 1/96 inch.</param>
        /// <returns>Newly created text format object, or null in case of failure.</returns>
        TextFormat^ CreateTextFormat(
            String^ fontFamilyName,
            Single fontSize);

        /// <summary>
        /// Creates a rendering parameters object with default settings for the primary monitor. 
        /// </summary>
        /// <returns>The newly created rendering parameters object, or NULL in case of failure.</returns>
        RenderingParams^ CreateRenderingParams();

        /// <summary>
        /// Creates a rendering parameters object with default settings for the specified monitor. 
        /// </summary>
        /// <param name="monitorHandle">A handle for the specified monitor.</param>
        /// <remarks>To obtain a handle to a monitor use Graphics Output.Description property or
        /// Win32 MonitorFromWindow() API to obtain a handle for the desired monitor.</remarks>
        /// <returns>The newly created rendering parameters object, or NULL in case of failure.</returns>
        RenderingParams^ CreateMonitorRenderingParams(
            IntPtr monitorHandle
            );

        /// <summary>
        /// Creates a rendering parameters object with the specified properties. 
        /// </summary>
        /// <param name="gamma">The gamma value used for gamma correction, which must be greater than zero and cannot exceed 256.</param>
        /// <param name="enhancedContrast">The amount of contrast enhancement, zero or greater.</param>
        /// <param name="clearTypeLevel">The degree of ClearType level, from 0.0f (no ClearType) to 1.0f (full ClearType).</param>
        /// <param name="pixelGeometry">The geometry of a device pixel.</param>
        /// <param name="renderingMode">Method of rendering glyphs. In most cases, this should be Default to automatically use an appropriate mode.</param>
        /// <returns>The newly created rendering parameters object, or NULL in case of failure.</returns>
        RenderingParams^ CreateCustomRenderingParams(
            FLOAT gamma,
            FLOAT enhancedContrast,
            FLOAT clearTypeLevel,
            PixelGeometry pixelGeometry,
            RenderingMode renderingMode);

    /// <summary>
    /// CreateTextLayout takes a string, format, and associated constraints
    /// and produces and object representing the fully analyzed
    /// and formatted result.
    /// </summary>
    /// <param name="text">Characters to layout.</param>
    /// <param name="textFormat">The format to apply to the string.</param>
    /// <param name="maxWidth">Width of the layout box.</param>
    /// <param name="maxHeight">Height of the layout box.</param>
    /// <returns>
    /// The resultant TextLayout object.
    /// </returns>
    TextLayout^ CreateTextLayout(
        String^ text,
        TextFormat^ textFormat,
        FLOAT maxWidth,
        FLOAT maxHeight
        );

    /// <summary>
    /// CreateGdiCompatibleTextLayout takes a string, format, and associated constraints
    /// and produces and object representing the result formatted for a particular display resolution
    /// and measuring method. The resulting text layout should only be used for the intended resolution,
    /// and for cases where text scalability is desired, CreateTextLayout should be used instead.
    /// </summary>
    /// <param name="text">The characters to layout.</param>
    /// <param name="textFormat">The format to apply to the string.</param>
    /// <param name="layoutWidth">Width of the layout box.</param>
    /// <param name="layoutHeight">Height of the layout box.</param>
    /// <param name="pixelsPerDip">Number of physical pixels per DIP. For example, if rendering onto a 96 DPI device then pixelsPerDip
    /// is 1. If rendering onto a 120 DPI device then pixelsPerDip is 120/96.</param>
    /// <param name="transform">Transform applied to the glyphs and their positions. This transform is applied after the
    /// scaling specified the font size and pixelsPerDip.</param>
    /// <param name="useGdiNatural">
    /// When set to False, instructs the text layout to use the same metrics as GDI aliased text.
    /// When set to True, instructs the text layout to use the same metrics as text measured by GDI using a font
    /// created with NaturalQuality.
    /// </param>
    /// <returns>
    /// The resultant TextLayout object.
    /// </returns>
    TextLayout^ CreateGdiCompatibleTextLayout(
        String^ text,
        TextFormat^ textFormat,
        FLOAT layoutWidth,
        FLOAT layoutHeight,
        FLOAT pixelsPerDip,
        Boolean useGdiNatural,
        Microsoft::WindowsAPICodePack::DirectX::Direct2D1::Matrix3x2F transform        
        );


    /// <summary>
    /// CreateGdiCompatibleTextLayout takes a string, format, and associated constraints
    /// and produces and object representing the result formatted for a particular display resolution
    /// and measuring method. The resulting text layout should only be used for the intended resolution,
    /// and for cases where text scalability is desired, CreateTextLayout should be used instead.
    /// </summary>
    /// <param name="text">The characters to layout.</param>
    /// <param name="textFormat">The format to apply to the string.</param>
    /// <param name="layoutWidth">Width of the layout box.</param>
    /// <param name="layoutHeight">Height of the layout box.</param>
    /// <param name="pixelsPerDip">Number of physical pixels per DIP. For example, if rendering onto a 96 DPI device then pixelsPerDip
    /// is 1. If rendering onto a 120 DPI device then pixelsPerDip is 120/96.</param>
    /// <param name="useGdiNatural">
    /// When set to False, instructs the text layout to use the same metrics as GDI aliased text.
    /// When set to True, instructs the text layout to use the same metrics as text measured by GDI using a font
    /// created with CleartypeNaturalQuality.
    /// </param>
    /// <returns>
    /// The resultant TextLayout object.
    /// </returns>
    TextLayout^ CreateGdiCompatibleTextLayout(
        String^ text,
        TextFormat^ textFormat,
        FLOAT layoutWidth,
        FLOAT layoutHeight,
        FLOAT pixelsPerDip,
        Boolean useGdiNatural
        );

    // REVIEW: return ICustomInlineObject here?

    /// <summary>
    /// The application may call this function to create an inline object for trimming, using an ellipsis as the omission sign.
    /// The ellipsis will be created using the current settings of the format, including base font, style, and any effects.
    /// Alternate omission signs can be created by the application by implementing ICustomInlineObject.
    /// </summary>
    /// <param name="textFormat">The format to use for settings.</param>
    /// <returns>
    /// Created omission sign.
    /// </returns>
    InlineObject^ CreateEllipsisTrimmingSign(
        TextFormat^ textFormat
        );

    /// <summary>
    /// Gets a font family collection representing the set of installed fonts.
    /// </summary>
    property FontFamilyCollection^ SystemFontFamilyCollection
    {
        FontFamilyCollection^ get();
    }

    /// <summary>
    /// Creates a typography object used in conjunction with text format for text layout.
    /// </summary>
    /// <returns>
    /// A TypographySettingCollection object.
    /// </returns>
    TypographySettingCollection^ CreateTypography();

    /// <summary>
    /// Creates a font face from a given TrueType font file.
    /// This method will load only a TrueType font file with no font simulations applied.
    /// </summary>
    /// <param name="fontFileName">The input font file.</param>
    /// <returns>
    /// A FontFace object.
    /// </returns>
    FontFace^ CreateFontFaceFromFontFile(System::String^ fontFileName);

    /// <summary>
    /// Creates a font face from a given a font file.
    /// </summary>
    /// <param name="fontFileName">The input font file.</param>
    /// <param name="fontFaceType">The file format of the font face.</param>
    /// <param name="faceIndex">The zero based index of a font face in cases when the font files contain a collection of font faces.
    /// If the font files contain a single face, this value should be zero.</param>
    /// <param name="faceSimulations">Font face simulation flags for algorithmic emboldening and italicization.</param>
    /// <returns>
    /// A FontFace object.
    /// </returns>
    FontFace^ CreateFontFaceFromFontFile(
        System::String^ fontFileName, 
        FontFaceType fontFaceType, 
        UINT32 faceIndex,
        FontSimulations faceSimulations);

    internal:
        DWriteFactory()
        { }
    
        DWriteFactory(IDWriteFactory* pNativeIDWriteFactory) : 
            DirectUnknown(pNativeIDWriteFactory)
        { }

    private:
        FontFamilyCollection^ m_sysFontFamilies;
        IDWriteFontCollection* m_fontCollection;
    };

} } } }
