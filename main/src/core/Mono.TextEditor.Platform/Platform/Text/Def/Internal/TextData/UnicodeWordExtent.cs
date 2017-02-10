// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// This is a little buffer over the contents of a line, so that we avoid fetching the entirety of giant lines when we really
    /// just need to look at a small local area.
    /// </summary>
    public class LineBuffer
    {
        public static int BufferSize = 1024;   // this is non-constant so unit tests can change it to better test LineBuffer logic

        private readonly ITextSnapshotLine line;

        /// <summary>
        /// Current window into the line. Size is less than or equal to BufferSize.
        /// </summary>
        private string contents;

        /// <summary>
        /// Bounds of the current contents, 0-origin with respect to the line.
        /// </summary>
        private Span extent; 

        public LineBuffer(ITextSnapshotLine line)
        {
            this.line = line;
            if (line.LengthIncludingLineBreak <= BufferSize)
            {
                this.contents = line.GetTextIncludingLineBreak();
                this.extent = new Span(0, line.LengthIncludingLineBreak);
            }
            else
            {
                this.extent = new Span(0, 0);
            }
        }

        public char this[int index]
        {
            get
            {
                if (!this.extent.Contains(index))
                {
                    // shift the window so that it is centered on "index"
                    int start = Math.Max(0, index - BufferSize / 2);
                    int end = Math.Min(line.LengthIncludingLineBreak, start + BufferSize);
                    this.extent = Span.FromBounds(start, end);
                    this.contents = line.Snapshot.GetText(line.Start + start, this.extent.Length);
                    return this.contents[index - this.extent.Start];
                }
                return this.contents[index - this.extent.Start];
            }
        }
    }
    
    public class UnicodeWordExtent
    {
        static public bool FindCurrentToken(SnapshotPoint currentPosition, out SnapshotSpan span)
        {
            ITextSnapshotLine textSnapshotLine = currentPosition.GetContainingLine();
            LineBuffer lineText = new LineBuffer(textSnapshotLine);
            int lineLength = textSnapshotLine.LengthIncludingLineBreak;
            int iCol = (currentPosition - textSnapshotLine.Start);

            // handle end of buffer case
            if (iCol >= lineLength)
            {
                span = new SnapshotSpan(currentPosition.Snapshot, textSnapshotLine.End, 0);
                return true;
            }

            // scan left for base char
            while ((iCol > 0) && !IsGraphemeBreak(lineText, iCol))
            {
                iCol--;
            }

            // if it's a word, return the word
            char ch = lineText[iCol];
            if (IsWordChar(ch))
            {
                return FindCurrentWordCoords(new SnapshotPoint(currentPosition.Snapshot, textSnapshotLine.Start + iCol), out span);
            }

            // contiguous whitespace
            int iBeg;
            if (Char.IsWhiteSpace(ch))
            {
                for (iBeg = iCol - 1; iBeg >= 0; --iBeg)
                {
                    if (!Char.IsWhiteSpace(lineText[iBeg]))
                        break;
                }
                iBeg++;

                for (++iCol; iCol < lineLength; ++iCol)
                {
                    if (!Char.IsWhiteSpace(lineText[iCol]))
                        break;
                }
            }

            // contiguous punctuation and math symbols
            else if (Char.IsPunctuation(ch) || IsMathSymbol(ch))
            {
                for (iBeg = iCol - 1; iBeg >= 0; --iBeg)
                {
                    ch = lineText[iBeg];
                    if (!(Char.IsPunctuation(ch) || IsMathSymbol(ch)))
                        break;
                }
                iBeg++;

                for (++iCol; iCol < lineLength; ++iCol)
                {
                    ch = lineText[iCol];
                    if (!(Char.IsPunctuation(ch) || IsMathSymbol(ch)))
                        break;
                }
            }

            //Let's get the whole surrogate pair.
            else if (Char.IsHighSurrogate(ch) && ((iCol + 1) < lineLength) && Char.IsLowSurrogate(lineText[iCol + 1]))
            {
                iBeg = iCol;
                iCol += 2;
            }

            // any other single char
            else
            {
                iBeg = iCol++;
            }

            if (iCol > lineLength)
            {
                iCol = lineLength;
            }

            // Done -- fill in the data
            span = new SnapshotSpan(currentPosition.Snapshot, (textSnapshotLine.Start + iBeg), (iCol - iBeg));

            return true;
        }

        //---------------------------------------------------------------------------
        // FindCurrentWordCoords
        //---------------------------------------------------------------------------
        public static bool FindCurrentWordCoords(SnapshotPoint currentPosition, out SnapshotSpan span)
        {
            span = new SnapshotSpan(currentPosition, 0);

            ITextSnapshotLine textSnapshotLine = currentPosition.GetContainingLine();
            LineBuffer lineText = new LineBuffer(textSnapshotLine);
            int lineLength = textSnapshotLine.LengthIncludingLineBreak;
            int iCol = (currentPosition - textSnapshotLine.Start);

            // scan left for base char
            while ((iCol > 0) && !IsGraphemeBreak(lineText, iCol))
            {
                iCol--;
            }

            if ((iCol == lineLength) || !IsWordChar(lineText[iCol]))
            {
                if (iCol == 0 || !IsWordChar(lineText[iCol - 1]))
                {
                    return false;
                }
                iCol--;
            }

            // Find the beginning
            int iBeg;
            for (iBeg = iCol; iBeg >= 0; --iBeg)
            {
                if (IsWordBreak(lineText, iBeg, false))
                    break;
            }

            // Find the end
            for (++iCol; iCol < lineLength; ++iCol)
            {
                if (IsWordBreak(lineText, iCol, false))
                    break;
            }

            // Done -- fill in the data
            span = new SnapshotSpan(currentPosition.Snapshot, (textSnapshotLine.Start + iBeg), (iCol - iBeg));

            return true;
        }

        public static bool IsWordBreak(LineBuffer line, int iChar, bool fHanWordBreak)
        {
            if (iChar <= 0)
                return true;

            if (!IsGraphemeBreak(line, iChar))
                return false;

            char cR = line[iChar];

            int iCharPrev = PrevChar(line, iChar);
            if (iCharPrev < 0)
                return true;
            char cL = line[iCharPrev];

            if (IsNonBreaking(cL) || IsNonBreaking(cR))
                return false;

            return IsPropBreak(cL, cR, fHanWordBreak);
        }

        public static int PrevChar(LineBuffer line, int iChar)
        {
            if (iChar <= 0)
                return -1;

            for (--iChar; !IsGraphemeBreak(line, iChar); --iChar) ;

            return iChar;
        }

        // Returns true if iChar is at a grapheme boundary
        public static bool IsGraphemeBreak(LineBuffer line, int iChar)
        {
            if (iChar <= 0)
                return true;

            char cR = line[iChar];
            if (cR == 0)
                return true;

            char cL = line[(iChar - 1)];
            if (cL == 0)
                return true;

            // break around line breaks
            if (IsLineBreak(cL) || IsLineBreak(cR))
                return true;

            // don't separate a combining char from it's base
            if (IsCombining(cR))
            {
                // A combining character after a quote or whitespace does not combine with the quote or whitespace
                if (cL != '\'' && cL != '\"' && !Char.IsWhiteSpace(cL) && Char.IsLetter(cL))
                {
                    return false;
                }

            }

            // don't break surrogate pairs
            if (Char.IsHighSurrogate(cL))
            {
                return !Char.IsLowSurrogate(cR);
            }

            HangulJamoType hjL = GetHangulJamoType(cL);
            HangulJamoType hjR = GetHangulJamoType(cR);
            if ((hjL != HangulJamoType.Other) && (hjR != HangulJamoType.Other))
            {
                switch (hjL)
                {
                    case HangulJamoType.Lead:
                        return false;
                    case HangulJamoType.Vowel:
                        return (HangulJamoType.Lead == hjR);
                    case HangulJamoType.Trail:
                        return (HangulJamoType.Trail != hjR);
                    default:
                        break;
                }
            }
            return true;
        }

        // Modified algorithm from:
        // [U2] 5.13 <Locating Text Element Boundaries> 'Word Boundaries'
        //
        // The rule notation here uses "//" for the italic double dagger used in [U2],
        // and ! for the 'not' symbol.
        //
        // We include decimal digits and LOW LINE ('_') in the definition of a 'word'
        //
        public static bool IsPropBreak(char cL, char cR, bool fHanWordBreak)
        {
            UnicodeCategory R = Char.GetUnicodeCategory(cR);
            if (IsPropCombining(R))
                return false;

            // Rule (1 usually does not occur (no line/para bounds in the text we're searching)
            // or will be caught by (2 and (3.

            // Rule (1  Para //
            // Rule (2  !Let // Let
            // Rule (3  Let  // !Let
            bool fL = IsWordChar(cL);
            bool fR = IsWordChar(cR);
            if (fL != fR)
                return true;

            // break between any two non-word-chars
            if (!fL && !fR)
                return true;

            // Break between characters of different scripts (e.g. Arabic/English)
            // unless one is a letter & the other is a digit
            UnicodeScript LScript = UScript(cL);
            UnicodeScript RScript = UScript(cR);
            if (LScript != RScript)
            {
                UnicodeCategory L = Char.GetUnicodeCategory(cL);
                if ((IsPropAlpha(L) && IsPropAlpha(R)) ||
                    (IsPropDigit(L) && IsPropDigit(R)))
                    return true;
            }

            if ((LScript == UnicodeScript.CJK) || (RScript == UnicodeScript.CJK))
            {
                bool fLHan = IsIdeograph(cL);
                bool fRHan = IsIdeograph(cR);
                if (fHanWordBreak && (fLHan || fRHan))
                    return true;

                // Rule (4  !(Hira|Kata|Han) // Hira|Kata|Han
                if ((LScript != UnicodeScript.CJK) && (RScript == UnicodeScript.CJK))
                    return true;

                // Rule (5  Hira // !Hira
                bool fRHiragana = IsHiragana(cR);
                if (IsHiragana(cL) && !fRHiragana)
                    return true;

                // Rule (6  Kata // !(Hira|Kata)
                if (IsKatakana(cL) && !(fRHiragana || IsKatakana(cR)))
                    return true;

                // Rule (7  Han // !(Hira|Han)
                if (fLHan && !(fRHan || fRHiragana))
                    return true;
            }

            return false;
        }

        public static bool IsWordChar(char ch)
        {
            return Char.IsLetterOrDigit(ch) || (ch == '_') || (ch == '$');
        }

        /// <summary>
        /// Logic from vscommon\unilib\uniword.cpp
        /// </summary>
        public static bool IsWholeWord(SnapshotSpan candidate, bool acceptHanWordBreak = true)
        {
            ITextSnapshotLine line = candidate.Start.GetContainingLine();
            LineBuffer lineBuffer = null;

            // A word that spans over two lines is not a whole word
            if (line.Extent.End < candidate.End)
            {
                return false;
            }

            bool isLeftBreak = candidate.Start == 0;
            bool isRightBreak = candidate.End == candidate.Snapshot.Length;

            if (!isLeftBreak || !isRightBreak)
            {
                lineBuffer = new LineBuffer(line);
            }

            if (!isLeftBreak)
            {
                isLeftBreak = IsWordBreak(lineBuffer, candidate.Start - line.Start, acceptHanWordBreak);

                if (!isLeftBreak)
                {
                    return false;
                }
            }

            if (!isRightBreak)
            {
                isRightBreak = IsWordBreak(lineBuffer, candidate.End - line.Start, acceptHanWordBreak);
            }

            return isLeftBreak && isRightBreak;
        }

        public static bool IsNonBreaking(char ch)
        {
            return ((ch == 0x00a0) ||   // NO-BREAK SPACE
                    (ch == 0x2011) ||   // NON-BREAKING HYPHEN
                    (ch == 0x202F) ||   // NARROW NO-BREAK SPACE
                    (ch == 0x30FC) ||   // Japanese NO-BREAK character a bit like a long hyphen
                    (ch == 0xfeff));    // ZERO WIDTH NO-BREAK SPACE (byte order mark)
        }

        public const char UCH_LF  = (char)0x000A;
        public const char UCH_CR  = (char)0x000D;
        public const char UCH_LS  = (char)0x2028;
        public const char UCH_PS  = (char)0x2029;
        public const char UCH_NEL = (char)0x0085;
        public static bool IsLineBreak(char ch)
        {
            return (ch <= UCH_PS) && (UCH_CR == ch || UCH_LF == ch || UCH_LS == ch || UCH_PS == ch || UCH_NEL == ch);
        }

        public enum UnicodeScript
        {
            // Pre-Unicode 3.0 scripts

            NONE                = 0x0000,   // punctuation, dingbat, symbol, math, ...
            LATIN               = 0x0001,
            GREEK               = 0x0002,
            CYRILLIC            = 0x0003,
            ARMENIAN            = 0x0004,
            HEBREW              = 0x0005,
            ARABIC              = 0x0006,
            DEVANAGARI          = 0x0007,
            BANGLA             = 0x0008,
            GURMUKHI            = 0x0009,
            GUJARATI            = 0x000a,
            ODIA               = 0x000b,
            TAMIL               = 0x000c,
            TELUGU              = 0x000d,
            KANNADA             = 0x000e,
            MALAYALAM           = 0x000f,
            THAI                = 0x0010,
            LAO                 = 0x0011,
            TIBETAN             = 0x0012,
            GEORGIAN            = 0x0013,
            CJK                 = 0x0014,   // Chinese, Japanese, Korean (Han, Katakana, Hiragana, Hangul)

            // Unicode 3.0 added scripts

            BRAILLE             = 0x0015,
            SYRIAC              = 0x0016,
            THAANA              = 0x0017,
            SINHALA             = 0x0018,
            MYANMAR             = 0x0019,
            ETHIOPIC            = 0x001a,
            CHEROKEE            = 0x001b,
            CANADIAN_ABORIGINAL = 0x001c,
            OGHAM               = 0x001d,
            RUNIC               = 0x001e,
            KHMER               = 0x001f,
            MONGOLIAN           = 0x0020,
            YI                  = 0x0021
        }

        public static UnicodeScript UScript(char ch)
        {
            if (ch <= 0x024F) return UnicodeScript.LATIN;                       // 0x0000, 0x007F, Basic Latin
                                                                                // 0x0080, 0x00FF, Latin-1 Supplement
                                                                                // 0x0100, 0x017F, Latin Extended-A
                                                                                // 0x0180, 0x024F, Latin Extended-B
            if (ch < 0x2000)
            {
                if (ch < 0x1000)
                {
                    if (ch <  0x0370) return UnicodeScript.NONE;                // 0x0250, 0x02AF, IPA Extensions
                                                                                // 0x02B0, 0x02FF, Spacing Modifier Letters
                                                                                // 0x0300, 0x036F, Combining Diacritical Marks
                    if (ch <  0x0400) return UnicodeScript.GREEK;               // 0x0370, 0x03FF, Greek
                    if (ch <= 0x04FF) return UnicodeScript.CYRILLIC;            // 0x0400, 0x04FF, Cyrillic
                    if (ch <  0x0530) return UnicodeScript.NONE;                // 0x0500, 0x052F, NONE
                    if (ch <  0x0590) return UnicodeScript.ARMENIAN;            // 0x0530, 0x058F, Armenian
                    if (ch <  0x0600) return UnicodeScript.HEBREW;              // 0x0590, 0x05FF, Hebrew
                    if (ch <  0x0700) return UnicodeScript.ARABIC;              // 0x0600, 0x06FF, ARABIC
                    if (ch <= 0x074F) return UnicodeScript.SYRIAC;              // 0x0700, 0x074F, SYRIAC  
                    if (ch <  0x0780) return UnicodeScript.NONE;                // 0x0750, 0x077F, NONE
                    if (ch <= 0x07BF) return UnicodeScript.THAANA;              // 0x0780, 0x07BF, THAANA
                    if (ch <  0x0900) return UnicodeScript.NONE;                // 0x07C0, 0x08FF, NONE
                    if (ch <  0x0980) return UnicodeScript.DEVANAGARI;          // 0x0900, 0x097F, DEVANAGARI
                    if (ch <  0x0A00) return UnicodeScript.BANGLA;             // 0x0980, 0x09FF, BANGLA
                    if (ch <  0x0A80) return UnicodeScript.GURMUKHI;            // 0x0A00, 0x0A7F, GURMUKHI
                    if (ch <  0x0B00) return UnicodeScript.GUJARATI;            // 0x0A80, 0x0AFF, GUJARATI
                    if (ch <  0x0B80) return UnicodeScript.ODIA;               // 0x0B00, 0x0B7F, ODIA
                    if (ch <  0x0C00) return UnicodeScript.TAMIL;               // 0x0B80, 0x0BFF, TAMIL
                    if (ch <  0x0C80) return UnicodeScript.TELUGU;              // 0x0C00, 0x0C7F, TELUGU
                    if (ch <  0x0D00) return UnicodeScript.KANNADA;             // 0x0C80, 0x0CFF, KANNADA
                    if (ch <  0x0D80) return UnicodeScript.MALAYALAM;           // 0x0D00, 0x0D7F, MALAYALAM
                    if (ch <  0x0E00) return UnicodeScript.SINHALA;             // 0x0D80, 0x0DFF, SINHALA
                    if (ch <  0x0E80) return UnicodeScript.THAI;                // 0x0E00, 0x0E7F, THAI
                    if (ch <  0x0F00) return UnicodeScript.LAO;                 // 0x0E80, 0x0EFF, LAO

                    return UnicodeScript.TIBETAN;                               // 0x0F00, 0x0FFF, TIBETAN
                }
                else
                {
                    if (ch <  0x10A0) return UnicodeScript.MYANMAR;             // 0x1000, 0x109F, Myanmar 
                    if (ch <  0x1100) return UnicodeScript.GEORGIAN;            // 0x10A0, 0x10FF, Georgian
                    if (ch <  0x1200) return UnicodeScript.CJK;                 // 0x1100, 0x11FF, Hangul Jamo
                    if (ch <  0x13A0) return UnicodeScript.ETHIOPIC;            // 0x1200, 0x139F, Ethiopic
                    if (ch <  0x1400) return UnicodeScript.CHEROKEE;            // 0x13A0, 0x13FF, Cherokee
                    if (ch <  0x1680) return UnicodeScript.CANADIAN_ABORIGINAL; // 0x1400, 0x167F, Unified Canadian Aboriginal Syllabics
                    if (ch <  0x16A0) return UnicodeScript.OGHAM;               // 0x1680, 0x169F, Ogham
                    if (ch <  0x1780) return UnicodeScript.RUNIC;               // 0x16A0, 0x177F, Runic
                    if (ch <  0x1800) return UnicodeScript.KHMER;               // 0x1780, 0x17FF, Khmer
                    if (ch <= 0x18AF) return UnicodeScript.MONGOLIAN;           // 0x1800, 0x18AF, Mongolian
                    if (ch <  0x1E00) return UnicodeScript.NONE;                // 0x18B0, 0x1DFF, NONE
                    if (ch <  0x1F00) return UnicodeScript.LATIN;               // 0x1E00, 0x1EFF, Latin Extended Additional

                    return UnicodeScript.GREEK;                                 // 0x1F00, 0x1FFF, Greek Extended
                }
            }
            if (ch < 0xD800)
            {
                if (ch <= 0x27FF) return UnicodeScript.NONE;                    // 0x2000, 0x206F, General Punctuation
                                                                                // 0x2070, 0x209F, Superscripts and Subscripts
                                                                                // 0x20A0, 0x20CF, Currency Symbols
                                                                                // 0x20D0, 0x20FF, Combining Marks for Symbols
                                                                                // 0x2100, 0x214F, Letterlike Symbols
                                                                                // 0x2150, 0x218F, Number Forms
                                                                                // 0x2190, 0x21FF, Arrows
                                                                                // 0x2200, 0x22FF, Mathematical Operators
                                                                                // 0x2300, 0x23FF, Miscellaneous Technical
                                                                                // 0x2400, 0x243F, Control Pictures
                                                                                // 0x2440, 0x245F, Optical Character Recognition
                                                                                // 0x2460, 0x24FF, Enclosed Alphanumerics
                                                                                // 0x2500, 0x257F, Box Drawing
                                                                                // 0x2580, 0x259F, Block Elements
                                                                                // 0x25A0, 0x25FF, Geometric Shapes
                                                                                // 0x2600, 0x26FF, Miscellaneous Symbols
                                                                                // 0x2700, 0x27BF, Dingbats
                if (ch <= 0x28FF) return UnicodeScript.BRAILLE;                 // 0x2800, 0x28FF, Braille Patterns
                if (ch <  0x2E80) return UnicodeScript.NONE;                    // 0x2900, 0x2E7F, NONE
                if (ch <= 0x31BF) return UnicodeScript.CJK;                     // 0x2E80, 0x2EFF, CJK Radicals Supplement
                                                                                // 0x2F00, 0x2FDF, Kangxi Radicals
                                                                                // 0x2FF0, 0x2FFF, Ideographic Description Characters
                                                                                // 0x3000, 0x303F, CJK Symbols and Punctuation
                                                                                // 0x3040, 0x309F, Hiragana
                                                                                // 0x30A0, 0x30FF, Katakana
                                                                                // 0x3100, 0x312F, Bopomofo
                                                                                // 0x3130, 0x318F, Hangul Compatibility Jamo
                                                                                // 0x3190, 0x319F, Kanbun
                                                                                // 0x31A0, 0x31BF, Bopomofo Extended
                if (ch <  0x3200) return UnicodeScript.NONE;                    // 0x31C0, 0x31FF, NONE
                if (ch <= 0x4DBf) return UnicodeScript.CJK;                     // 0x3200, 0x32FF, Enclosed CJK Letters and Months
                                                                                // 0x3300, 0x33FF, CJK Compatibility
                                                                                // 0x3400, 0x4DB5, CJK Unified Ideographs Extension A
                if (ch <  0x4E00) return UnicodeScript.NONE;                    // 0x4DC0, 0x3DFF, NONE
                if (ch <= 0x9FFF) return UnicodeScript.CJK;                     // 0x4E00, 0x9FFF, CJK Unified Ideographs
                if (ch <= 0xA4CF) return UnicodeScript.YI;                      // 0xA000, 0xA48F, Yi Syllables
                                                                                // 0xA490, 0xA4CF, Yi Radicals
                if (ch <  0xAC00) return UnicodeScript.NONE;                    // 0xA4D0, 0xABFF, NONE
                if (ch <= 0xD7A3) return UnicodeScript.CJK;                     // 0xAC00, 0xD7A3, Hangul Syllables

                return UnicodeScript.NONE;                                      // 0xD7A4, 0xD7FF, NONE
            }
            if (ch <  0xF900) return UnicodeScript.NONE;                        // 0xD800, 0xDB7F, High Surrogates
                                                                                // 0xDB80, 0xDBFF, High Private Use Surrogates
                                                                                // 0xDC00, 0xDFFF, Low Surrogates
                                                                                // 0xE000, 0xF8FF, Private Use
            if (ch <  0xFB00) return UnicodeScript.CJK;                         // 0xF900, 0xFAFF, CJK Compatibility Ideographs
            if (ch <  0xFB4F) return UnicodeScript.LATIN;                       // 0xFB00, 0xFB4F, Alphabetic Presentation Forms
            if (ch <  0xFE00) return UnicodeScript.ARABIC;                      // 0xFB50, 0xFDFF, Arabic Presentation Forms-A
            if (ch <  0xFE30) return UnicodeScript.NONE;                        // 0xFE20, 0xFE2F, Combining Half Marks
            if (ch <  0xFE50) return UnicodeScript.CJK;                         // 0xFE30, 0xFE4F, CJK Compatibility Forms
            if (ch <  0xFE70) return UnicodeScript.NONE;                        // 0xFE50, 0xFE6F, Small Form Variants
            if (ch <  0xFEFF) return UnicodeScript.ARABIC;                      // 0xFE70, 0xFEFE, Arabic Presentation Forms-B
            if (ch <  0xFFF0)
            {
                if (ch == 0xFEFF) return UnicodeScript.NONE;                    // 0xFEFF, 0xFEFF, Specials

                                                                                // 0xFF00, 0xFF00, Halfwidth and Fullwidth Forms (FALLTHROUGH)

                if ((ch >= 0xFF01) && (ch <= 0xFF5E))
                    return UnicodeScript.LATIN;                                 // 0xFF01, 0xFF5E, LATIN

                return UnicodeScript.CJK;                                       // 0xFF5E, 0xFFEF, Halfwidth and Fullwidth Forms
            }

            return UnicodeScript.NONE;                                          // 0xFFF0, 0xFFFD, Specials
                                                                                // 0xFFFeE 0xFFFF, NONE
        }

        //public static bool IsHangul(char ch)
        //{
        //    bool f = false;
        //    if      (ch <  0x1100) {}
        //    else if (ch <= 0x11FF) f = true;  // (0x1100 - 0x11FF) Hangul Combining Jamo
        //    else if (ch <  0x3130) {}
        //    else if (ch <= 0x318f) f = true;  // (0x3130 - 0x318f) Hangul Compatability Jamo
        //    else if (ch <  0xac00) {}
        //    else if (ch <= 0xd7a3) f = true;  // (0xac00 - 0xd7a3) Hangul Syllables
        //    else if (ch <  0xffa0) {}
        //    else if (ch <= 0xffdf) f = true;  // (0xffa0 - 0xffdf) Halfwidth Hangul Compatability Jamo
        //    return f;
        //}

        public static bool IsKatakana(char ch)
        {
            // Odd short circuit logic but directly ported from unilib
            return ((ch >= 0x3031) &&
                    (((ch >= 0x3099) && (ch <= 0x30fe)) ||
                     ((ch >= 0xff66) && (ch <= 0xff9f))));
        }

        public static bool IsHiragana(char ch)
        {
            // Odd short circuit logic but directly ported from unilib
            return ((ch >= 0x3031) &&
                    (((ch >= 0x3041) && (ch <= 0x309e)) ||
                     (0x30fc == ch) || (0xff70 == ch))); // fullwidth and halfwidth hira/kata prolonged sound mark
        }

        public static bool IsIdeograph(char ch)
        {
            bool f = false;
            if      (ch <  0x4e00) { if (ch == 0x3005) f = true; }
            else if (ch <= 0x9fa5) f = true;
            else if (ch <  0xf900) {}
            else if (ch <= 0xfa2d) f = true;
            return f;
        }

        public static bool IsMathSymbol(char ch)
        {
            return (Char.GetUnicodeCategory(ch) == UnicodeCategory.MathSymbol);
        }

        public static bool IsCombining(char ch)
        {
            return IsPropCombining(Char.GetUnicodeCategory(ch));
        }

        public static bool IsPropCombining(UnicodeCategory cat)
        {
            return ((cat >= UnicodeCategory.NonSpacingMark) && (cat <= UnicodeCategory.EnclosingMark));
        }

        public static bool IsPropAlpha(UnicodeCategory cat)
        {
            return (cat <= UnicodeCategory.OtherLetter);
        }
        public static bool IsPropDigit(UnicodeCategory cat)
        {
            return (cat == UnicodeCategory.DecimalDigitNumber);
        }

        public const char UCH_HANGUL_JAMO_FIRST       = (char)0x1100;
        public const char UCH_HANGUL_JAMO_LEAD_FIRST  = (char)0x1100;
        public const char UCH_HANGUL_JAMO_LEAD_LAST   = (char)0x115F;
        public const char UCH_HANGUL_JAMO_VOWEL_FIRST = (char)0x1160;
        public const char UCH_HANGUL_JAMO_VOWEL_LAST  = (char)0x11A2;
        public const char UCH_HANGUL_JAMO_TRAIL_FIRST = (char)0x11A8;
        public const char UCH_HANGUL_JAMO_TRAIL_LAST  = (char)0x11F9;
        public const char UCH_HANGUL_JAMO_LAST        = (char)0x11FF;
        //public static bool IsHangulJamo(char ch)
        //{
        //    return ((ch >= UCH_HANGUL_JAMO_FIRST) && (ch <= UCH_HANGUL_JAMO_LAST));
        //}

        public enum HangulJamoType
        {
            Other = 0,
            Lead = 1,
            Vowel = 2,
            Trail = 3
        }

        public static HangulJamoType GetHangulJamoType(char ch)
        {
            if (ch < UCH_HANGUL_JAMO_FIRST)
                return HangulJamoType.Other;
            if (ch > UCH_HANGUL_JAMO_LAST)
                return HangulJamoType.Other;
            if (ch <= UCH_HANGUL_JAMO_LEAD_LAST)
                return HangulJamoType.Lead;
            if (ch <= UCH_HANGUL_JAMO_VOWEL_LAST)
                return HangulJamoType.Vowel;
            return HangulJamoType.Trail;
        }

    }
}
