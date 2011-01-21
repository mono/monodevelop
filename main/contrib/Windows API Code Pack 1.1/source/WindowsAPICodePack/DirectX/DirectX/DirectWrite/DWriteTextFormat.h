//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "DWriteEnums.h"
#include "DWriteStructs.h"

using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {


/// <summary>
/// The format of text used for text layout purpose.
/// <para>(Also see DirectX SDK: IDWriteTextFormat)</para>
/// </summary>
public ref class TextFormat : public DirectUnknown
{
public: 

    /// <summary>
    /// Get or Set alignment option of text relative to layout box's leading and trailing edge.
    /// </summary>
    property DirectWrite::TextAlignment TextAlignment
    {
        DirectWrite::TextAlignment get();
        void set (DirectWrite::TextAlignment);
    }

    /// <summary>
    /// Get or Set alignment option of paragraph relative to layout box's top and bottom edge.
    /// </summary>
    property DirectWrite::ParagraphAlignment ParagraphAlignment
    {
        DirectWrite::ParagraphAlignment get();
        void set (DirectWrite::ParagraphAlignment);
    }

    /// <summary>
    /// Get or Set word wrapping option.
    /// </summary>
    property DirectWrite::WordWrapping WordWrapping
    {
        DirectWrite::WordWrapping get();
        void set (DirectWrite::WordWrapping);
    }

    /// <summary>
    /// Get or Set paragraph reading direction.
    /// </summary>
    property DirectWrite::ReadingDirection ReadingDirection
    {
        DirectWrite::ReadingDirection get();
        void set (DirectWrite::ReadingDirection);
    }

    /// <summary>
    /// Get or Set paragraph flow direction.
    /// </summary>
    property DirectWrite::FlowDirection FlowDirection
    {
        DirectWrite::FlowDirection get();
        void set (DirectWrite::FlowDirection);
    }

    /// <summary>
    /// Set incremental tab stop position.
    /// </summary>
    property Single IncrementalTabStop
    {
        Single get();
        void set (Single);
    }

    /// <summary>
    /// Get or Set line spacing.
    /// </summary>
    property DirectWrite::LineSpacing LineSpacing
    {
        DirectWrite::LineSpacing get();
        void set (DirectWrite::LineSpacing);
    }

    /// <summary>
    /// Gets or sets trimming options.
    /// </summary>
    property DirectWrite::Trimming Trimming
    {
        DirectWrite::Trimming get();
        void set (DirectWrite::Trimming);
    }

    /// <summary>
    /// Gets and sets trimming options for text overflowing the layout width,
    /// including the trimming sign object.
    /// </summary>
    /// <param name="trimmingSign">Trimming omission sign.</param>
    /// <returns>
    /// Text trimming options.
    /// </returns>
    property DirectWrite::TrimmingWithSign TrimmingWithSign
    {
        DirectWrite::TrimmingWithSign get(void);
        void set(DirectWrite::TrimmingWithSign);
    }

    property String^ FontFamilyName
    {
        String^ get();
    }

    /// <summary>
    /// Get the font weight.
    /// </summary>
    property DirectWrite::FontWeight FontWeight
    {
        DirectWrite::FontWeight get();
    }

    /// <summary>
    /// Get the font style.
    /// </summary>
    property DirectWrite::FontStyle FontStyle
    {
        DirectWrite::FontStyle get();
    }

    /// <summary>
    /// Get the font stretch.
    /// </summary>
    property DirectWrite::FontStretch FontStretch
    {
        DirectWrite::FontStretch get();
    }

    /// <summary>
    /// Get the font em height.
    /// </summary>
    property Single FontSize
    {
        Single get();
    }

    /// <summary>
    /// Get the CultureInfo for this object.
    /// </summary>
    property System::Globalization::CultureInfo^ CultureInfo
    {
        System::Globalization::CultureInfo^ get();
    }

internal:

    TextFormat(void)
    { }

    TextFormat(IDWriteTextFormat* pNativeIDWriteTextFormat) : DirectUnknown(pNativeIDWriteTextFormat)
    { }
};

} } } }
