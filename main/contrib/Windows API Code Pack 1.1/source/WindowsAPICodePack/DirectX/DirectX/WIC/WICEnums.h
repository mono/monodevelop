//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace WindowsImagingComponent {

/// <summary>
/// Describes the reading mode for WICImagingFactory.CreateDecoderFromFilename
/// </summary>
[Flags]
public enum class DesiredAccess
{
    /// <summary>
    /// Read data from the file
    /// </summary>
    Read = GENERIC_READ,
    /// <summary>
    /// Write data to the file.
    /// </summary>
    Write = GENERIC_WRITE
};

/// <summary>
/// Specifies image decode options.
/// <para>(Also see WICDecodeOptions Enumerated Type)</para>
/// </summary>
public enum class DecodeMetadataCacheOption
{
    /// <summary>
    /// Cache metadata when needed.
    /// </summary>
    OnDemand = WICDecodeMetadataCacheOnDemand,
    /// <summary>
    /// Cache metadata when decoder is loaded.
    /// </summary>
    OnLoad = WICDecodeMetadataCacheOnLoad
};

/// <summary>
/// Specifies the type of dither algorithm to apply when converting between image formats.
/// <para>(Also see WICBitmapDitherType Enumerated Type)</para>
/// </summary>
public enum class BitmapDitherType
{
    /// <summary>
    /// A solid color algorithm without dither.
    /// </summary>
    None = WICBitmapDitherTypeNone,
    /// <summary>
    /// A solid color algorithm without dither.
    /// </summary>
    Solid = WICBitmapDitherTypeSolid,
    /// <summary>
    /// A 4x4 ordered dither algorithm. 
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId="x")
    Ordered4x4 = WICBitmapDitherTypeOrdered4x4,
    /// <summary>
    /// An 8x8 ordered dither algorithm.
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId="x")
    Ordered8x8 = WICBitmapDitherTypeOrdered8x8,
    /// <summary>
    /// A 16x16 ordered dither algorithm.
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId="x")
    Ordered16x16 = WICBitmapDitherTypeOrdered16x16,
    /// <summary>
    /// A 4x4 spiral dither algorithm.
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId="x")
    Spiral4x4 = WICBitmapDitherTypeSpiral4x4,
    /// <summary>
    /// An 8x8 spiral dither algorithm.
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId="x")
    Spiral8x8 = WICBitmapDitherTypeSpiral8x8,
    /// <summary>
    /// A 4x4 dual spiral dither algorithm.
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId="x")
    DualSpiral4x4 = WICBitmapDitherTypeDualSpiral4x4,
    /// <summary>
    /// An 8x8 dual spiral dither algorithm.
    /// </summary>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId="x")
    DualSpiral8x8 = WICBitmapDitherTypeDualSpiral8x8,
    /// <summary>
    /// An error diffusion algorithm.
    /// </summary>
    ErrorDiffusion = WICBitmapDitherTypeErrorDiffusion
};

/// <summary>
/// Specifies the type of palette used for an indexed image format.
/// <para>(Also see WICBitmapPaletteType Enumerated Type)</para>
/// </summary>
public enum class BitmapPaletteType
{
    /// <summary>
    /// An arbitrary custom palette provided by caller.
    /// </summary>
    Custom = WICBitmapPaletteTypeCustom,
    /// <summary>
    /// An optimal palette generated using a median-cut algorithm. Derived from the colors in an image.
    /// </summary>
    MedianCut = WICBitmapPaletteTypeMedianCut,
    /// <summary>
    /// A black and white palette.
    /// </summary>
    FixedBW = WICBitmapPaletteTypeFixedBW,
    /// <summary>
    /// A palette that has its 8-color on-off primaries and the 16 system colors added. With duplicates removed, 16 colors are available.
    /// </summary>
    FixedHalftone8 = WICBitmapPaletteTypeFixedHalftone8,
    /// <summary>
    /// A palette that has 3 intensity levels of each primary: 27-color on-off primaries and the 16 system colors added. With duplicates removed, 35 colors are available.
    /// </summary>
    FixedHalftone27 = WICBitmapPaletteTypeFixedHalftone27,
    /// <summary>
    /// A palette that has 4 intensity levels of each primary: 64-color on-off primaries and the 16 system colors added. With duplicates removed, 72 colors are available.
    /// </summary>
    FixedHalftone64 = WICBitmapPaletteTypeFixedHalftone64,
    /// <summary>
    /// A palette that has 5 intensity levels of each primary: 125-color on-off primaries and the 16 system colors added. With duplicates removed, 133 colors are available.
    /// </summary>
    FixedHalftone125 = WICBitmapPaletteTypeFixedHalftone125,
    /// <summary>
    /// A palette that has 6 intensity levels of each primary: 216-color on-off primaries and the 16 system colors added. With duplicates removed, 224 colors are available. This is the same as FixedWebPalette.
    /// </summary>
    FixedHalftone216 = WICBitmapPaletteTypeFixedHalftone216,
    /// <summary>
    /// A palette that has 6 intensity levels of each primary: 216-color on-off primaries and the 16 system colors added. With duplicates removed, 224 colors are available. This is the same as FixedHalftone216.
    /// </summary>
    FixedWebPalette = WICBitmapPaletteTypeFixedWebPalette,
    /// <summary>
    /// A palette that has its 252-color on-off primaries and the 16 system colors added. With duplicates removed, 256 colors are available.
    /// </summary>
    FixedHalftone252 = WICBitmapPaletteTypeFixedHalftone252,
    /// <summary>
    /// A palette that has its 256-color on-off primaries and the 16 system colors added. With duplicates removed, 256 colors are available.
    /// </summary>
    FixedHalftone256 = WICBitmapPaletteTypeFixedHalftone256,
    /// <summary>
    /// A palette that has 4 shades of gray.
    /// </summary>
    FixedGray4 = WICBitmapPaletteTypeFixedGray4,
    /// <summary>
    /// A palette that has 16 shades of gray.
    /// </summary>
    FixedGray16 = WICBitmapPaletteTypeFixedGray16,
    /// <summary>
    /// A palette that has 256 shades of gray.
    /// </summary>
    FixedGray256 = WICBitmapPaletteTypeFixedGray256
};

/// <summary>
/// Specifies access to a ImagingBitmap.
/// </summary>
[Flags]
public enum class BitmapLockOptions
{
    /// <summary>
    /// A read access lock.
    /// </summary>
    Read = WICBitmapLockRead,

    /// <summary>
    /// A write access lock.
    /// </summary>
    Write = WICBitmapLockWrite
};

/// <summary>
/// Specifies the desired cache usage for a ImagingBitmap.
/// </summary>
public enum class BitmapCreateCacheOption
{	
    /// <summary>
    /// Do not cache the bitmap.
    /// </summary>
    NoCache = WICBitmapNoCache,
    /// <summary>
    /// Cache the bitmap when needed.
    /// </summary>
    CacheOnDemand = WICBitmapCacheOnDemand,
    /// <summary>
    /// Cache the bitmap at initialization.
    /// </summary>
    CacheOnLoad = WICBitmapCacheOnLoad,    
};
} } } }