//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace WindowsImagingComponent {

using namespace System;

// REVIEW: if we were willing to link to the WIC library to get the GUID definitions,
// the below could avoid hard-coding GUID values, and just use the pre-defined constants,
// by calling the Utilities::Convert::SystemGuidFromGUID() method

/// <summary>
/// A listing of pixel formats supported by the Windows Imaging Component.
/// </summary>
public ref class PixelFormats sealed
{
public:
    /* undefined pixel formats */

    /// <summary>
    /// A pixel format used when the actual pixelformat is unimportant to an image operation.
    /// </summary>
    static property Guid DoNotCare
    {
        Guid get(void) { return doNotCare; }
    }

    
    /* indexed pixel formats */

    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 1, Bits Per Pixel = 1, Storage Type = uint
    /// The value for each pixel is an index into a color palette.
    /// </summary>
    static property Guid Indexed1Bpp
    {
        Guid get(void) { return indexed1Bpp; }
    }
    
    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 2, Bits Per Pixel = 2, Storage Type = uint
    /// The value for each pixel is an index into a color palette.
    /// </summary>
    static property Guid Indexed2Bpp
    {
        Guid get(void) { return indexed2Bpp; }
    }
    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 4, Bits Per Pixel = 4, Storage Type = uint
    /// The value for each pixel is an index into a color palette.
    /// </summary>
    static property Guid Indexed4Bpp
    {
        Guid get(void) { return indexed4Bpp; }
    }
    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 8, Bits Per Pixel = 8, Storage Type = uint
    /// The value for each pixel is an index into a color palette.
    /// </summary>
    static property Guid Indexed8Bpp
    {
        Guid get(void) { return indexed8Bpp; }
    }

    
    /* Packed Bit Pixel Formats */ 

    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 5, Bits Per Pixel = 16, Storage Type = uint
    /// Color-channel data crosses byte boundaries.
    /// </summary>
    static property Guid Bgr55516Bpp
    {
        Guid get(void) { return bgr55516Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel =     5(B)/6(G)/5(R), Bits Per Pixel = 16, Storage Type = uint
    /// Color-channel data crosses byte boundaries.
    /// </summary>
    static property Guid Bgr56516Bpp
    {
        Guid get(void) { return bgr56516Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 10, Bits Per Pixel = 32, Storage Type = uint
    /// Color-channel data crosses byte boundaries.
    /// </summary>
    static property Guid Bgr10101032Bpp
    {
        Guid get(void) { return bgr10101032Bpp; }
    }


    /* Grayscale Pixel Formats */

    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 1, Bits Per Pixel = 1, Storage Type = uint
    /// Color data represents shades of gray.
    /// </summary>
    static property Guid BlackWhite
    {
        Guid get(void) { return blackWhite; }
    }
    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 2, Bits Per Pixel = 2, Storage Type = uint
    /// Color data represents shades of gray.
    /// </summary>
    static property Guid Gray2Bpp
    {
        Guid get(void) { return gray2Bpp; }
    }
    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 4, Bits Per Pixel = 4, Storage Type = uint
    /// Color data represents shades of gray.
    /// </summary>
    static property Guid Gray4Bpp
    {
        Guid get(void) { return gray4Bpp; }
    }
    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 8, Bits Per Pixel = 8, Storage Type = uint
    /// Color data represents shades of gray.
    /// </summary>
    static property Guid Gray8Bpp
    {
        Guid get(void) { return gray8Bpp; }
    }
    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 16, Bits Per Pixel = 16, Storage Type = uint
    /// Color data represents shades of gray.
    /// </summary>
    static property Guid Gray16Bpp
    {
        Guid get(void) { return gray16Bpp; }
    }
    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 16, Bits Per Pixel = 16, Storage Type = FixedPoint
    /// Color data represents shades of gray.
    /// </summary>
    static property Guid GrayFixedPoint16Bpp
    {
        Guid get(void) { return grayFixedPoint16Bpp; }
    }
    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 16, Bits Per Pixel = 16, Storage Type = float
    /// Color data represents shades of gray.
    /// </summary>
    static property Guid GrayHalf16Bpp
    {
        Guid get(void) { return grayHalf16Bpp; }
    }
    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 32, Bits Per Pixel = 32, Storage Type = float
    /// Color data represents shades of gray.
    /// </summary>
    static property Guid GrayFloat32Bpp
    {
        Guid get(void) { return grayFloat32Bpp; }
    }
    /// <summary>
    /// Channel Count = 1, Bits Per Channel = 32, Bits Per Pixel = 32, Storage Type = FixedPoint
    /// Color data represents shades of gray.
    /// </summary>
    static property Guid GrayFixedPoint32Bpp
    {
        Guid get(void) { return grayFixedPoint32Bpp; }
    }

    
    /* RGB/BGR Pixel formats */

    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 8, Bits Per Pixel = 24, Storage Type = uint
    /// Color data is separated into red(R), green(G), and blue(B) channels.
    /// </summary>
    static property Guid Bgr24Bpp
    {
        Guid get(void) { return bgr24Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 8, Bits Per Pixel = 24, Storage Type = uint
    /// Color data is separated into red(R), green(G), and blue(B) channels.
    /// </summary>
    static property Guid Rgb24Bpp
    {
        Guid get(void) { return rgb24Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 8, Bits Per Pixel = 32, Storage Type = uint
    /// Color data is separated into red(R), green(G), and blue(B) channels.
    /// </summary>
    static property Guid Bgr32Bpp
    {
        Guid get(void) { return bgr32Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 16, Bits Per Pixel = 48, Storage Type = uint
    /// Color data is separated into red(R), green(G), and blue(B) channels.
    /// </summary>
    static property Guid Rgb48Bpp
    {
        Guid get(void) { return rgb48Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 16, Bits Per Pixel = 48, Storage Type = Fixed
    /// Color data is separated into red(R), green(G), and blue(B) channels.
    /// </summary>
    static property Guid RgbFixedPoint48Bpp
    {
        Guid get(void) { return rgbFixedPoint48Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 16, Bits Per Pixel = 48, Storage Type = float
    /// Color data is separated into red(R), green(G), and blue(B) channels.
    /// </summary>
    static property Guid RgbHalf48Bpp
    {
        Guid get(void) { return rgbHalf48Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 16, Bits Per Pixel = 64, Storage Type = Fixed
    /// Color data is separated into red(R), green(G), and blue(B) channels.
    /// </summary>
    static property Guid RgbFixedPoint64Bpp
    {
        Guid get(void) { return rgbFixedPoint64Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 16, Bits Per Pixel = 64, Storage Type = float
    /// Color data is separated into red(R), green(G), and blue(B) channels.
    /// </summary>
    static property Guid RgbHalf64Bpp
    {
        Guid get(void) { return rgbHalf64Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 32, Bits Per Pixel = 96, Storage Type = Fixed
    /// Color data is separated into red(R), green(G), and blue(B) channels.
    /// </summary>
    static property Guid RgbFixedPoint96Bpp
    {
        Guid get(void) { return rgbFixedPoint96Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 32, Bits Per Pixel = 128, Storage Type = float
    /// Color data is separated into red(R), green(G), and blue(B) channels.
    /// </summary>
    static property Guid RgbFloat128Bpp
    {
        Guid get(void) { return rgbFloat128Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 32, Bits Per Pixel = 128, Storage Type = Fixed
    /// Color data is separated into red(R), green(G), and blue(B) channels.
    /// </summary>
    static property Guid RgbFixedPoint128Bpp
    {
        Guid get(void) { return rgbFixedPoint128Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 8, Bits Per Pixel = 32, Storage Type = uint
    /// Color data is separated into red(R), green(G), blue(B), and alpha(A) channels.
    /// </summary>
    static property Guid Bgra32Bpp
    {
        Guid get(void) { return bgra32Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 8, Bits Per Pixel = 32, Storage Type = uint
    /// Color data is separated into red(R), green(G), blue(B), and alpha(A) channels.
    /// </summary>
    static property Guid Rgba32Bpp
    {
        Guid get(void) { return rgba32Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 8, Bits Per Pixel = 32, Storage Type = uint
    /// Color data is separated into red(R), green(G), blue(B), and exponent(E) channels.
    /// This format has an additional byte for shared exponent information. This provides 
    /// additional floating-point precision without the additional memory needed for a true 
    /// floating-point format.
    /// </summary>
    static property Guid Rgbe32Bpp
    {
        Guid get(void) { return rgbe32Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 8, Bits Per Pixel = 32, Storage Type = uint
    /// Color data is separated into red(R), green(G), blue(B), and alpha(A) channels.
    /// </summary>
    static property Guid Pbgra32Bpp
    {
        Guid get(void) { return pbgra32Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 8, Bits Per Pixel = 32, Storage Type = uint
    /// Color data is separated into red(R), green(G), blue(B), and alpha(A) channels.
    /// RGB channels premultiplied with alpha.
    /// </summary>
    static property Guid Prgba32Bpp
    {
        Guid get(void) { return prgba32Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 16, Bits Per Pixel = 64, Storage Type = uint
    /// Color data is separated into red(R), green(G), blue(B), and alpha(A) channels.
    /// </summary>
    static property Guid Rgba64Bpp
    {
        Guid get(void) { return rgba64Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 16, Bits Per Pixel = 64, Storage Type = uint
    /// Color data is separated into red(R), green(G), blue(B), and alpha(A) channels.
    /// RGB channels premultiplied with alpha.
    /// </summary>
    static property Guid Prgba64Bpp
    {
        Guid get(void) { return prgba64Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 16, Bits Per Pixel = 64, Storage Type = Fixed
    /// Color data is separated into red(R), green(G), blue(B), and alpha(A) channels.
    /// </summary>
    static property Guid RgbaFixedPoint64Bpp
    {
        Guid get(void) { return rgbaFixedPoint64Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 16, Bits Per Pixel = 64, Storage Type = float
    /// Color data is separated into red(R), green(G), blue(B), and alpha(A) channels.
    /// </summary>
    static property Guid RgbaHalf64Bpp
    {
        Guid get(void) { return rgbaHalf64Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 32, Bits Per Pixel = 128, Storage Type = float
    /// Color data is separated into red(R), green(G), blue(B), and alpha(A) channels.
    /// </summary>
    static property Guid RgbaFloat128Bpp
    {
        Guid get(void) { return rgbaFloat128Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 32, Bits Per Pixel = 128, Storage Type = float
    /// Color data is separated into red(R), green(G), blue(B), and alpha(A) channels.
    /// RGB channels premultiplied with alpha.
    /// </summary>
    static property Guid PrgbaFloat128Bpp
    {
        Guid get(void) { return prgbaFloat128Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 32, Bits Per Pixel = 128, Storage Type = Fixed
    /// Color data is separated into red(R), green(G), blue(B), and alpha(A) channels.
    /// </summary>
    static property Guid RgbaFixedPoint128Bpp
    {
        Guid get(void) { return rgbaFixedPoint128Bpp; }
    }


    /* CMYK Pixel Formats */

    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 8, Bits Per Pixel = 32, Storage Type = Fixed
    /// Color data is separated into cyan(C), magenta(M), yellow(Y), and black(K) channels.
    /// </summary>
    static property Guid Cmyk32Bpp
    {
        Guid get(void) { return cmyk32Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 16, Bits Per Pixel = 64, Storage Type = Fixed
    /// Color data is separated into cyan(C), magenta(M), yellow(Y), and black(K) channels.
    /// </summary>
    static property Guid Cmyk64Bpp
    {
        Guid get(void) { return cmyk64Bpp; }
    }
    /// <summary>
    /// Channel Count = 5, Bits Per Channel = 8, Bits Per Pixel = 40, Storage Type = Fixed
    /// Color data is separated into cyan(C), magenta(M), yellow(Y), black(K), and alpha channels.
    /// </summary>
    static property Guid CmykAlpha40Bpp
    {
        Guid get(void) { return cmykAlpha40Bpp; }
    }
    /// <summary>
    /// Channel Count = 5, Bits Per Channel = 16, Bits Per Pixel = 80, Storage Type = Fixed
    /// Color data is separated into cyan(C), magenta(M), yellow(Y), black(K), and alpha channels.
    /// </summary>
    static property Guid CmykAlpha80Bpp
    {
        Guid get(void) { return cmykAlpha80Bpp; }
    }


    /* n-channel Pixel Formats */

    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 8, Bits Per Pixel = 24, Storage Type = uint
    /// </summary>
    static property Guid ThreeChannel24Bpp
    {
        Guid get(void) { return threeChannel24Bpp; }
    }
    /// <summary>
    /// Channel Count = 3, Bits Per Channel = 16, Bits Per Pixel = 48, Storage Type = uint
    /// </summary>
    static property Guid ThreeChannel48Bpp
    {
        Guid get(void) { return threeChannel48Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 8, Bits Per Pixel = 32, Storage Type = uint
    /// </summary>
    static property Guid ThreeChannelAlpha32Bpp
    {
        Guid get(void) { return threeChannelAlpha32Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 16, Bits Per Pixel = 64, Storage Type = uint
    /// </summary>
    static property Guid ThreeChannelAlpha64Bpp
    {
        Guid get(void) { return threeChannelAlpha64Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 8, Bits Per Pixel = 32, Storage Type = uint
    /// </summary>
    static property Guid FourChannel32Bpp
    {
        Guid get(void) { return fourChannel32Bpp; }
    }
    /// <summary>
    /// Channel Count = 4, Bits Per Channel = 16, Bits Per Pixel = 64, Storage Type = uint
    /// </summary>
    static property Guid FourChannel64Bpp
    {
        Guid get(void) { return fourChannel64Bpp; }
    }
    /// <summary>
    /// Channel Count = 5, Bits Per Channel = 8, Bits Per Pixel = 40, Storage Type = uint
    /// </summary>
    static property Guid FourChannelAlpha40Bpp
    {
        Guid get(void) { return fourChannelAlpha40Bpp; }
    }
    /// <summary>
    /// Channel Count = 5, Bits Per Channel = 16, Bits Per Pixel = 80, Storage Type = uint
    /// </summary>
    static property Guid FourChannelAlpha80Bpp
    {
        Guid get(void) { return fourChannelAlpha80Bpp; }
    }
    /// <summary>
    /// Channel Count = 5, Bits Per Channel = 8, Bits Per Pixel = 40, Storage Type = uint
    /// </summary>
    static property Guid FiveChannel40Bpp
    {
        Guid get(void) { return fiveChannel40Bpp; }
    }
    /// <summary>
    /// Channel Count = 5, Bits Per Channel = 16, Bits Per Pixel = 80, Storage Type = uint
    /// </summary>
    static property Guid FiveChannel80Bpp
    {
        Guid get(void) { return fiveChannel80Bpp; }
    }
    /// <summary>
    /// Channel Count = 6, Bits Per Channel = 8, Bits Per Pixel = 48, Storage Type = uint
    /// </summary>
    static property Guid FiveChannelAlpha48Bpp
    {
        Guid get(void) { return fiveChannelAlpha48Bpp; }
    }
    /// <summary>
    /// Channel Count = 6, Bits Per Channel = 16, Bits Per Pixel = 96, Storage Type = uint
    /// </summary>
    static property Guid FiveChannelAlpha96Bpp
    {
        Guid get(void) { return fiveChannelAlpha96Bpp; }
    }
    /// <summary>
    /// Channel Count = 6, Bits Per Channel = 8, Bits Per Pixel = 48, Storage Type = uint
    /// </summary>
    static property Guid SixChannel48Bpp
    {
        Guid get(void) { return sixChannel48Bpp; }
    }
    /// <summary>
    /// Channel Count = 6, Bits Per Channel = 16, Bits Per Pixel = 96, Storage Type = uint
    /// </summary>
    static property Guid SixChannel96Bpp
    {
        Guid get(void) { return sixChannel96Bpp; }
    }
    /// <summary>
    /// Channel Count = 7, Bits Per Channel = 8, Bits Per Pixel = 56, Storage Type = uint
    /// </summary>
    static property Guid SixChannelAlpha56Bpp
    {
        Guid get(void) { return sixChannelAlpha56Bpp; }
    }
    /// <summary>
    /// Channel Count = 7, Bits Per Channel = 16, Bits Per Pixel = 112, Storage Type = uint
    /// </summary>
    static property Guid SixChannelAlpha112Bpp
    {
        Guid get(void) { return sixChannelAlpha112Bpp; }
    }
    /// <summary>
    /// Channel Count = 7, Bits Per Channel = 8, Bits Per Pixel = 56, Storage Type = uint
    /// </summary>
    static property Guid SevenChannel56Bpp
    {
        Guid get(void) { return sevenChannel56Bpp; }
    }
    /// <summary>
    /// Channel Count = 7, Bits Per Channel = 16, Bits Per Pixel = 112, Storage Type = uint
    /// </summary>
    static property Guid SevenChannel112Bpp
    {
        Guid get(void) { return sevenChannel112Bpp; }
    }
    /// <summary>
    /// Channel Count = 8, Bits Per Channel = 8, Bits Per Pixel = 64, Storage Type = uint
    /// </summary>
    static property Guid SevenChannelAlpha64Bpp
    {
        Guid get(void) { return sevenChannelAlpha64Bpp; }
    }
    /// <summary>
    /// Channel Count = 8, Bits Per Channel = 16, Bits Per Pixel = 128, Storage Type = uint
    /// </summary>
    static property Guid SevenChannelAlpha128Bpp
    {
        Guid get(void) { return sevenChannelAlpha128Bpp; }
    }
    /// <summary>
    /// Channel Count = 8, Bits Per Channel = 8, Bits Per Pixel = 64, Storage Type = uint
    /// </summary>
    static property Guid EightChannel64Bpp
    {
        Guid get(void) { return eightChannel64Bpp; }
    }
    /// <summary>
    /// Channel Count = 8, Bits Per Channel = 16, Bits Per Pixel = 128, Storage Type = uint
    /// </summary>
    static property Guid EightChannel128Bpp
    {
        Guid get(void) { return eightChannel128Bpp; }
    }
    /// <summary>
    /// Channel Count = 9, Bits Per Channel = 8, Bits Per Pixel = 72, Storage Type = uint
    /// </summary>
    static property Guid EightChannelAlpha72Bpp
    {
        Guid get(void) { return eightChannelAlpha72Bpp; }
    }
    /// <summary>
    /// Channel Count = 9, Bits Per Channel = 16, Bits Per Pixel = 144, Storage Type = uint
    /// </summary>
    static property Guid EightChannelAlpha144Bpp
    {
        Guid get(void) { return eightChannelAlpha144Bpp; }
    }


private:

	// Static class
	PixelFormats(void) { }

    static Guid doNotCare = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x00);
    static Guid indexed1Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x01);
    static Guid indexed2Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x02);
    static Guid indexed4Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x03);
    static Guid indexed8Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x04);
    static Guid bgr55516Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x09);
    static Guid bgr56516Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x0a);
    static Guid bgr10101032Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x14);
    static Guid blackWhite = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x05);
    static Guid gray2Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x06);
    static Guid gray4Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x07);
    static Guid gray8Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x08);
    static Guid gray16Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x0b);
    static Guid grayFixedPoint16Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x13);
    static Guid grayHalf16Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x3e);
    static Guid grayFloat32Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x11);
    static Guid grayFixedPoint32Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x3f);
    static Guid bgr24Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x0c);
    static Guid rgb24Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x0d);
    static Guid bgr32Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x0e);
    static Guid rgb48Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x15);
    static Guid rgbFixedPoint48Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x12);
    static Guid rgbHalf48Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x3b);
    static Guid rgbFixedPoint64Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x40);
    static Guid rgbHalf64Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x42);
    static Guid rgbFixedPoint96Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x18);
    static Guid rgbFloat128Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x1b);
    static Guid rgbFixedPoint128Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x41);
    static Guid bgra32Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x0f);
    static Guid rgba32Bpp = Guid(0xf5c7ad2d, 0x6a8d, 0x43dd, 0xa7, 0xa8, 0xa2, 0x99, 0x35, 0x26, 0x1a, 0xe9);
    static Guid rgbe32Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x3d);
    static Guid pbgra32Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x10);
    static Guid prgba32Bpp = Guid(0x3cc4a650, 0xa527u, 0x4d37, 0xa9, 0x16, 0x31, 0x42, 0xc7, 0xeb, 0xed, 0xba);
    static Guid rgba64Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x16);
    static Guid prgba64Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x17);
    static Guid rgbaFixedPoint64Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x1d);
    static Guid rgbaHalf64Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x3a);
    static Guid rgbaFloat128Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x19);
    static Guid prgbaFloat128Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x1a);
    static Guid rgbaFixedPoint128Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x1e);
    static Guid cmyk32Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x1c);
    static Guid cmyk64Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x1f);
    static Guid cmykAlpha40Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x2c);
    static Guid cmykAlpha80Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x2d);
    static Guid threeChannel24Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x20);
    static Guid threeChannel48Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x26);
    static Guid threeChannelAlpha32Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x2e);
    static Guid threeChannelAlpha64Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x34);
    static Guid fourChannel32Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x21);
    static Guid fourChannel64Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x27);
    static Guid fourChannelAlpha40Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x2f);
    static Guid fourChannelAlpha80Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x35);
    static Guid fiveChannel40Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x22);
    static Guid fiveChannel80Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x28);
    static Guid fiveChannelAlpha48Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x30);
    static Guid fiveChannelAlpha96Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x36);
    static Guid sixChannel48Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x23);
    static Guid sixChannel96Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x29);
    static Guid sixChannelAlpha56Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x31);
    static Guid sixChannelAlpha112Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x37);
    static Guid sevenChannel56Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x24);
    static Guid sevenChannel112Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x2a);
    static Guid sevenChannelAlpha64Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x32);
    static Guid sevenChannelAlpha128Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x38);
    static Guid eightChannel64Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x25);
    static Guid eightChannel128Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x2b);
    static Guid eightChannelAlpha72Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x33);
    static Guid eightChannelAlpha144Bpp = Guid(0x6fddc324, 0x4e03, 0x4bfe, 0xb1, 0x85, 0x3d, 0x77, 0x76, 0x8d, 0xc9, 0x39);

};

} } } }