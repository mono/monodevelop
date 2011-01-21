//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DWriteEnums.h"

using namespace System::Runtime::InteropServices;
using namespace System::Runtime::CompilerServices;
using namespace System::Linq;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct2D1;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite { 

    /// <summary>
    /// Line spacing parameters.
    /// </summary>
    /// <remarks>
    /// For the default line spacing method, spacing depends solely on the content.
    /// For uniform line spacing, the given line height will override the content.
    /// </remarks>    
    public value struct LineSpacing
    {
    public:

        /// <summary>
        /// Constructor for LineSpacing
        /// </summary>
        /// <param name="lineSpacingMethod">Initializes the LineSpaceingMethod field.</param>
        /// <param name="height">Initializes the Height field.</param>
        /// <param name="baseline">Initializes the Baseline field.</param>
        LineSpacing(
            DirectWrite::LineSpacingMethod lineSpacingMethod,
            Single height,
            Single baseline
            )
        {
            LineSpacingMethod = lineSpacingMethod;
            Height = height;
            Baseline = baseline;
        }


        /// <summary>
        /// How line height is determined.
        /// </summary>
        property DirectWrite::LineSpacingMethod LineSpacingMethod
        {
            DirectWrite::LineSpacingMethod get()
            {
                return lineSpacingMethod;
            }

            void set(DirectWrite::LineSpacingMethod value)
            {
                lineSpacingMethod = value;
            }
        }

        /// <summary>
        /// The line height, or rather distance between one baseline to another.
        /// </summary>
        property Single Height
        {
            Single get()
            {
                return height;
            }

            void set(Single value)
            {
                height = value;
            }
        }

        /// <summary>
        /// Distance from top of line to baseline. A reasonable ratio to Height is 80%.
        /// </summary>
        property Single Baseline
        {
            Single get()
            {
                return baseline;
            }

            void set(Single value)
            {
                baseline = value;
            }
        }

    private:

        DirectWrite::LineSpacingMethod lineSpacingMethod;
        Single height;
        Single baseline;

    public:

        static Boolean operator == (LineSpacing lineSpacing1, LineSpacing lineSpacing2)
        {
            return (lineSpacing1.lineSpacingMethod == lineSpacing2.lineSpacingMethod) &&
                (lineSpacing1.height == lineSpacing2.height) &&
                (lineSpacing1.baseline == lineSpacing2.baseline);
        }

        static Boolean operator != (LineSpacing lineSpacing1, LineSpacing lineSpacing2)
        {
            return !(lineSpacing1 == lineSpacing2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != LineSpacing::typeid)
            {
                return false;
            }

            return *this == safe_cast<LineSpacing>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + lineSpacingMethod.GetHashCode();
            hashCode = hashCode * 31 + height.GetHashCode();
            hashCode = hashCode * 31 + baseline.GetHashCode();

            return hashCode;
        }

    };

    /// <summary>
    /// Specifies a range of text positions where format is applied.
    /// </summary>
    public value struct TextRange
    {
    public:
        /// <summary>
        /// Constructor for TextRange
        /// </summary>
        TextRange (
            UINT32 startPosition,
            UINT32 length
            )
        {
            StartPosition = startPosition;
            Length = length;
        }

        /// <summary>
        /// The start text position of the range.
        /// </summary>
        property UINT32 StartPosition
        {
            UINT32 get()
            {
                return startPosition;
            }

            void set(UINT32 value)
            {
                startPosition = value;
            }
        }

        /// <summary>
        /// The number of text positions in the range.
        /// </summary>
        property UINT32 Length
        {
            UINT32 get()
            {
                return length;
            }

            void set(UINT32 value)
            {
                length = value;
            }
        }
    private:

        UINT32 startPosition;
        UINT32 length;

    internal:
        void CopyFrom(
            const DWRITE_TEXT_RANGE & nativeStruct
            )
        {
            StartPosition = nativeStruct.startPosition;
            Length = nativeStruct.length;
        }

        void CopyTo(
            DWRITE_TEXT_RANGE *pNativeStruct
            )
        {
            pNativeStruct->startPosition = StartPosition;
            pNativeStruct->length = Length;
        }    
    public:

        static Boolean operator == (TextRange textRange1, TextRange textRange2)
        {
            return (textRange1.startPosition == textRange2.startPosition) &&
                (textRange1.length == textRange2.length);
        }

        static Boolean operator != (TextRange textRange1, TextRange textRange2)
        {
            return !(textRange1 == textRange2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != TextRange::typeid)
            {
                return false;
            }

            return *this == safe_cast<TextRange>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + startPosition.GetHashCode();
            hashCode = hashCode * 31 + length.GetHashCode();

            return hashCode;
        }

    };


    /// <summary>
    /// Geometry enclosing of text positions.
    /// </summary>
    public value struct HitTestMetrics
    {
    public:
        /// <summary>
        /// Constructor for HitTestMetrics
        /// </summary>
        HitTestMetrics(
            UINT32 textPosition,
            UINT32 length,
            FLOAT left,
            FLOAT top,
            FLOAT width,
            FLOAT height,
            UINT32 bidirectionalLevel,
            Boolean isText,
            Boolean isTrimmed
            )
        {
            TextPosition = textPosition;
            Length = length;
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            BidiLevel = bidirectionalLevel;
            IsText = isText;
            IsTrimmed = isTrimmed;
        }

        /// <summary>
        /// First text position within the geometry.
        /// </summary>
        property UINT32 TextPosition
        {
            UINT32 get()
            {
                return textPosition;
            }

            void set(UINT32 value)
            {
                textPosition = value;
            }
        }

        /// <summary>
        /// Number of text positions within the geometry.
        /// </summary>
        property UINT32 Length
        {
            UINT32 get()
            {
                return length;
            }

            void set(UINT32 value)
            {
                length = value;
            }
        }

        /// <summary>
        /// Left position of the top-left coordinate of the geometry.
        /// </summary>
        property FLOAT Left
        {
            FLOAT get()
            {
                return left;
            }

            void set(FLOAT value)
            {
                left = value;
            }
        }

        /// <summary>
        /// Top position of the top-left coordinate of the geometry.
        /// </summary>
        property FLOAT Top
        {
            FLOAT get()
            {
                return top;
            }

            void set(FLOAT value)
            {
                top = value;
            }
        }

        /// <summary>
        /// Geometry's width.
        /// </summary>
        property FLOAT Width
        {
            FLOAT get()
            {
                return width;
            }

            void set(FLOAT value)
            {
                width = value;
            }
        }

        /// <summary>
        /// Geometry's height.
        /// </summary>
        property FLOAT Height
        {
            FLOAT get()
            {
                return height;
            }

            void set(FLOAT value)
            {
                height = value;
            }
        }


        // REVIEW: FxCop doesn't like "Bidi". We could replace with "Bidirectional".
        // Or, we could learn more about what this value really is, and try to encapsulate
        // it as something better than a plain uint.

        /// <summary>
        /// Bidi level of text positions enclosed within the geometry.
        /// </summary>
        CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId="Bidi")
        property UINT32 BidiLevel
        {
            UINT32 get()
            {
                return bidiLevel;
            }

            void set(UINT32 value)
            {
                bidiLevel = value;
            }
        }

        /// <summary>
        /// Geometry encloses text?
        /// </summary>
        property Boolean IsText
        {
            Boolean get()
            {
                return isText;
            }

            void set(Boolean value)
            {
                isText = value;
            }
        }

        /// <summary>
        /// Range is trimmed.
        /// </summary>
        property Boolean IsTrimmed
        {
            Boolean get()
            {
                return isTrimmed;
            }

            void set(Boolean value)
            {
                isTrimmed = value;
            }
        }

    private:

        UINT32 textPosition;
        UINT32 length;
        FLOAT left;
        FLOAT top;
        FLOAT width;
        FLOAT height;
        UINT32 bidiLevel;
        Boolean isText;
        Boolean isTrimmed;

    internal:
        void CopyFrom(
            const DWRITE_HIT_TEST_METRICS & nativeStruct
            )
        {
            TextPosition = nativeStruct.textPosition;
            Length = nativeStruct.length;
            Left = nativeStruct.left;
            Top = nativeStruct.top;
            Width = nativeStruct.width;
            Height = nativeStruct.height;
            BidiLevel = nativeStruct.bidiLevel;
            IsText = nativeStruct.isText != 0;
            IsTrimmed = nativeStruct.isTrimmed != 0;
        }

        void CopyTo(
            DWRITE_HIT_TEST_METRICS *pNativeStruct
            )
        {
            pNativeStruct->textPosition = TextPosition;
            pNativeStruct->length = Length;
            pNativeStruct->left = Left;
            pNativeStruct->top = Top;
            pNativeStruct->width = Width;
            pNativeStruct->height = Height;
            pNativeStruct->bidiLevel = BidiLevel;
            pNativeStruct->isText = IsText;
            pNativeStruct->isTrimmed = IsTrimmed;
        }
    public:

        static Boolean operator == (HitTestMetrics hitTestMetrics1, HitTestMetrics hitTestMetrics2)
        {
            return (hitTestMetrics1.textPosition == hitTestMetrics2.textPosition) &&
                (hitTestMetrics1.length == hitTestMetrics2.length) &&
                (hitTestMetrics1.left == hitTestMetrics2.left) &&
                (hitTestMetrics1.top == hitTestMetrics2.top) &&
                (hitTestMetrics1.width == hitTestMetrics2.width) &&
                (hitTestMetrics1.height == hitTestMetrics2.height) &&
                (hitTestMetrics1.bidiLevel == hitTestMetrics2.bidiLevel) &&
                (hitTestMetrics1.isText == hitTestMetrics2.isText) &&
                (hitTestMetrics1.isTrimmed == hitTestMetrics2.isTrimmed);
        }

        static Boolean operator != (HitTestMetrics hitTestMetrics1, HitTestMetrics hitTestMetrics2)
        {
            return !(hitTestMetrics1 == hitTestMetrics2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != HitTestMetrics::typeid)
            {
                return false;
            }

            return *this == safe_cast<HitTestMetrics>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + textPosition.GetHashCode();
            hashCode = hashCode * 31 + length.GetHashCode();
            hashCode = hashCode * 31 + left.GetHashCode();
            hashCode = hashCode * 31 + top.GetHashCode();
            hashCode = hashCode * 31 + width.GetHashCode();
            hashCode = hashCode * 31 + height.GetHashCode();
            hashCode = hashCode * 31 + bidiLevel.GetHashCode();
            hashCode = hashCode * 31 + isText.GetHashCode();
            hashCode = hashCode * 31 + isTrimmed.GetHashCode();

            return hashCode;
        }

    };

    public value class HitTestInfo
    {
    public:

        property HitTestMetrics Metrics
        {
            HitTestMetrics get(void) { return metrics; }
        }

        property Point2F Location
        {
            Point2F get(void) { return location; }
        }

        /// <summary>
        /// Indicates whether the hit-test location is at the leading or the trailing
        /// side of the character. When the <see cref="IsInside" /> value is false, this
        /// value indicates where the hit-test location is relative to the text position as
        /// described by the <see cref="Metrics" /> property.
        /// </summary>
        property bool IsTrailing
        {
            bool get(void) { return isTrailing; }
        }

        /// <summary>
        /// Indicates whether or not the hit-test location is actually inside the nearest
        /// text geometry. If this property is false, the <see cref="IsTrailing" /> property
        /// will indicate to which edge of the nearest geometry the hit-test location is located.
        /// </summary>
        property bool IsInside
        {
            bool get(void) { return isInside; }
        }

        static bool operator ==(HitTestInfo info1, HitTestInfo info2)
        {
            return info1.metrics == info2.metrics &&
                info1.location == info2.location &&
                info1.isTrailing == info2.isTrailing &&
                info1.isInside == info2.isInside;
        }

        static bool operator !=(HitTestInfo info1, HitTestInfo info2)
        {
            return !(info1 == info2);
        }

        virtual bool Equals(Object^ obj) override
        {
            if (obj->GetType() != HitTestInfo::typeid)
            {
                return false;
            }

            return *this == safe_cast<HitTestInfo>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + metrics.GetHashCode();
            hashCode = hashCode * 31 + location.GetHashCode();
            hashCode = hashCode * 31 + isTrailing.GetHashCode();
            hashCode = hashCode * 31 + isInside.GetHashCode();

            return hashCode;
        }

    internal:

        HitTestInfo(HitTestMetrics metrics, Point2F location, bool isTrailing, bool isInside)
            : metrics(metrics), location(location), isTrailing(isTrailing), isInside(isInside)
        { }

    private:

        HitTestMetrics metrics;
        Point2F location;
        bool isTrailing;
        bool isInside;
    };


    /// <summary>
    /// Contains information about a formatted line of text.
    /// </summary>
    public value struct LineMetrics
    {
    public:
        /// <summary>
        /// Constructor for LineMetrics
        /// </summary>
        LineMetrics(
            UINT32 length,
            UINT32 trailingWhitespaceLength,
            UINT32 newlineLength,
            FLOAT height,
            FLOAT baseline,
            Boolean isTrimmed
            )
        {
            Length = length;
            TrailingWhitespaceLength = trailingWhitespaceLength;
            NewlineLength = newlineLength;
            Height = height;
            Baseline = baseline;
            IsTrimmed = isTrimmed;
        }
        /// <summary>
        /// The number of total text positions in the line.
        /// This includes any trailing whitespace and newline characters.
        /// </summary>
        property UINT32 Length
        {
            UINT32 get()
            {
                return length;
            }

            void set(UINT32 value)
            {
                length = value;
            }
        }

        /// <summary>
        /// The number of whitespace positions at the end of the line.  Newline
        /// sequences are considered whitespace.
        /// </summary>
        property UINT32 TrailingWhitespaceLength
        {
            UINT32 get()
            {
                return trailingWhitespaceLength;
            }

            void set(UINT32 value)
            {
                trailingWhitespaceLength = value;
            }
        }

        /// <summary>
        /// The number of characters in the newline sequence at the end of the line.
        /// If the count is zero, then the line was either wrapped or it is the
        /// end of the text.
        /// </summary>
        property UINT32 NewlineLength
        {
            UINT32 get()
            {
                return newlineLength;
            }

            void set(UINT32 value)
            {
                newlineLength = value;
            }
        }

        /// <summary>
        /// Height of the line as measured from top to bottom.
        /// </summary>
        property FLOAT Height
        {
            FLOAT get()
            {
                return height;
            }

            void set(FLOAT value)
            {
                height = value;
            }
        }

        /// <summary>
        /// Distance from the top of the line to its baseline.
        /// </summary>
        property FLOAT Baseline
        {
            FLOAT get()
            {
                return baseline;
            }

            void set(FLOAT value)
            {
                baseline = value;
            }
        }

        /// <summary>
        /// The line is trimmed.
        /// </summary>
        property Boolean IsTrimmed
        {
            Boolean get()
            {
                return isTrimmed;
            }

            void set(Boolean value)
            {
                isTrimmed = value;
            }
        }

    private:

        UINT32 length;
        UINT32 trailingWhitespaceLength;
        UINT32 newlineLength;
        FLOAT height;
        FLOAT baseline;
        Boolean isTrimmed;

    internal:
        void CopyFrom(
            const DWRITE_LINE_METRICS & nativeStruct
            )
        {
            Length = nativeStruct.length;
            TrailingWhitespaceLength = nativeStruct.trailingWhitespaceLength;
            NewlineLength = nativeStruct.newlineLength;
            Height = nativeStruct.height;
            Baseline = nativeStruct.baseline;
            IsTrimmed = nativeStruct.isTrimmed != 0;
        }

        void CopyTo(
            DWRITE_LINE_METRICS *pNativeStruct
            )
        {
            pNativeStruct->length = Length;

            pNativeStruct->trailingWhitespaceLength = TrailingWhitespaceLength;
            pNativeStruct->newlineLength = NewlineLength;
            pNativeStruct->height = Height;
            pNativeStruct->baseline = Baseline;
            pNativeStruct->isTrimmed = IsTrimmed;
        }
    public:

        static Boolean operator == (LineMetrics lineMetrics1, LineMetrics lineMetrics2)
        {
            return (lineMetrics1.length == lineMetrics2.length) &&
                (lineMetrics1.trailingWhitespaceLength == lineMetrics2.trailingWhitespaceLength) &&
                (lineMetrics1.newlineLength == lineMetrics2.newlineLength) &&
                (lineMetrics1.height == lineMetrics2.height) &&
                (lineMetrics1.baseline == lineMetrics2.baseline) &&
                (lineMetrics1.isTrimmed == lineMetrics2.isTrimmed);
        }

        static Boolean operator != (LineMetrics lineMetrics1, LineMetrics lineMetrics2)
        {
            return !(lineMetrics1 == lineMetrics2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != LineMetrics::typeid)
            {
                return false;
            }

            return *this == safe_cast<LineMetrics>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + length.GetHashCode();
            hashCode = hashCode * 31 + trailingWhitespaceLength.GetHashCode();
            hashCode = hashCode * 31 + newlineLength.GetHashCode();
            hashCode = hashCode * 31 + height.GetHashCode();
            hashCode = hashCode * 31 + baseline.GetHashCode();
            hashCode = hashCode * 31 + isTrimmed.GetHashCode();

            return hashCode;
        }

    };


    /// <summary>
    /// Overall metrics associated with text after layout.
    /// All coordinates are in device independent pixels (DIPs).
    /// </summary>
    public value struct TextMetrics
    {
    public:
        /// <summary>
        /// Constructor for TextMetrics
        /// </summary>
        TextMetrics(
            FLOAT left,
            FLOAT top,
            FLOAT width,
            FLOAT widthIncludingTrailingWhitespace,
            FLOAT height,
            FLOAT layoutWidth,
            FLOAT layoutHeight,
            UINT32 maxBidirectionalReorderingDepth,
            UINT32 lineCount)
        {
            Left = left;
            Top = top;
            Width = width;
            WidthIncludingTrailingWhitespace = widthIncludingTrailingWhitespace;
            Height = height;
            LayoutWidth = layoutWidth;
            LayoutHeight = layoutHeight;
            MaxBidirectionalReorderingDepth = maxBidirectionalReorderingDepth;
            LineCount = lineCount;
        }

        /// <summary>
        /// Left-most point of formatted text relative to layout box
        /// (excluding any glyph overhang).
        /// </summary>
        property FLOAT Left
        {
            FLOAT get()
            {
                return left;
            }

            void set(FLOAT value)
            {
                left = value;
            }
        }

        /// <summary>
        /// Top-most point of formatted text relative to layout box
        /// (excluding any glyph overhang).
        /// </summary>
        property FLOAT Top
        {
            FLOAT get()
            {
                return top;
            }

            void set(FLOAT value)
            {
                top = value;
            }
        }

        /// <summary>
        /// The width of the formatted text ignoring trailing whitespace
        /// at the end of each line.
        /// </summary>
        property FLOAT Width
        {
            FLOAT get()
            {
                return width;
            }

            void set(FLOAT value)
            {
                width = value;
            }
        }

        /// <summary>
        /// The width of the formatted text taking into account the
        /// trailing whitespace at the end of each line.
        /// </summary>
        property FLOAT WidthIncludingTrailingWhitespace
        {
            FLOAT get()
            {
                return widthIncludingTrailingWhitespace;
            }

            void set(FLOAT value)
            {
                widthIncludingTrailingWhitespace = value;
            }
        }

        /// <summary>
        /// The height of the formatted text. The height of an empty string
        /// is determined by the size of the default font's line height.
        /// </summary>
        property FLOAT Height
        {
            FLOAT get()
            {
                return height;
            }

            void set(FLOAT value)
            {
                height = value;
            }
        }

        /// <summary>
        /// Initial width given to the layout. Depending on whether the text
        /// was wrapped or not, it can be either larger or smaller than the
        /// text content width.
        /// </summary>
        property FLOAT LayoutWidth
        {
            FLOAT get()
            {
                return layoutWidth;
            }

            void set(FLOAT value)
            {
                layoutWidth = value;
            }
        }

        /// <summary>
        /// Initial height given to the layout. Depending on the length of the
        /// text, it may be larger or smaller than the text content height.
        /// </summary>
        property FLOAT LayoutHeight
        {
            FLOAT get()
            {
                return layoutHeight;
            }

            void set(FLOAT value)
            {
                layoutHeight = value;
            }
        }

        /// <summary>
        /// The maximum reordering count of any line of text, used
        /// to calculate the most number of hit-testing boxes needed.
        /// If the layout has no bidirectional text or no text at all,
        /// the minimum level is 1.
        /// </summary>
        property UINT32 MaxBidirectionalReorderingDepth
        {
            UINT32 get()
            {
                return maxBidiReorderingDepth;
            }

            void set(UINT32 value)
            {
                maxBidiReorderingDepth = value;
            }
        }

        /// <summary>
        /// Total number of lines.
        /// </summary>
        property UINT32 LineCount
        {
            UINT32 get()
            {
                return lineCount;
            }

            void set(UINT32 value)
            {
                lineCount = value;
            }
        }


    private:

        FLOAT left;
        FLOAT top;
        FLOAT width;
        FLOAT widthIncludingTrailingWhitespace;
        FLOAT height;
        FLOAT layoutWidth;
        FLOAT layoutHeight;
        UINT32 maxBidiReorderingDepth;
        UINT32 lineCount;

    internal:
        void CopyFrom(
            const DWRITE_TEXT_METRICS & nativeStruct
            )
        {
            Left = nativeStruct.left;
            Top = nativeStruct.top;
            Width = nativeStruct.width;
            WidthIncludingTrailingWhitespace = nativeStruct.widthIncludingTrailingWhitespace;
            Height = nativeStruct.height;
            LayoutWidth = nativeStruct.layoutWidth;
            LayoutHeight = nativeStruct.layoutHeight;
            MaxBidirectionalReorderingDepth = nativeStruct.maxBidiReorderingDepth;
            LineCount = nativeStruct.lineCount;
        }

        void CopyTo(
            DWRITE_TEXT_METRICS *pNativeStruct
            )
        {
            pNativeStruct->left = Left ;
            pNativeStruct->top = Top;
            pNativeStruct->width = Width;
            pNativeStruct->widthIncludingTrailingWhitespace = WidthIncludingTrailingWhitespace;
            pNativeStruct->height = Height;
            pNativeStruct->layoutWidth = LayoutWidth;
            pNativeStruct->layoutHeight = LayoutHeight;
            pNativeStruct->maxBidiReorderingDepth = MaxBidirectionalReorderingDepth;
            pNativeStruct->lineCount = LineCount;
        }
    public:

        static Boolean operator == (TextMetrics textMetrics1, TextMetrics textMetrics2)
        {
            return (textMetrics1.left == textMetrics2.left) &&
                (textMetrics1.top == textMetrics2.top) &&
                (textMetrics1.width == textMetrics2.width) &&
                (textMetrics1.widthIncludingTrailingWhitespace == textMetrics2.widthIncludingTrailingWhitespace) &&
                (textMetrics1.height == textMetrics2.height) &&
                (textMetrics1.layoutWidth == textMetrics2.layoutWidth) &&
                (textMetrics1.layoutHeight == textMetrics2.layoutHeight) &&
                (textMetrics1.maxBidiReorderingDepth == textMetrics2.maxBidiReorderingDepth) &&
                (textMetrics1.lineCount == textMetrics2.lineCount);
        }

        static Boolean operator != (TextMetrics textMetrics1, TextMetrics textMetrics2)
        {
            return !(textMetrics1 == textMetrics2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != TextMetrics::typeid)
            {
                return false;
            }

            return *this == safe_cast<TextMetrics>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + left.GetHashCode();
            hashCode = hashCode * 31 + top.GetHashCode();
            hashCode = hashCode * 31 + width.GetHashCode();
            hashCode = hashCode * 31 + widthIncludingTrailingWhitespace.GetHashCode();
            hashCode = hashCode * 31 + height.GetHashCode();
            hashCode = hashCode * 31 + layoutWidth.GetHashCode();
            hashCode = hashCode * 31 + layoutHeight.GetHashCode();
            hashCode = hashCode * 31 + maxBidiReorderingDepth.GetHashCode();
            hashCode = hashCode * 31 + lineCount.GetHashCode();

            return hashCode;
        }

    };


    /// <summary>
    /// Holds information about how much any visible pixels
    /// (in DIPs) overshoot each side of the layout or inline objects.
    /// </summary>
    /// <remarks>
    /// Positive overhangs indicate that the visible area extends outside the layout
    /// box or inline object, while negative values mean there is whitespace inside.
    /// The returned values are unaffected by rendering transforms or pixel snapping.
    /// Additionally, they may not exactly match final target's pixel bounds after
    /// applying grid fitting and hinting.
    /// </remarks>
    [StructLayout(LayoutKind::Sequential)]
    public value struct OverhangMetrics
    {
    public:
        /// <summary>
        /// Constructor for OverhangMetrics
        /// </summary>
        OverhangMetrics (
            FLOAT left,
            FLOAT top,
            FLOAT right,
            FLOAT bottom )
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>
        /// The distance from the left-most visible DIP to its left alignment edge.
        /// </summary>
        property FLOAT Left
        {
            FLOAT get()
            {
                return left;
            }

            void set(FLOAT value)
            {
                left = value;
            }
        }

        /// <summary>
        /// The distance from the top-most visible DIP to its top alignment edge.
        /// </summary>
        property FLOAT Top
        {
            FLOAT get()
            {
                return top;
            }

            void set(FLOAT value)
            {
                top = value;
            }
        }

        /// <summary>
        /// The distance from the right-most visible DIP to its right alignment edge.
        /// </summary>
        property FLOAT Right
        {
            FLOAT get()
            {
                return right;
            }

            void set(FLOAT value)
            {
                right = value;
            }
        }

        /// <summary>
        /// The distance from the bottom-most visible DIP to its bottom alignment edge.
        /// </summary>
        property FLOAT Bottom
        {
            FLOAT get()
            {
                return bottom;
            }

            void set(FLOAT value)
            {
                bottom = value;
            }
        }

    private:

        FLOAT left;
        FLOAT top;
        FLOAT right;
        FLOAT bottom;

    internal:
        void CopyFrom(
            const DWRITE_OVERHANG_METRICS & nativeStruct
            )
        {
            Left = nativeStruct.left;
            Top = nativeStruct.top;
            Right = nativeStruct.right;
            Bottom = nativeStruct.bottom;
        }

        void CopyTo(
            DWRITE_OVERHANG_METRICS *pNativeStruct
            )
        {
            pNativeStruct->left = Left ;
            pNativeStruct->top = Top;
            pNativeStruct->right = Right;
            pNativeStruct->bottom = Bottom;
        }
    public:

        static Boolean operator == (OverhangMetrics overhangMetrics1, OverhangMetrics overhangMetrics2)
        {
            return (overhangMetrics1.left == overhangMetrics2.left) &&
                (overhangMetrics1.top == overhangMetrics2.top) &&
                (overhangMetrics1.right == overhangMetrics2.right) &&
                (overhangMetrics1.bottom == overhangMetrics2.bottom);
        }

        static Boolean operator != (OverhangMetrics overhangMetrics1, OverhangMetrics overhangMetrics2)
        {
            return !(overhangMetrics1 == overhangMetrics2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != OverhangMetrics::typeid)
            {
                return false;
            }

            return *this == safe_cast<OverhangMetrics>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + left.GetHashCode();
            hashCode = hashCode * 31 + top.GetHashCode();
            hashCode = hashCode * 31 + right.GetHashCode();
            hashCode = hashCode * 31 + bottom.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Contains information about a glyph cluster.
    /// </summary>
    public value struct ClusterMetrics
    {
    public:
        /// <summary>
        /// Constructor for ClusterMetrics
        /// </summary>
        ClusterMetrics (
            FLOAT width,
            UINT16 length,
            Boolean canWrapLineAfter,
            Boolean isWhitespace,
            Boolean isNewline,
            Boolean isSoftHyphen,
            Boolean isRightToLeft)
        {
            Width = width;
            Length = length;
            CanWrapLineAfter = canWrapLineAfter;
            IsWhitespace = isWhitespace;
            IsNewline = isNewline;
            IsSoftHyphen = isSoftHyphen;
            IsRightToLeft = isRightToLeft;
        }

        /// <summary>
        /// The total advance width of all glyphs in the cluster.
        /// </summary>
        property FLOAT Width
        {
            FLOAT get()
            {
                return width;
            }

            void set(FLOAT value)
            {
                width = value;
            }
        }

        /// <summary>
        /// The number of text positions in the cluster.
        /// </summary>
        property UINT16 Length
        {
            UINT16 get()
            {
                return length;
            }

            void set(UINT16 value)
            {
                length = value;
            }
        }

        /// <summary>
        /// Indicate whether line can be broken right after the cluster.
        /// </summary>
        property Boolean CanWrapLineAfter
        {
            Boolean get()
            {
                return canWrapLineAfter;
            }

            void set(Boolean value)
            {
                canWrapLineAfter = value;
            }
        }

        /// <summary>
        /// Indicate whether the cluster corresponds to whitespace character.
        /// </summary>
        property Boolean IsWhitespace
        {
            Boolean get()
            {
                return isWhitespace;
            }

            void set(Boolean value)
            {
                isWhitespace = value;
            }
        }

        /// <summary>
        /// Indicate whether the cluster corresponds to a newline character.
        /// </summary>
        property Boolean IsNewline
        {
            Boolean get()
            {
                return isNewline;
            }

            void set(Boolean value)
            {
                isNewline = value;
            }
        }

        /// <summary>
        /// Indicate whether the cluster corresponds to soft hyphen character.
        /// </summary>
        property Boolean IsSoftHyphen
        {
            Boolean get()
            {
                return isSoftHyphen;
            }

            void set(Boolean value)
            {
                isSoftHyphen = value;
            }
        }

        /// <summary>
        /// Indicate whether the cluster is read from right to left.
        /// </summary>
        property Boolean IsRightToLeft
        {
            Boolean get()
            {
                return isRightToLeft;
            }

            void set(Boolean value)
            {
                isRightToLeft = value;
            }
        }

    private:

        FLOAT width;
        UINT16 length;
        Boolean canWrapLineAfter;
        Boolean isWhitespace;
        Boolean isNewline;
        Boolean isSoftHyphen;
        Boolean isRightToLeft;

    internal:
        void CopyFrom(
            const DWRITE_CLUSTER_METRICS & nativeStruct
            )
        {
            Width = nativeStruct.width;
            Length = nativeStruct.length;
            CanWrapLineAfter = nativeStruct.canWrapLineAfter != 0;
            IsWhitespace = nativeStruct.isWhitespace != 0;
            IsNewline = nativeStruct.isNewline != 0;
            IsSoftHyphen = nativeStruct.isSoftHyphen != 0;
            IsRightToLeft = nativeStruct.isRightToLeft != 0;
        }

        void CopyTo(
            DWRITE_CLUSTER_METRICS *pNativeStruct
            )
        {
            pNativeStruct->width = Width;
            pNativeStruct->length = Length;
            pNativeStruct->canWrapLineAfter = CanWrapLineAfter ? 1 : 0;
            pNativeStruct->isWhitespace = IsWhitespace ? 1 : 0;
            pNativeStruct->isNewline = IsNewline ? 1 : 0;
            pNativeStruct->isSoftHyphen = IsSoftHyphen ? 1 : 0;
            pNativeStruct->isRightToLeft = IsRightToLeft ? 1 : 0;
        }

    public:

        static Boolean operator == (ClusterMetrics clusterMetrics1, ClusterMetrics clusterMetrics2)
        {
            return (clusterMetrics1.width == clusterMetrics2.width) &&
                (clusterMetrics1.length == clusterMetrics2.length) &&
                (clusterMetrics1.canWrapLineAfter == clusterMetrics2.canWrapLineAfter) &&
                (clusterMetrics1.isWhitespace == clusterMetrics2.isWhitespace) &&
                (clusterMetrics1.isNewline == clusterMetrics2.isNewline) &&
                (clusterMetrics1.isSoftHyphen == clusterMetrics2.isSoftHyphen) &&
                (clusterMetrics1.isRightToLeft == clusterMetrics2.isRightToLeft);
        }

        static Boolean operator != (ClusterMetrics clusterMetrics1, ClusterMetrics clusterMetrics2)
        {
            return !(clusterMetrics1 == clusterMetrics2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != ClusterMetrics::typeid)
            {
                return false;
            }

            return *this == safe_cast<ClusterMetrics>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + width.GetHashCode();
            hashCode = hashCode * 31 + length.GetHashCode();
            hashCode = hashCode * 31 + canWrapLineAfter.GetHashCode();
            hashCode = hashCode * 31 + isWhitespace.GetHashCode();
            hashCode = hashCode * 31 + isNewline.GetHashCode();
            hashCode = hashCode * 31 + isSoftHyphen.GetHashCode();
            hashCode = hashCode * 31 + isRightToLeft.GetHashCode();

            return hashCode;
        }

    };


    /// <summary>
    /// Specifies the trimming option for text overflowing the layout box.
    /// </summary>
    public value struct Trimming
    {
    public:
        /// <summary>
        /// Constructor for Trimming
        /// </summary>
        Trimming (
            TrimmingGranularity granularity,
            UINT32 delimiter,
            UINT32 delimiterCount)
        {
            Granularity = granularity;
            Delimiter = delimiter;
            DelimiterCount = delimiterCount;
        }

        /// <summary>
        /// Text granularity of which trimming applies.
        /// </summary>
        property TrimmingGranularity Granularity
        {
            TrimmingGranularity get()
            {
                return granularity;
            }

            void set(TrimmingGranularity value)
            {
                granularity = value;
            }
        }

        /// <summary>
        /// Character code used as the delimiter signaling the beginning of the portion of text to be preserved,
        /// most useful for path ellipsis, where the delimeter would be a slash.
        /// </summary>
        property UINT32 Delimiter
        {
            UINT32 get()
            {
                return delimiter;
            }

            void set(UINT32 value)
            {
                delimiter = value;
            }
        }

        /// <summary>
        /// How many occurences of the delimiter to step back.
        /// </summary>
        property UINT32 DelimiterCount
        {
            UINT32 get()
            {
                return delimiterCount;
            }

            void set(UINT32 value)
            {
                delimiterCount = value;
            }
        }

    private:

        TrimmingGranularity granularity;
        UINT32 delimiter;
        UINT32 delimiterCount;

    internal:
        void CopyFrom(
            const DWRITE_TRIMMING & nativeStruct
            )
        {
            Granularity = static_cast<TrimmingGranularity>(nativeStruct.granularity);
            Delimiter = nativeStruct.delimiter;
            DelimiterCount = nativeStruct.delimiterCount;
        }

        void CopyTo(
            DWRITE_TRIMMING *pNativeStruct
            )
        {
            pNativeStruct->granularity = static_cast<DWRITE_TRIMMING_GRANULARITY>(Granularity);
            pNativeStruct->delimiter = Delimiter;
            pNativeStruct->delimiterCount = DelimiterCount;
        }
    public:

        static Boolean operator == (Trimming trimming1, Trimming trimming2)
        {
            return (trimming1.granularity == trimming2.granularity) &&
                (trimming1.delimiter == trimming2.delimiter) &&
                (trimming1.delimiterCount == trimming2.delimiterCount);
        }

        static Boolean operator != (Trimming trimming1, Trimming trimming2)
        {
            return !(trimming1 == trimming2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != Trimming::typeid)
            {
                return false;
            }

            return *this == safe_cast<Trimming>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + granularity.GetHashCode();
            hashCode = hashCode * 31 + delimiter.GetHashCode();
            hashCode = hashCode * 31 + delimiterCount.GetHashCode();

            return hashCode;
        }

    };

    interface class ICustomInlineObject;

    public value struct TrimmingWithSign
    {
    public:

        property DirectWrite::Trimming Trimming
        {
            DirectWrite::Trimming get(void) { return trimming; }
        }

        property ICustomInlineObject^ Sign
        {
            ICustomInlineObject^ get(void) { return sign; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="trimming">
        /// The trimming options.
        /// </param>
        /// <param name="sign">
        /// An inline object to use as the sign that trimming as occurred.
        /// </param>
        /// <remarks>
        /// Any inline object can be used for the trimming sign, but DWriteFactory.CreateEllipsisTrimmingSign
        /// provides a typical ellipsis symbol. Trimming is also useful vertically for hiding
        /// partial lines.
        /// </remarks>
        TrimmingWithSign(DirectWrite::Trimming trimming, ICustomInlineObject^ sign)
            : trimming(trimming), sign(sign)
        { }

    private:

        DirectWrite::Trimming trimming;
        ICustomInlineObject^ sign;
    };



    /// <summary>
    /// Specifies the metrics of a font face that are applicable to all glyphs within the font face.
    /// </summary>
    public value struct FontMetrics
    {
    public:
        /// <summary>
        /// Constructor for FontMetrics
        /// </summary>
        FontMetrics (
            UINT16 designUnitsPerEm,
            UINT16 ascent,
            UINT16 descent,
            INT16 lineGap,
            UINT16 capHeight,
            UINT16 xHeight,
            INT16 underlinePosition,
            UINT16 underlineThickness,
            INT16 strikethroughPosition,
            UINT16 strikethroughThickness
            )
        {
            DesignUnitsPerEm = designUnitsPerEm;
            Ascent = ascent;
            Descent = descent;
            LineGap = lineGap;
            CapHeight = capHeight;
            XHeight = xHeight;
            UnderlinePosition = underlinePosition;
            UnderlineThickness = underlineThickness;
            StrikethroughPosition = strikethroughPosition;
            StrikethroughThickness = strikethroughThickness;
        }

    internal:
        /// <summary>
        /// Internal Constructor for FontMetrics
        /// </summary>
        FontMetrics (const DWRITE_FONT_METRICS & native)
        {
            DesignUnitsPerEm = native.designUnitsPerEm;
            Ascent = native.ascent;
            Descent = native.descent;
            LineGap = native.lineGap;
            CapHeight = native.capHeight;
            XHeight = native.xHeight;
            UnderlinePosition = native.underlinePosition;
            UnderlineThickness = native.underlineThickness;
            StrikethroughPosition = native.strikethroughPosition;
            StrikethroughThickness = native.strikethroughThickness;
        }

    public:
        /// <summary>
        /// The number of font design units per em unit.
        /// Font files use their own coordinate system of font design units.
        /// A font design unit is the smallest measurable unit in the em square,
        /// an imaginary square that is used to size and align glyphs.
        /// The concept of em square is used as a reference scale factor when defining font size and device transformation semantics.
        /// The size of one em square is also commonly used to compute the paragraph identation value.
        /// </summary>
        property UINT16 DesignUnitsPerEm
        {
            UINT16 get()
            {
                return designUnitsPerEm;
            }

            void set(UINT16 value)
            {
                designUnitsPerEm = value;
            }
        }

        /// <summary>
        /// Ascent value of the font face in font design units.
        /// Ascent is the distance from the top of font character alignment box to English baseline.
        /// </summary>
        property UINT16 Ascent
        {
            UINT16 get()
            {
                return ascent;
            }

            void set(UINT16 value)
            {
                ascent = value;
            }
        }

        /// <summary>
        /// Descent value of the font face in font design units.
        /// Descent is the distance from the bottom of font character alignment box to English baseline.
        /// </summary>
        property UINT16 Descent
        {
            UINT16 get()
            {
                return descent;
            }

            void set(UINT16 value)
            {
                descent = value;
            }
        }

        /// <summary>
        /// Line gap in font design units.
        /// Recommended additional white space to add between lines to improve legibility. The recommended line spacing 
        /// (baseline-to-baseline distance) is thus the sum of ascent, descent, and lineGap. The line gap is usually 
        /// positive or zero but can be negative, in which case the recommended line spacing is less than the height
        /// of the character alignment box.
        /// </summary>
        property INT16 LineGap
        {
            INT16 get()
            {
                return lineGap;
            }

            void set(INT16 value)
            {
                lineGap = value;
            }
        }

        /// <summary>
        /// Cap height value of the font face in font design units.
        /// Cap height is the distance from English baseline to the top of a typical English capital.
        /// Capital "H" is often used as a reference character for the purpose of calculating the cap height value.
        /// </summary>
        property UINT16 CapHeight
        {
            UINT16 get()
            {
                return capHeight;
            }

            void set(UINT16 value)
            {
                capHeight = value;
            }
        }

        /// <summary>
        /// x-height value of the font face in font design units.
        /// x-height is the distance from English baseline to the top of lowercase letter "x", or a similar lowercase character.
        /// </summary>
        property UINT16 XHeight
        {
            UINT16 get()
            {
                return xHeight;
            }

            void set(UINT16 value)
            {
                xHeight = value;
            }
        }

        /// <summary>
        /// The underline position value of the font face in font design units.
        /// Underline position is the position of underline relative to the English baseline.
        /// The value is usually made negative in order to place the underline below the baseline.
        /// </summary>
        property INT16 UnderlinePosition
        {
            INT16 get()
            {
                return underlinePosition;
            }

            void set(INT16 value)
            {
                underlinePosition = value;
            }
        }

        /// <summary>
        /// The suggested underline thickness value of the font face in font design units.
        /// </summary>
        property UINT16 UnderlineThickness
        {
            UINT16 get()
            {
                return underlineThickness;
            }

            void set(UINT16 value)
            {
                underlineThickness = value;
            }
        }

        /// <summary>
        /// The strikethrough position value of the font face in font design units.
        /// Strikethrough position is the position of strikethrough relative to the English baseline.
        /// The value is usually made positive in order to place the strikethrough above the baseline.
        /// </summary>
        property INT16 StrikethroughPosition
        {
            INT16 get()
            {
                return strikethroughPosition;
            }

            void set(INT16 value)
            {
                strikethroughPosition = value;
            }
        }

        /// <summary>
        /// The suggested strikethrough thickness value of the font face in font design units.
        /// </summary>
        property UINT16 StrikethroughThickness
        {
            UINT16 get()
            {
                return strikethroughThickness;
            }

            void set(UINT16 value)
            {
                strikethroughThickness = value;
            }
        }

    private:

        UINT16 designUnitsPerEm;
        UINT16 ascent;
        UINT16 descent;
        INT16 lineGap;
        UINT16 capHeight;
        UINT16 xHeight;
        INT16 underlinePosition;
        UINT16 underlineThickness;
        INT16 strikethroughPosition;
        UINT16 strikethroughThickness;

    internal:
        void CopyFrom(const DWRITE_FONT_METRICS & native)
        {
            DesignUnitsPerEm = native.designUnitsPerEm;
            Ascent = native.ascent;
            Descent = native.descent;
            LineGap = native.lineGap;
            CapHeight = native.capHeight;
            XHeight = native.xHeight;
            UnderlinePosition = native.underlinePosition;
            UnderlineThickness = native.underlineThickness;
            StrikethroughPosition = native.strikethroughPosition;
            StrikethroughThickness = native.strikethroughThickness;
        }

        void CopyTo(DWRITE_FONT_METRICS *pNativeStruct)
        {
            pNativeStruct->designUnitsPerEm = DesignUnitsPerEm;
            pNativeStruct->ascent = Ascent;
            pNativeStruct->descent = Descent;
            pNativeStruct->lineGap = LineGap;
            pNativeStruct->capHeight = CapHeight;
            pNativeStruct->xHeight = XHeight;
            pNativeStruct->underlinePosition = UnderlinePosition;
            pNativeStruct->underlineThickness = UnderlineThickness;
            pNativeStruct->strikethroughPosition = StrikethroughPosition;
            pNativeStruct->strikethroughThickness = StrikethroughThickness;
        }
    public:

        static Boolean operator == (FontMetrics fontMetrics1, FontMetrics fontMetrics2)
        {
            return (fontMetrics1.designUnitsPerEm == fontMetrics2.designUnitsPerEm) &&
                (fontMetrics1.ascent == fontMetrics2.ascent) &&
                (fontMetrics1.descent == fontMetrics2.descent) &&
                (fontMetrics1.lineGap == fontMetrics2.lineGap) &&
                (fontMetrics1.capHeight == fontMetrics2.capHeight) &&
                (fontMetrics1.xHeight == fontMetrics2.xHeight) &&
                (fontMetrics1.underlinePosition == fontMetrics2.underlinePosition) &&
                (fontMetrics1.underlineThickness == fontMetrics2.underlineThickness) &&
                (fontMetrics1.strikethroughPosition == fontMetrics2.strikethroughPosition) &&
                (fontMetrics1.strikethroughThickness == fontMetrics2.strikethroughThickness);
        }

        static Boolean operator != (FontMetrics fontMetrics1, FontMetrics fontMetrics2)
        {
            return !(fontMetrics1 == fontMetrics2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != FontMetrics::typeid)
            {
                return false;
            }

            return *this == safe_cast<FontMetrics>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + designUnitsPerEm.GetHashCode();
            hashCode = hashCode * 31 + ascent.GetHashCode();
            hashCode = hashCode * 31 + descent.GetHashCode();
            hashCode = hashCode * 31 + lineGap.GetHashCode();
            hashCode = hashCode * 31 + capHeight.GetHashCode();
            hashCode = hashCode * 31 + xHeight.GetHashCode();
            hashCode = hashCode * 31 + underlinePosition.GetHashCode();
            hashCode = hashCode * 31 + underlineThickness.GetHashCode();
            hashCode = hashCode * 31 + strikethroughPosition.GetHashCode();
            hashCode = hashCode * 31 + strikethroughThickness.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// Properties describing the geometric measurement of an
    /// application-defined inline object.
    /// </summary>
    public value struct InlineObjectMetrics
    {
        /// <summary>
        /// Constructor for InlineObjectMetrics
        /// </summary>
        InlineObjectMetrics (
            Single width,
            Single height,
            Single baseline,
            Boolean supportsSideways
            )
        {
            Width = width;
            Height = height;
            Baseline = baseline;
            SupportsSideways = supportsSideways;
        }

        /// <summary>
        /// Width of the inline object.
        /// </summary>
        property Single Width
        {
            Single get()
            {
                return width;
            }

            void set(Single value)
            {
                width = value;
            }
        }

        /// <summary>
        /// Height of the inline object as measured from top to bottom.
        /// </summary>
        property Single Height
        {
            Single get()
            {
                return height;
            }

            void set(Single value)
            {
                height = value;
            }
        }

        /// <summary>
        /// Distance from the top of the object to the baseline where it is lined up with the adjacent text.
        /// If the baseline is at the bottom, baseline simply equals height.
        /// </summary>
        property Single Baseline
        {
            Single get()
            {
                return baseline;
            }

            void set(Single value)
            {
                baseline = value;
            }
        }

        /// <summary>
        /// Flag indicating whether the object is to be placed upright or alongside the text baseline
        /// for vertical text.
        /// </summary>
        property Boolean SupportsSideways
        {
            Boolean get()
            {
                return supportsSideways;
            }

            void set(Boolean value)
            {
                supportsSideways = value;
            }
        }

    private:

        Single width;
        Single height;
        Single baseline;
        Boolean supportsSideways;

    internal:
        void CopyTo(DWRITE_INLINE_OBJECT_METRICS *pNativeStruct)
        {
            pNativeStruct->width = Width;
            pNativeStruct->height = Height;
            pNativeStruct->baseline = Baseline;
            pNativeStruct->supportsSideways = SupportsSideways ? 1 : 0;
        }
    public:

        static Boolean operator == (InlineObjectMetrics inlineObjectMetrics1, InlineObjectMetrics inlineObjectMetrics2)
        {
            return (inlineObjectMetrics1.width == inlineObjectMetrics2.width) &&
                (inlineObjectMetrics1.height == inlineObjectMetrics2.height) &&
                (inlineObjectMetrics1.baseline == inlineObjectMetrics2.baseline) &&
                (inlineObjectMetrics1.supportsSideways == inlineObjectMetrics2.supportsSideways);
        }

        static Boolean operator != (InlineObjectMetrics inlineObjectMetrics1, InlineObjectMetrics inlineObjectMetrics2)
        {
            return !(inlineObjectMetrics1 == inlineObjectMetrics2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != InlineObjectMetrics::typeid)
            {
                return false;
            }

            return *this == safe_cast<InlineObjectMetrics>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + width.GetHashCode();
            hashCode = hashCode * 31 + height.GetHashCode();
            hashCode = hashCode * 31 + baseline.GetHashCode();
            hashCode = hashCode * 31 + supportsSideways.GetHashCode();

            return hashCode;
        }

    };



    /// <summary>
    /// The FontFeature structure specifies properties used to identify and execute typographic feature in the font.
    /// </summary>
    /// <remarks>
    /// <para>A non-zero value generally enables the feature execution, while the zero value disables it. 
    /// A feature requiring a selector uses this value to indicate the selector index.</para>
    /// <para>The OpenType standard provides access to typographic features available in the font by means 
    /// of a feature tag with the associated parameters. The OpenType feature tag is a 4-byte identifier of 
    /// the registered name of a feature. For example, the ‘kern’ feature name tag is used to identify 
    /// the ‘Kerning’ feature in OpenType font. Similarly, the OpenType feature tag for ‘Standard Ligatures’ and 
    /// ‘Fractions’ is ‘liga’ and ‘frac’ respectively. Since a single run can be associated with more than one 
    /// typographic features, the Text String API accepts typographic settings for a run as a list of features 
    /// and are executed in the order they are specified.</para>
    /// <para>The value of the Tag member represents the OpenType name tag of the feature, while the param value 
    /// represents additional parameter for the execution of the feature referred by the tag member. 
    /// Both nameTag and param are stored as little endian, the same convention followed by GDI. 
    /// Most features treat the Param value as a binary value that indicates whether to turn the 
    /// execution of the feature on or off, with it being off by default in the majority of cases. 
    /// Some features, however, treat this value as an integral value representing the integer index to 
    /// the list of alternate results it may produce during the execution; for instance, the feature 
    /// ‘Stylistic Alternates’ or ‘salt’ uses the param value as an index to the list of alternate substituting 
    /// glyphs it could produce for a specified glyph. </para>
    /// </remarks>
    public value struct FontFeature
    {
    public:
        /// <summary>
        /// Constructor for FontFeature
        /// </summary>
        FontFeature (
            FontFeatureTag nameTag,
            UINT32 parameter
            )
        {
            NameTag = nameTag;
            Parameter = parameter;
        }

        /// <summary>
        /// The feature OpenType name identifier.
        /// </summary>
        property FontFeatureTag NameTag
        {
            FontFeatureTag get()
            {
                return nameTag;
            }

            void set(FontFeatureTag value)
            {
                nameTag = value;
            }
        }

        /// <summary>
        /// Execution parameter of the feature.
        /// </summary>
        /// <remarks>
        /// The parameter should be non-zero to enable the feature.  Once enabled, a feature can't be disabled again within
        /// the same range.  Features requiring a selector use this value to indicate the selector index. 
        /// </remarks>
        property UINT32 Parameter
        {
            UINT32 get()
            {
                return parameter;
            }

            void set(UINT32 value)
            {
                parameter = value;
            }
        }

    private:

        FontFeatureTag nameTag;
        UINT32 parameter;

    internal:
        void CopyFrom(const DWRITE_FONT_FEATURE & native)
        {
            NameTag = static_cast<FontFeatureTag>(native.nameTag);
            Parameter = native.parameter;
        }

        void CopyTo(DWRITE_FONT_FEATURE *pNativeStruct)
        {
            pNativeStruct->nameTag = static_cast<DWRITE_FONT_FEATURE_TAG>(NameTag);
            pNativeStruct->parameter = Parameter;
        }
    public:

        static Boolean operator == (FontFeature fontFeature1, FontFeature fontFeature2)
        {
            return (fontFeature1.nameTag == fontFeature2.nameTag) &&
                (fontFeature1.parameter == fontFeature2.parameter);
        }

        static Boolean operator != (FontFeature fontFeature1, FontFeature fontFeature2)
        {
            return !(fontFeature1 == fontFeature2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != FontFeature::typeid)
            {
                return false;
            }

            return *this == safe_cast<FontFeature>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + nameTag.GetHashCode();
            hashCode = hashCode * 31 + parameter.GetHashCode();

            return hashCode;
        }

    };


    /// <summary>
    /// The GlyphMetrics structure specifies the metrics of an individual glyph.
    /// The units depend on how the metrics are obtained.
    /// </summary>
    public value struct GlyphMetrics
    {
        /// <summary>
        /// Specifies the X offset from the glyph origin to the left edge of the black box.
        /// The glyph origin is the current horizontal writing position.
        /// A negative value means the black box extends to the left of the origin (often true for lowercase italic 'f').
        /// </summary>
        property INT32 LeftSideBearing
        {
            INT32 get()
            {
                return leftSideBearing;
            }

            void set(INT32 value)
            {
                leftSideBearing = value;
            }
        }

        /// <summary>
        /// Specifies the X offset from the origin of the current glyph to the origin of the next glyph when writing horizontally.
        /// </summary>
        property UINT32 AdvanceWidth
        {
            UINT32 get()
            {
                return advanceWidth;
            }

            void set(UINT32 value)
            {
                advanceWidth = value;
            }
        }

        /// <summary>
        /// Specifies the X offset from the right edge of the black box to the origin of the next glyph when writing horizontally.
        /// The value is negative when the right edge of the black box overhangs the layout box.
        /// </summary>
        property INT32 RightSideBearing
        {
            INT32 get()
            {
                return rightSideBearing;
            }

            void set(INT32 value)
            {
                rightSideBearing = value;
            }
        }

        /// <summary>
        /// Specifies the vertical offset from the vertical origin to the top of the black box.
        /// Thus, a positive value adds whitespace whereas a negative value means the glyph overhangs the top of the layout box.
        /// </summary>
        property INT32 TopSideBearing
        {
            INT32 get()
            {
                return topSideBearing;
            }

            void set(INT32 value)
            {
                topSideBearing = value;
            }
        }

        /// <summary>
        /// Specifies the Y offset from the vertical origin of the current glyph to the vertical origin of the next glyph when writing vertically.
        /// (Note that the term "origin" by itself denotes the horizontal origin. The vertical origin is different.
        /// Its Y coordinate is specified by verticalOriginY value,
        /// and its X coordinate is half the advanceWidth to the right of the horizontal origin).
        /// </summary>
        property UINT32 AdvanceHeight
        {
            UINT32 get()
            {
                return advanceHeight;
            }

            void set(UINT32 value)
            {
                advanceHeight = value;
            }
        }

        /// <summary>
        /// Specifies the vertical distance from the black box's bottom edge to the advance height.
        /// Positive when the bottom edge of the black box is within the layout box.
        /// Negative when the bottom edge of black box overhangs the layout box.
        /// </summary>
        property INT32 BottomSideBearing
        {
            INT32 get()
            {
                return bottomSideBearing;
            }

            void set(INT32 value)
            {
                bottomSideBearing = value;
            }
        }

        /// <summary>
        /// Specifies the Y coordinate of a glyph's vertical origin, in the font's design coordinate system.
        /// The y coordinate of a glyph's vertical origin is the sum of the glyph's top side bearing
        /// and the top (i.e. yMax) of the glyph's bounding box.
        /// </summary>
        property INT32 VerticalOriginY
        {
            INT32 get()
            {
                return verticalOriginY;
            }

            void set(INT32 value)
            {
                verticalOriginY = value;
            }
        }
    private:

        INT32 leftSideBearing;
        UINT32 advanceWidth;
        INT32 rightSideBearing;
        INT32 topSideBearing;
        UINT32 advanceHeight;
        INT32 bottomSideBearing;
        INT32 verticalOriginY;

    public:

        static Boolean operator == (GlyphMetrics glyphMetrics1, GlyphMetrics glyphMetrics2)
        {
            return (glyphMetrics1.leftSideBearing == glyphMetrics2.leftSideBearing) &&
                (glyphMetrics1.advanceWidth == glyphMetrics2.advanceWidth) &&
                (glyphMetrics1.rightSideBearing == glyphMetrics2.rightSideBearing) &&
                (glyphMetrics1.topSideBearing == glyphMetrics2.topSideBearing) &&
                (glyphMetrics1.advanceHeight == glyphMetrics2.advanceHeight) &&
                (glyphMetrics1.bottomSideBearing == glyphMetrics2.bottomSideBearing) &&
                (glyphMetrics1.verticalOriginY == glyphMetrics2.verticalOriginY);
        }

        static Boolean operator != (GlyphMetrics glyphMetrics1, GlyphMetrics glyphMetrics2)
        {
            return !(glyphMetrics1 == glyphMetrics2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != GlyphMetrics::typeid)
            {
                return false;
            }

            return *this == safe_cast<GlyphMetrics>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + leftSideBearing.GetHashCode();
            hashCode = hashCode * 31 + advanceWidth.GetHashCode();
            hashCode = hashCode * 31 + rightSideBearing.GetHashCode();
            hashCode = hashCode * 31 + topSideBearing.GetHashCode();
            hashCode = hashCode * 31 + advanceHeight.GetHashCode();
            hashCode = hashCode * 31 + bottomSideBearing.GetHashCode();
            hashCode = hashCode * 31 + verticalOriginY.GetHashCode();

            return hashCode;
        }

    };


    /// <summary>
    /// Optional adjustment to a glyph's position. An glyph offset changes the position of a glyph without affecting
    /// the pen position. Offsets are in logical, pre-transform units.
    /// </summary>
    public value struct GlyphOffset
    {
        /// <summary>
        /// Constructor for FontFeature
        /// </summary>
        GlyphOffset (
            FLOAT advanceOffset,
            FLOAT ascenderOffset
            )
        {
            AdvanceOffset = advanceOffset;
            AscenderOffset = ascenderOffset;
        }

        /// <summary>
        /// Offset in the advance direction of the run. A positive advance offset moves the glyph to the right
        /// (in pre-transform coordinates) if the run is left-to-right or to the left if the run is right-to-left.
        /// </summary>
        property FLOAT AdvanceOffset
        {
            FLOAT get()
            {
                return advanceOffset;
            }

            void set(FLOAT value)
            {
                advanceOffset = value;
            }
        }

        /// <summary>
        /// Offset in the ascent direction, i.e., the direction ascenders point. A positive ascender offset moves
        /// the glyph up (in pre-transform coordinates).
        /// </summary>
        property FLOAT AscenderOffset
        {
            FLOAT get()
            {
                return ascenderOffset;
            }

            void set(FLOAT value)
            {
                ascenderOffset = value;
            }
        }
    private:

        FLOAT advanceOffset;
        FLOAT ascenderOffset;

    public:

        static Boolean operator == (GlyphOffset glyphOffset1, GlyphOffset glyphOffset2)
        {
            return (glyphOffset1.advanceOffset == glyphOffset2.advanceOffset) &&
                (glyphOffset1.ascenderOffset == glyphOffset2.ascenderOffset);
        }

        static Boolean operator != (GlyphOffset glyphOffset1, GlyphOffset glyphOffset2)
        {
            return !(glyphOffset1 == glyphOffset2);
        }

        virtual Boolean Equals(Object^ obj) override
        {
            if (obj->GetType() != GlyphOffset::typeid)
            {
                return false;
            }

            return *this == safe_cast<GlyphOffset>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + advanceOffset.GetHashCode();
            hashCode = hashCode * 31 + ascenderOffset.GetHashCode();

            return hashCode;
        }

    };


    ref class FontFace;
    /// <summary>
    /// Contains the information needed by renderers to draw glyph runs. 
    /// All coordinates are in device independent pixels (DIPs). 
    /// </summary>
    public value struct GlyphRun
    {
    public:

        /// <summary>
        /// The physical font face to draw with.
        /// </summary>
        property DirectWrite::FontFace^ FontFace
        {
            DirectWrite::FontFace^ get()
            {
                return fontFace;
            }

            void set(DirectWrite::FontFace^ value)
            {
                fontFace = value;
            }
        }

        /// <summary>
        /// Logical size of the font in DIPs, not points (equals 1/96 inch).
        /// </summary>
        property FLOAT FontEmSize
        {
            FLOAT get()
            {
                return fontEmSize;
            }

            void set(FLOAT value)
            {
                fontEmSize = value;
            }
        }

        /// <summary>
        /// The indices to render.
        /// </summary>    
        property IEnumerable<UINT16>^ GlyphIndexes
        {
            IEnumerable<UINT16>^ get()
            {
                return Array::AsReadOnly(glyphIndexes);
            }

            void set(IEnumerable<UINT16>^ value)
            {
                glyphIndexes = Enumerable::ToArray(value);
            }
        }

        /// <summary>
        /// Glyph advance widths.
        /// </summary>
        property IEnumerable<FLOAT>^ GlyphAdvances
        {
            IEnumerable<FLOAT>^ get()
            {
                return Array::AsReadOnly(glyphAdvances);
            }

            void set(IEnumerable<FLOAT>^ value)
            {
                glyphAdvances = Enumerable::ToArray(value);
            }
        }

        /// <summary>
        /// Glyph offsets.
        /// </summary>
        property IEnumerable<GlyphOffset>^ GlyphOffsets
        {
            IEnumerable<GlyphOffset>^ get()
            {
                return Array::AsReadOnly(glyphOffsets);
            }

            void set(IEnumerable<GlyphOffset>^ value)
            {
                glyphOffsets = Enumerable::ToArray(value);
            }
        }

        /// <summary>
        /// If true, specifies that glyphs are rotated 90 degrees to the left and
        /// vertical metrics are used. Vertical writing is achieved by specifying
        /// isSideways = true and rotating the entire run 90 degrees to the right
        /// via a rotate transform.
        /// </summary>
        property Boolean IsSideways
        {
            Boolean get()
            {
                return isSideways;
            }

            void set(Boolean value)
            {
                isSideways = value;
            }
        }


        // REVIEW: FxCop doesn't like "Bidi". We could replace with "Bidirectional".
        // Or, we could learn more about what this value really is, and try to encapsulate
        // it as something better than a plain uint.

        /// <summary>
        /// The implicit resolved bidi level of the run. Odd levels indicate
        /// right-to-left languages like Hebrew and Arabic, while even levels
        /// indicate left-to-right languages like English and Japanese (when
        /// written horizontally). For right-to-left languages, the text origin
        /// is on the right, and text should be drawn to the left.
        /// </summary>
        CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId="Bidi")
        property UInt32 BidiLevel
        {
            UInt32 get()
            {
                return bidiLevel;
            }

            void set(UInt32 value)
            {
                bidiLevel = value;
            }
        }

    private:

        array<UINT16>^ glyphIndexes;
        array<FLOAT>^ glyphAdvances;
        array<GlyphOffset>^ glyphOffsets;

        DirectWrite::FontFace^ fontFace;
        FLOAT fontEmSize;
        Boolean isSideways;
        UInt32 bidiLevel;

    internal:
        void CopyTo(DWRITE_GLYPH_RUN *pNativeStruct);
    };

    generic<typename T>
    public value class TextRangeOf
    {
    public:

        TextRangeOf(DirectWrite::TextRange range, T value)
            : range(range), value(value)
        { }

        property DirectWrite::TextRange TextRange
        {
            DirectWrite::TextRange get(void) { return range; }
        }

        property T Value
        {
            T get(void) { return value; }
        }

    private:

        DirectWrite::TextRange range;
        T value;
    };

} } } }