// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "DirectWrite/DWriteStructs.h"
#include "D2DPointAndTangent.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace WindowsImagingComponent {
    ref class ImagingBitmap;
    ref class ImagingBitmapLock;
    ref class BitmapSource;
}}}}

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {
    ref class Surface;
}}}}

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {
    ref class TextFormat;
    ref class TextLayout;
    ref class RenderingParams;
}}}}

using namespace System::Collections::ObjectModel;
using namespace Microsoft::WindowsAPICodePack::DirectX;
using namespace Microsoft::WindowsAPICodePack::DirectX::WindowsImagingComponent;
using namespace Microsoft::WindowsAPICodePack::DirectX::DirectWrite;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct2D1 {

    ref class GdiInteropRenderTarget; // Required forward declaration

/// <summary>
/// The base class for all resources in Direct2D 1.0
/// <para>(Also see DirectX SDK: ID2D1Resource)</para>
/// </summary>
public ref class D2DResource abstract : DirectUnknown 
{
public:

    /// <summary>
    /// Retrieve the factory associated with this resource.
    /// <para>(Also see DirectX SDK: ID2D1Resource::GetFactory)</para>
    /// </summary>
    property D2DFactory^ Factory
    {
        D2DFactory^ get(void);
    }

internal:

    D2DResource();

    D2DResource(
        IUnknown *pInner
        );

};

/// <summary>
/// Represents a bitmap that has been bound to a <see cref="RenderTarget"/>.
/// <para>(Also see DirectX SDK: ID2D1Bitmap)</para>
/// </summary>
public ref class D2DBitmap : D2DResource 
{
public:

    /// <summary>
    /// Returns the size of the bitmap in resolution independent units.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::GetSize)</para>
    /// </summary>
    property SizeF Size
    {
        SizeF get();
    }

    /// <summary>
    /// Returns the size of the bitmap in resolution dependent units (pixels).
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::GetPixelSize)</para>
    /// </summary>
    property SizeU PixelSize
    {
        SizeU get();
    }


    /// <summary>
    /// Retrieve the format of the bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::GetPixelFormat)</para>
    /// </summary>
    property Direct2D1::PixelFormat PixelFormat
    {
        Direct2D1::PixelFormat get();
    }

    /// <summary>
    /// Return the DPI of the bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::GetDpi)</para>
    /// </summary>
    property DpiF Dpi
    {
        DpiF get();
    }

    /// <summary>Copies the specified region from the specified render target into the current bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::CopyFromBitmap)</para>
    /// </summary>
    /// <param name="destinationPoint">The destinationPoint parameter.</param>
    /// <param name="bitmap">The bitmap parameter.</param>
    /// <param name="sourceRect">The sourceRect parameter.</param>
    void
    CopyFromBitmap(
        D2DBitmap ^ bitmap,
        Point2U destinationPoint,
        RectU sourceRect
        );

    /// <summary>Copies the specified region from the specified render target into the current bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::CopyFromBitmap)</para>
    /// </summary>
    /// <param name="destinationPoint">The destinationPoint parameter.</param>
    /// <param name="bitmap">The bitmap parameter.</param>
    void
    CopyFromBitmap(
        D2DBitmap ^ bitmap,
        Point2U destinationPoint
        );

    /// <summary>Copies the specified region from the specified render target into the current bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::CopyFromBitmap)</para>
    /// </summary>
    /// <param name="bitmap">The bitmap parameter.</param>
    /// <param name="sourceRect">The sourceRect parameter.</param>
    void
    CopyFromBitmap(
        D2DBitmap ^ bitmap,
        RectU sourceRect
        );

    /// <summary>Copies the specified region from the specified render target into the current bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::CopyFromBitmap)</para>
    /// </summary>
    /// <param name="bitmap">The bitmap parameter.</param>
    void
    CopyFromBitmap(
        D2DBitmap ^ bitmap
        );

    /// <summary>Copies the specified region from the specified render target into the current bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::CopyFromRenderTarget)</para>
    /// </summary>
    /// <param name="destinationPoint">The destinationPoint parameter.</param>
    /// <param name="renderTarget">The renderTarget parameter.</param>
    /// <param name="sourceRect">The sourceRect parameter.</param>
    void
    CopyFromRenderTarget(
        RenderTarget ^ renderTarget,
        Point2U destinationPoint,
        RectU sourceRect
        );

    /// <summary>Copies the specified region from the specified render target into the current bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::CopyFromRenderTarget)</para>
    /// </summary>
    /// <param name="destinationPoint">The destinationPoint parameter.</param>
    /// <param name="renderTarget">The renderTarget parameter.</param>
    void
    CopyFromRenderTarget(
        RenderTarget ^ renderTarget,
        Point2U destinationPoint
        );

    /// <summary>Copies the specified region from the specified render target into the current bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::CopyFromRenderTarget)</para>
    /// </summary>
    /// <param name="renderTarget">The renderTarget parameter.</param>
    /// <param name="sourceRect">The sourceRect parameter.</param>
    void
    CopyFromRenderTarget(
        RenderTarget ^ renderTarget,
        RectU sourceRect
        );

    /// <summary>Copies the specified region from the specified render target into the current bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::CopyFromRenderTarget)</para>
    /// </summary>
    /// <param name="renderTarget">The renderTarget parameter.</param>
    void
    CopyFromRenderTarget(
        RenderTarget ^ renderTarget
        );


    /// <summary>Copies the specified region from memory into the current bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::CopyFromMemory)</para>
    /// </summary>
    /// <param name="destinationRect">In the current bitmap, the upper-left corner of the area to which the region specified is copied.</param>
    /// <param name="sourceData">The data to copy.</param>
    /// <param name="pitch">The stride, or pitch, of the source bitmap stored in sourceData. The stride is the byte count of a scanline (one row of pixels in memory). The stride can be computed from the following formula: pixel width * bytes per pixel + memory padding.</param>
    void
    CopyFromMemory(
        RectU destinationRect,
        IntPtr sourceData,
        UINT32 pitch
        );

    /// <summary>Copies the specified region from memory into the current bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::CopyFromMemory)</para>
    /// </summary>
    /// <param name="sourceData">The data to copy.</param>
    /// <param name="pitch">The stride, or pitch, of the source bitmap stored in sourceData. The stride is the byte count of a scanline (one row of pixels in memory). The stride can be computed from the following formula: pixel width * bytes per pixel + memory padding.</param>
    void
    CopyFromMemory(
        IntPtr sourceData,
        UINT32 pitch
        );

    /// <summary>Copies the specified region from memory into the current bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::CopyFromMemory)</para>
    /// </summary>
    /// <param name="destinationRect">In the current bitmap, the upper-left corner of the area to which the region specified is copied.</param>
    /// <param name="byteArray">The data to copy.</param>
    /// <param name="pitch">The stride, or pitch, of the source bitmap stored in sourceData. The stride is the byte count of a scanline (one row of pixels in memory). The stride can be computed from the following formula: pixel width * bytes per pixel + memory padding.</param>
    void
    CopyFromMemory(
        RectU destinationRect,
        array<unsigned char>^ byteArray,
        UINT32 pitch
        );

    /// <summary>Copies the specified region from memory into the current bitmap.
    /// <para>(Also see DirectX SDK: ID2D1Bitmap::CopyFromMemory)</para>
    /// </summary>
    /// <param name="byteArray">The data to copy.</param>
    /// <param name="pitch">The stride, or pitch, of the source bitmap stored in sourceData. The stride is the byte count of a scanline (one row of pixels in memory). The stride can be computed from the following formula: pixel width * bytes per pixel + memory padding.</param>
    void
    CopyFromMemory(
        array<unsigned char>^ byteArray,
        UINT32 pitch
        );

internal:

    D2DBitmap();

    D2DBitmap(
        IUnknown *pInner
        );

};

/// <summary>
/// Represents a collection of <see cref="GradientStop"/> for either a linear or radial gradient brush.
/// <para>(Also see DirectX SDK: ID2D1GradientStopCollection)</para>
/// </summary>
public ref class GradientStopCollection : D2DResource, IList<GradientStop>
{
public:

    virtual void Add(GradientStop) = System::Collections::Generic::IList<GradientStop>::Add { throw gcnew NotSupportedException("GradientStopCollection is read-only"); }
	virtual void Clear(void) = System::Collections::Generic::IList<GradientStop>::Clear { throw gcnew NotSupportedException("GradientStopCollection is read-only"); }
	virtual void Insert(int, GradientStop) = System::Collections::Generic::IList<GradientStop>::Insert { throw gcnew NotSupportedException("GradientStopCollection is read-only"); }
	virtual bool Remove(GradientStop) = System::Collections::Generic::IList<GradientStop>::Remove { throw gcnew NotSupportedException("GradientStopCollection is read-only"); }
	virtual void RemoveAt(int) = System::Collections::Generic::IList<GradientStop>::RemoveAt { throw gcnew NotSupportedException("GradientStopCollection is read-only"); }

	virtual bool Contains(GradientStop item);
	virtual void CopyTo(array<GradientStop>^ destinationArray, int arrayIndex);
	virtual IEnumerator<GradientStop>^ GetEnumerator(void);
	virtual System::Collections::IEnumerator^ GetNonGenericEnumerator(void) = System::Collections::IEnumerable::GetEnumerator;
	virtual int IndexOf(GradientStop item);

    /// <summary>
    /// Returns the number of stops in the gradient.
    /// <para>(Also see DirectX SDK: ID2D1GradientStopCollection::GetGradientStopCount)</para>
    /// </summary>
	property int Count { virtual int get(void); }

	property bool IsReadOnly { virtual bool get(void) { return true; } }
	property GradientStop default[int]
	{
		virtual GradientStop get(int index);
		virtual void set(int, GradientStop)
		{
			throw gcnew NotSupportedException("GradientStopCollection is read-only");
		}
	}


    /// <summary>
    /// Returns whether the interpolation occurs with 1.0 or 2.2 gamma.
    /// <para>(Also see DirectX SDK: ID2D1GradientStopCollection::GetColorInterpolationGamma)</para>
    /// </summary>
    property Gamma ColorInterpolationGamma
    {
        Gamma get();
    }

    /// <summary>
    /// Indicates the behavior of the gradient outside the normalized gradient range. 
    /// <para>(Also see DirectX SDK: ID2D1GradientStopCollection::GetExtendMode)</para>
    /// </summary>
    property Direct2D1::ExtendMode ExtendMode
    {
        Direct2D1::ExtendMode get();
    }

private:

	/// <summary>
    /// Initializes the gradient stop collection cache
    /// </summary>
    void EnsureGradientStops(void);

	IList<GradientStop>^ gradientStops;

internal:

	GradientStopCollection() { }

	GradientStopCollection(ID2D1GradientStopCollection *pInner) : D2DResource(pInner) { }

};

/// <summary>
/// The root brush interface. All brushes can be used to fill or pen a geometry.
/// <para>(Also see DirectX SDK: ID2D1Brush)</para>
/// </summary>
public ref class Brush : D2DResource 
{
public:

    /// <summary>
    /// The opacity for when the brush is drawn over the entire fill of the brush.
    /// <para>(Also see DirectX SDK: ID2D1Brush::SetOpacity, ID2D1Brush::GetOpacity)</para>
    /// </summary>
    property FLOAT Opacity
    {
        FLOAT get();
        void set(FLOAT);
    }

    /// <summary>
    /// The transform that applies to everything drawn by the brush.
    /// <para>(Also see DirectX SDK: ID2D1Brush::SetTransform)</para>
    /// </summary>
    /// <param name="transform">The transform parameter.</param>
    property Matrix3x2F Transform
    {
        Matrix3x2F get();
        void set(Matrix3x2F);
    }

internal:

    Brush();

    Brush(
        IUnknown *pInner
        );
};

/// <summary>
/// A bitmap brush allows a bitmap to be used to fill a geometry.
/// <para>(Also see DirectX SDK: ID2D1BitmapBrush)</para>
/// </summary>
public ref class BitmapBrush : Brush 
{
public:

    /// <summary>
    /// Gets or sets how the bitmap is to be treated outside of its natural extent on the X axis.
    /// <para>(Also see DirectX SDK: ID2D1BitmapBrush::GetExtendModeX, ID2D1BitmapBrush::SetExtendModeX)</para>
    /// </summary>
    property Direct2D1::ExtendMode ExtendModeX
    {
        Direct2D1::ExtendMode get();
        void set(Direct2D1::ExtendMode);
    }

    /// <summary>
    /// Gets or sets how the bitmap is to be treated outside of its natural extent on the Y axis.
    /// <para>(Also see DirectX SDK: ID2D1BitmapBrush::GetExtendModeY, ID2D1BitmapBrush::SetExtendModeY)</para>
    /// </summary>
    property Direct2D1::ExtendMode ExtendModeY
    {
        Direct2D1::ExtendMode get();
        void set(Direct2D1::ExtendMode);
    }

    /// <summary>
    /// Sets the interpolation mode used when this brush is used.
    /// <para>(Also see DirectX SDK: ID2D1BitmapBrush::GetInterpolationMode, ID2D1BitmapBrush::SetInterpolationMode)</para>
    /// </summary>
    /// <param name="interpolationMode">The interpolationMode parameter.</param>
    property BitmapInterpolationMode InterpolationMode
    {
        BitmapInterpolationMode get();
        void set(BitmapInterpolationMode);
    }
        
    /// <summary>
    /// Gets or sets the bitmap associated with this brush.
    /// <para>(Also see DirectX SDK: ID2D1BitmapBrush::GetBitmap)</para>
    /// </summary>
    property D2DBitmap^ Bitmap
    {
        D2DBitmap^ get(void);
        void set(D2DBitmap ^ bitmap);
    }

internal:

    BitmapBrush();

    BitmapBrush(
        IUnknown *pInner
        );

};

/// <summary>
/// Paints an area with a solid color. 
/// <para>(Also see DirectX SDK: ID2D1SolidColorBrush)</para>
/// </summary>
public ref class SolidColorBrush : Brush 
{
public:

    /// <summary>
    /// The color of the solid color brush.
    /// <para>(Also see DirectX SDK: ID2D1SolidColorBrush::GetColor, ID2D1SolidColorBrush::SetColor)</para>
    /// </summary>
    /// <param name="color">The color parameter.</param>
    property ColorF Color
    {
        ColorF get();
        void set(ColorF);
    }

internal:

    SolidColorBrush();

    SolidColorBrush(
        IUnknown *pInner
        );

};

/// <summary>
/// Paints an area with a linear gradient.
/// <para>(Also see DirectX SDK: ID2D1LinearGradientBrush)</para>
/// </summary>
public ref class LinearGradientBrush : Brush 
{
public:

    /// <summary>
    /// The start point of the gradient in local coordinate space. 
    /// <para>(Also see DirectX SDK: ID2D1LinearGradientBrush::GetStartPoint, ID2D1LinearGradientBrush::SetStartPoint)</para>
    /// </summary>
    property Point2F StartPoint
    {
        Point2F get();
        void set(Point2F);
    }

    /// <summary>
    /// The end point of the gradient in local coordinate space. This is not influenced
    /// by the geometry being filled.
    /// <para>(Also see DirectX SDK: ID2D1LinearGradientBrush::GetEndPoint, ID2D1LinearGradientBrush::SetEndPoint)</para>
    /// </summary>
    property Point2F EndPoint
    {
        Point2F get();
        void set(Point2F);
    }


    /// <summary>
    /// Retrieves the <see cref="GradientStopCollection"/> associated with this linear gradient brush.
    /// <para>(Also see DirectX SDK: ID2D1LinearGradientBrush::GetGradientStopCollection)</para>
    /// </summary>
    /// <returns>GradientStopCollection</returns>
    property GradientStopCollection^ GradientStops
    {
        GradientStopCollection^ get(void);
    }

internal:

    LinearGradientBrush();

    LinearGradientBrush(
        IUnknown *pInner
        );

};

/// <summary>
/// Paints an area with a radial gradient.
/// <para>(Also see DirectX SDK: ID2D1RadialGradientBrush)</para>
/// </summary>
public ref class RadialGradientBrush : Brush 
{
public:

    /// <summary>
    /// The center of the radial gradient. This will be in local coordinates and will
    /// not depend on the geometry being filled.
    /// <para>(Also see DirectX SDK: ID2D1RadialGradientBrush::GetCenter, ID2D1RadialGradientBrush::SetCenter)</para>
    /// </summary>
    property Point2F Center
    {
        Point2F get();
        void set(Point2F);
    }


    /// <summary>
    /// the offset of the origin relative to the radial gradient center.
    /// <para>(Also see DirectX SDK: ID2D1RadialGradientBrush::SetGradientOriginOffset, ID2D1RadialGradientBrush::GetGradientOriginOffset)</para>
    /// </summary>
    property Point2F GradientOriginOffset
    {
        Point2F get();
        void set(Point2F);
    }
    /// <summary>
    /// The x-radius of the gradient ellipse. 
    /// <para>(Also see DirectX SDK: ID2D1RadialGradientBrush::GetRadiusX, ID2D1RadialGradientBrush::SetRadiusX)</para>
    /// </summary>
    property FLOAT RadiusX
    {
        FLOAT get();
        void set(FLOAT);
    }

    /// <summary>
    /// The y-radius of the gradient ellipse. 
    /// <para>(Also see DirectX SDK: ID2D1RadialGradientBrush::GetRadiusY, ID2D1RadialGradientBrush::SetRadiusY)</para>
    /// </summary>
    property FLOAT RadiusY
    {
        FLOAT get();
        void set(FLOAT);
    }

    /// <summary>
    /// Retrieves the <see cref="GradientStopCollection"/> associated with this radial gradient brush.
    /// <para>(Also see DirectX SDK: ID2D1RadialGradientBrush::GetGradientStopCollection)</para>
    /// </summary>
    /// <returns>GradientStopCollection</returns>
    property GradientStopCollection^ GradientStops
    {
        GradientStopCollection^ get(void);
    }

internal:

    RadialGradientBrush();

    RadialGradientBrush(
        IUnknown *pInner
        );

};

/// <summary>
/// Represents the drawing state of a render target: the antialiasing mode, transform, tags, and text-rendering options.
/// <para>(Also see DirectX SDK: ID2D1DrawingStateBlock)</para>
/// </summary>
public ref class DrawingStateBlock : D2DResource 
{
public:

    /// <summary>
    /// The state currently contained within this state block resource.
    /// <para>(Also see DirectX SDK: ID2D1DrawingStateBlock::GetDescription, ID2D1DrawingStateBlock::SetDescription)</para>
    /// </summary>
    /// <param name="stateDescription">The stateDescription parameter.</param>
    property DrawingStateDescription Description
    {
        DrawingStateDescription get();
        void set (DrawingStateDescription);
    }

    /// <summary>
    /// Gets or set the text-rendering configuration of the drawing state.
    /// <para>(Also see DirectX SDK: ID2D1DrawingStateBlock::GetTextRenderingParams, ID2D1DrawingStateBlock::SetTextRenderingParams)</para>
    /// </summary>
    property RenderingParams^ TextRenderingParams
    {
        RenderingParams^ get(void);
        void set(RenderingParams^ textRenderingParams);
    }

internal:
    DrawingStateBlock();

    DrawingStateBlock(
        IUnknown *pInner
        );

};

/// <summary>
/// Represents an object that can receive drawing commands. 
/// <B>Interfaces that inherit from RenderTarget render the drawing commands they receive in different ways.</B> 
/// <para>(Also see DirectX SDK: ID2D1RenderTarget)</para>
/// </summary>
public ref class RenderTarget : D2DResource 
{
public:

    /// <summary>
    /// Create an uninitialized bitmap.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateBitmap)</para>
    /// </summary>
    /// <param name="size">The dimension of the bitmap to create in pixels.</param>
    /// <param name="bitmapProperties">The pixel format and dots per inch (DPI) of the bitmap to create.</param>
    /// <returns>D2DBitmap</returns>
    D2DBitmap ^
    CreateBitmap(
        SizeU size,
        BitmapProperties bitmapProperties
        );

    /// <summary>
    /// Create a bitmap by copying from memory
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateBitmap)</para>
    /// </summary>
    /// <param name="size">The dimension of the bitmap to create in pixels.</param>
    /// <param name="sourceData">A pointer to the memory location of the image data.</param>
    /// <param name="pitch">The byte count of each scanline, which is equal to (the image width in pixels * the number of bytes per pixel) + memory padding. (Note that pitch is also sometimes called stride.)</param>
    /// <param name="bitmapProperties">The pixel format and dots per inch (DPI) of the bitmap to create.</param>
    /// <returns>D2DBitmap</returns>
    D2DBitmap ^
    CreateBitmap(
        SizeU size,
        IntPtr sourceData,
        UINT32 pitch,
        BitmapProperties bitmapProperties
        );

    /// <summary>
    /// Create a bitmap by copying from memory
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateBitmap)</para>
    /// </summary>
    /// <param name="size">The dimension of the bitmap to create in pixels.</param>
    /// <param name="source">An array of bytes holding the image data.</param>
    /// <param name="pitch">The byte count of each scanline, which is equal to (the image width in pixels * the number of bytes per pixel) + memory padding. (Note that pitch is also sometimes called stride.)</param>
    /// <param name="bitmapProperties">The pixel format and dots per inch (DPI) of the bitmap to create.</param>
    /// <returns>D2DBitmap</returns>
    D2DBitmap ^
    CreateBitmap(
        SizeU size,
        array<unsigned char>^ source,
        UINT32 pitch,
        BitmapProperties bitmapProperties
        );

    /// <summary>
    /// Create a D2D bitmap by copying a WIC bitmap.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateBitmapFromWicBitmap)</para>
    /// </summary>
    /// <param name="wicBitmapSource">The wicBitmapSource parameter.</param>
    /// <param name="bitmapProperties">The bitmapProperties parameter.</param>
    /// <returns>D2DBitmap</returns>
    D2DBitmap ^
    CreateBitmapFromWicBitmap(
        BitmapSource^ wicBitmapSource,
        BitmapProperties bitmapProperties
        );

    /// <summary>
    /// Create a D2D bitmap by copying a WIC bitmap.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateBitmapFromWicBitmap)</para>
    /// </summary>
    /// <param name="wicBitmapSource">The wicBitmapSource parameter.</param>
    /// <returns>D2DBitmap</returns>
    D2DBitmap ^
    CreateBitmapFromWicBitmap(
        BitmapSource^ wicBitmapSource
        );

    /// <summary>
    /// Create a D2D bitmap by sharing bits from another resource. The bitmap must be compatible
    /// with the render target for the call to succeed. For example, a ImagingBitmapLock can be
    /// shared with a software target, or a Graphics surface can be shared with a Graphics render
    /// target.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateSharedBitmap)</para>
    /// </summary>
    /// <param name="wicBitmap">The wicBitmap.</param>
    /// <param name="bitmapProperties">The bitmap properties.</param>
    /// <returns>D2DBitmap</returns>
    D2DBitmap ^
    CreateSharedBitmap(
        ImagingBitmapLock^ wicBitmap,
        BitmapProperties bitmapProperties
        );

    /// <summary>
    /// Create a D2D bitmap by sharing bits from another resource. The bitmap must be compatible
    /// with the render target for the call to succeed. For example, a ImagingBitmapLock can be
    /// shared with a software target, or a Graphics surface can be shared with a Graphics render
    /// target.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateSharedBitmap)</para>
    /// </summary>
    /// <param name="wicBitmap">The wicBitmap.</param>
    /// <returns>D2DBitmap</returns>
    D2DBitmap ^
    CreateSharedBitmap(
        ImagingBitmapLock^ wicBitmap
        );

    /// <summary>
    /// Create a D2D bitmap by sharing bits from another resource. The bitmap must be compatible
    /// with the render target for the call to succeed. For example, a ImagingBitmapLock can be
    /// shared with a software target, or a Graphics surface can be shared with a Graphics render
    /// target.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateSharedBitmap)</para>
    /// </summary>
    /// <param name="surface">The Graphics Surface.</param>
    /// <param name="bitmapProperties">The bitmap properties.</param>
    /// <returns>D2DBitmap</returns>
    D2DBitmap ^
    CreateSharedBitmap(
        Surface^ surface,
        BitmapProperties bitmapProperties
        );

    /// <summary>
    /// Create a D2D bitmap by sharing bits from another resource. The bitmap must be compatible
    /// with the render target for the call to succeed. For example, a ImagingBitmapLock can be
    /// shared with a software target, or a Graphics surface can be shared with a Graphics render
    /// target.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateSharedBitmap)</para>
    /// </summary>
    /// <param name="surface">The Graphics Surface.</param>
    /// <returns>D2DBitmap</returns>
    D2DBitmap ^
    CreateSharedBitmap(
        Surface^ surface
        );

    /// <summary>
    /// Create a D2D bitmap by sharing bits from another resource. The bitmap must be compatible
    /// with the render target for the call to succeed. For example, a ImagingBitmapLock can be
    /// shared with a software target, or a Graphics surface can be shared with a Graphics render
    /// target.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateSharedBitmap)</para>
    /// </summary>
    /// <param name="bitmap">The Bitmap.</param>
    /// <param name="bitmapProperties">The bitmap properties.</param>
    /// <returns>D2DBitmap</returns>
    D2DBitmap ^
    CreateSharedBitmap(
        D2DBitmap ^ bitmap,
        BitmapProperties bitmapProperties
        );

    /// <summary>
    /// Create a D2D bitmap by sharing bits from another resource. The bitmap must be compatible
    /// with the render target for the call to succeed. For example, a ImagingBitmapLock can be
    /// shared with a software target, or a Graphics surface can be shared with a Graphics render
    /// target.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateSharedBitmap)</para>
    /// </summary>
    /// <param name="bitmap">The Bitmap.</param>
    /// <returns>D2DBitmap</returns>
    D2DBitmap ^
    CreateSharedBitmap(
        D2DBitmap ^ bitmap
        );

    /// <summary>
    /// Creates a bitmap brush. The bitmap is scaled, rotated, skewed or tiled to fill or
    /// pen a geometry.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateBitmapBrush)</para>
    /// </summary>
    /// <param name="bitmap">The bitmap parameter.</param>
    /// <param name="bitmapBrushProperties">The bitmapBrushProperties parameter.</param>
    /// <param name="brushProperties">The brushProperties parameter.</param>
    /// <returns>BitmapBrush</returns>
    BitmapBrush ^
    CreateBitmapBrush(
        D2DBitmap ^ bitmap,
        BitmapBrushProperties bitmapBrushProperties,
        BrushProperties brushProperties
        );

    /// <summary>
    /// Creates a bitmap brush. The bitmap is scaled, rotated, skewed or tiled to fill or
    /// pen a geometry.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateBitmapBrush)</para>
    /// </summary>
    /// <param name="bitmap">The bitmap parameter.</param>
    /// <param name="bitmapBrushProperties">The bitmapBrushProperties parameter.</param>
    /// <returns>BitmapBrush</returns>
    BitmapBrush ^
    CreateBitmapBrush(
        D2DBitmap ^ bitmap,
        BitmapBrushProperties bitmapBrushProperties
        );

    /// <summary>
    /// Creates a bitmap brush. The bitmap is scaled, rotated, skewed or tiled to fill or
    /// pen a geometry.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateBitmapBrush)</para>
    /// </summary>
    /// <param name="bitmap">The bitmap parameter.</param>
    /// <param name="brushProperties">The brushProperties parameter.</param>
    /// <returns>BitmapBrush</returns>
    BitmapBrush ^
    CreateBitmapBrush(
        D2DBitmap ^ bitmap,
        BrushProperties brushProperties
        );

    /// <summary>
    /// Creates a bitmap brush. The bitmap is scaled, rotated, skewed or tiled to fill or
    /// pen a geometry.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateBitmapBrush)</para>
    /// </summary>
    /// <param name="bitmap">The bitmap parameter.</param>
    /// <returns>BitmapBrush</returns>
    BitmapBrush ^
    CreateBitmapBrush(
        D2DBitmap ^ bitmap
        );

    /// <summary>
    /// Creates a new <see cref="SolidColorBrush"/> that can be used to paint areas with a solid color.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateSolidColorBrush)</para>
    /// </summary>
    /// <param name="color">The color parameter.</param>
    /// <param name="brushProperties">The brushProperties parameter.</param>
    /// <returns>SolidColorBrush</returns>
    SolidColorBrush ^
    CreateSolidColorBrush(
        ColorF color,
        BrushProperties brushProperties
        );

    /// <summary>
    /// Creates a new <see cref="SolidColorBrush"/> that can be used to paint areas with a solid color.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateSolidColorBrush)</para>
    /// </summary>
    /// <param name="color">The color parameter.</param>
    /// <returns>SolidColorBrush</returns>
    SolidColorBrush ^
    CreateSolidColorBrush(
        ColorF color
        );
    /// <summary>
    /// A gradient stop collection represents a set of stops in an ideal unit length. This
    /// is the source resource for a linear gradient and radial gradient brush.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateGradientStopCollection)</para>
    /// </summary>
    /// <param name="gradientStops">The gradientStops parameter.</param>
    /// <param name="colorInterpolationGamma">Specifies which space the color interpolation occurs in.</param>
    /// <param name="extendMode">Specifies how the gradient will be extended outside of the unit length.</param>
    /// <returns>GradientStopCollection</returns>
    GradientStopCollection ^
    CreateGradientStopCollection(
        IEnumerable<GradientStop> ^ gradientStops,
        Gamma colorInterpolationGamma,
        ExtendMode extendMode
        );

    /// <summary>
    /// Creates a new <see cref="LinearGradientBrush"/> that can be used to paint areas with a linear gradient.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateLinearGradientBrush)</para>
    /// </summary>
    /// <param name="linearGradientBrushProperties">The linearGradientBrushProperties parameter.</param>
    /// <param name="brushProperties">The brushProperties parameter.</param>
    /// <param name="gradientStopCollection">The gradientStopCollection parameter.</param>
    /// <returns>LinearGradientBrush</returns>
    LinearGradientBrush ^
    CreateLinearGradientBrush(
        LinearGradientBrushProperties linearGradientBrushProperties,
        GradientStopCollection ^ gradientStopCollection,
        BrushProperties brushProperties
        );

    /// <summary>
    /// Creates a new <see cref="LinearGradientBrush"/> that can be used to paint areas with a linear gradient.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateLinearGradientBrush)</para>
    /// </summary>
    /// <param name="linearGradientBrushProperties">The linearGradientBrushProperties parameter.</param>
    /// <param name="gradientStopCollection">The gradientStopCollection parameter.</param>
    /// <returns>LinearGradientBrush</returns>
    LinearGradientBrush ^
    CreateLinearGradientBrush(
        LinearGradientBrushProperties linearGradientBrushProperties,
        GradientStopCollection ^ gradientStopCollection
        );

    /// <summary>
    /// Creates a new <see cref="RadialGradientBrush"/> that can be used to paint areas with a radial gradient.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateRadialGradientBrush)</para>
    /// </summary>
    /// <param name="radialGradientBrushProperties">The radialGradientBrushProperties parameter.</param>
    /// <param name="brushProperties">The brushProperties parameter.</param>
    /// <param name="gradientStopCollection">The gradientStopCollection parameter.</param>
    /// <returns>RadialGradientBrush</returns>
    RadialGradientBrush ^
    CreateRadialGradientBrush(
        RadialGradientBrushProperties radialGradientBrushProperties,
        GradientStopCollection ^ gradientStopCollection,
        BrushProperties brushProperties
        );

    /// <summary>
    /// Creates a new <see cref="RadialGradientBrush"/> that can be used to paint areas with a radial gradient.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateRadialGradientBrush)</para>
    /// </summary>
    /// <param name="radialGradientBrushProperties">The radialGradientBrushProperties parameter.</param>
    /// <param name="gradientStopCollection">The gradientStopCollection parameter.</param>
    /// <returns>RadialGradientBrush</returns>
    RadialGradientBrush ^
    CreateRadialGradientBrush(
        RadialGradientBrushProperties radialGradientBrushProperties,
        GradientStopCollection ^ gradientStopCollection
        );

    /// <summary>
    /// Creates a bitmap render target whose bitmap can be used as a source for rendering
    /// in the API.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateCompatibleRenderTarget)</para>
    /// </summary>
    /// <param name="desiredSize">The requested size of the target in DIPs. If the pixel size is not specified, the DPI is inherited from the parent target. However, the render target will never contain a fractional number of pixels.</param>
    /// <param name="desiredPixelSize">The requested size of the render target in pixels. If the DIP size is also specified, the DPI is calculated from these two values. If the desired size is not specified, the DPI is inherited from the parent render target. If neither value is specified, the compatible render target will be the same size and have the same DPI as the parent target.</param>
    /// <param name="options">Allows the caller to retrieve a GDI compatible render target.</param>
    /// <returns>A bitmap render target.</returns>
    BitmapRenderTarget ^
    CreateCompatibleRenderTarget(
        CompatibleRenderTargetOptions options,
        SizeF desiredSize,
        SizeU desiredPixelSize
        );

    /// <summary>
    /// Creates a bitmap render target whose bitmap can be used as a source for rendering
    /// in the API.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateCompatibleRenderTarget)</para>
    /// </summary>
    /// <returns>A bitmap render target.</returns>
    BitmapRenderTarget ^
    CreateCompatibleRenderTarget();
    /// <summary>
    /// Creates a bitmap render target whose bitmap can be used as a source for rendering
    /// in the API.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateCompatibleRenderTarget)</para>
    /// </summary>
    /// <param name="desiredSize">The requested size of the target in DIPs. If the pixel size is not specified, the DPI is inherited from the parent target. However, the render target will never contain a fractional number of pixels.</param>
    /// <param name="options">Allows the caller to retrieve a GDI compatible render target.</param>
    /// <returns>A bitmap render target.</returns>
    BitmapRenderTarget ^
    CreateCompatibleRenderTarget(
        CompatibleRenderTargetOptions options,
        SizeF desiredSize
        );

    /// <summary>
    /// Creates a bitmap render target whose bitmap can be used as a source for rendering
    /// in the API.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateCompatibleRenderTarget)</para>
    /// </summary>
    /// <param name="desiredPixelSize">The requested size of the render target in pixels. If the DIP size is also specified, the DPI is calculated from these two values. If the desired size is not specified, the DPI is inherited from the parent render target. If neither value is specified, the compatible render target will be the same size and have the same DPI as the parent target.</param>
    /// <param name="options">Allows the caller to retrieve a GDI compatible render target.</param>
    /// <returns>A bitmap render target.</returns>
    BitmapRenderTarget ^
    CreateCompatibleRenderTarget(
        CompatibleRenderTargetOptions options,
        SizeU desiredPixelSize
        );

    /// <summary>
    /// Creates a bitmap render target whose bitmap can be used as a source for rendering
    /// in the API.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateCompatibleRenderTarget)</para>
    /// </summary>
    /// <param name="options">Allows the caller to retrieve a GDI compatible render target.</param>
    /// <returns>A bitmap render target.</returns>
    BitmapRenderTarget ^
    CreateCompatibleRenderTarget(
        CompatibleRenderTargetOptions options
        );

    /// <summary>
    /// Creates a bitmap render target whose bitmap can be used as a source for rendering
    /// in the API.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateCompatibleRenderTarget)</para>
    /// </summary>
    /// <param name="desiredSize">The requested size of the target in DIPs. If the pixel size is not specified, the DPI is inherited from the parent target. However, the render target will never contain a fractional number of pixels.</param>
    /// <param name="desiredPixelSize">The requested size of the render target in pixels. If the DIP size is also specified, the DPI is calculated from these two values. If the desired size is not specified, the DPI is inherited from the parent render target. If neither value is specified, the compatible render target will be the same size and have the same DPI as the parent target.</param>
    /// <param name="desiredFormat">The desired pixel format. The format must be compatible with the parent render target type. If the format is not specified, it will be inherited from the parent render target.</param>
    /// <param name="options">Allows the caller to retrieve a GDI compatible render target.</param>
    /// <returns>A bitmap render target.</returns>
    BitmapRenderTarget ^
    CreateCompatibleRenderTarget(
        CompatibleRenderTargetOptions options,
        Direct2D1::PixelFormat desiredFormat,
        SizeF desiredSize,
        SizeU desiredPixelSize
        );

    /// <summary>
    /// Creates a bitmap render target whose bitmap can be used as a source for rendering
    /// in the API.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateCompatibleRenderTarget)</para>
    /// </summary>
    /// <param name="desiredSize">The requested size of the target in DIPs. If the pixel size is not specified, the DPI is inherited from the parent target. However, the render target will never contain a fractional number of pixels.</param>
    /// <param name="desiredFormat">The desired pixel format. The format must be compatible with the parent render target type. If the format is not specified, it will be inherited from the parent render target.</param>
    /// <param name="options">Allows the caller to retrieve a GDI compatible render target.</param>
    /// <returns>A bitmap render target.</returns>
    BitmapRenderTarget ^
    CreateCompatibleRenderTarget(
        CompatibleRenderTargetOptions options,
        Direct2D1::PixelFormat desiredFormat,
        SizeF desiredSize
        );

    /// <summary>
    /// Creates a bitmap render target whose bitmap can be used as a source for rendering
    /// in the API.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateCompatibleRenderTarget)</para>
    /// </summary>
    /// <param name="desiredPixelSize">The requested size of the render target in pixels. If the DIP size is also specified, the DPI is calculated from these two values. If the desired size is not specified, the DPI is inherited from the parent render target. If neither value is specified, the compatible render target will be the same size and have the same DPI as the parent target.</param>
    /// <param name="desiredFormat">The desired pixel format. The format must be compatible with the parent render target type. If the format is not specified, it will be inherited from the parent render target.</param>
    /// <param name="options">Allows the caller to retrieve a GDI compatible render target.</param>
    /// <returns>A bitmap render target.</returns>
    BitmapRenderTarget ^
    CreateCompatibleRenderTarget(
        CompatibleRenderTargetOptions options,
        Direct2D1::PixelFormat desiredFormat,
        SizeU desiredPixelSize
        );

    /// <summary>
    /// Creates a bitmap render target whose bitmap can be used as a source for rendering
    /// in the API.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateCompatibleRenderTarget)</para>
    /// </summary>
    /// <param name="desiredFormat">The desired pixel format. The format must be compatible with the parent render target type. If the format is not specified, it will be inherited from the parent render target.</param>
    /// <param name="options">Allows the caller to retrieve a GDI compatible render target.</param>
    /// <returns>A bitmap render target.</returns>
    BitmapRenderTarget ^
    CreateCompatibleRenderTarget(
        CompatibleRenderTargetOptions options,
        Direct2D1::PixelFormat desiredFormat
        );

    /// <summary>
    /// Creates a layer resource that can be used on any target and which will resize under
    /// the covers if necessary.
    /// Use this overload to prevent unwanted reallocation of the layer backing store. The size is in DIPs, but, it is unaffected by the current world transform.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateLayer)</para>
    /// </summary>
    /// <param name="size">The resolution independent minimum size hint for the layer resource.</param>
    /// <returns>Layer</returns>
    Layer ^
    CreateLayer(
        SizeF size
        );

    /// <summary>
    /// Creates a layer resource that can be used on any target and which will resize under
    /// the covers if necessary.
    /// The size will be unspecified so the returned resource is a placeholder and the backing store will be allocated to be the minimum size that can hold the content when the layer is pushed.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateLayer)</para>
    /// </summary>
    /// <returns>Layer</returns>
    Layer ^
    CreateLayer(
        );
    /// <summary>
    /// Create a mesh that uses triangles to describe a shape.
    /// Create a D2D mesh.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::CreateMesh)</para>
    /// </summary>
    /// <returns>Mesh</returns>
    Mesh ^
    CreateMesh(
        );

    /// <summary>
    /// Draws a line between the specified points using the specified stroke style. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawLine)</para>
    /// </summary>
    /// <param name="firstPoint">The first point.</param>
    /// <param name="secondPoint">The second point.</param>
    /// <param name="brush">The brush parameter.</param>
    /// <param name="strokeWidth">The strokeWidth parameter.</param>
    /// <param name="strokeStyle">The strokeStyle parameter.</param>
    void
    DrawLine(
        Point2F firstPoint,
        Point2F secondPoint,
        Brush ^ brush,
        FLOAT strokeWidth,
        StrokeStyle ^ strokeStyle
        );

    /// <summary>
    /// Draws a line between the specified points using the specified stroke style. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawLine)</para>
    /// </summary>
    /// <param name="firstPoint">The first point.</param>
    /// <param name="secondPoint">The second point.</param>
    /// <param name="brush">The brush parameter.</param>
    /// <param name="strokeWidth">The strokeWidth parameter.</param>
    void
    DrawLine(
        Point2F firstPoint,
        Point2F secondPoint,
        Brush ^ brush,
        FLOAT strokeWidth
        );

    /// <summary>
    /// Draws the outline of a rectangle that has the specified dimensions and stroke style and width.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawRectangle)</para>
    /// </summary>
    /// <param name="rect">The rect parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    /// <param name="strokeWidth">The strokeWidth parameter.</param>
    /// <param name="strokeStyle">The strokeStyle parameter.</param>
    void
    DrawRectangle(
        RectF rect,
        Brush ^ brush,
        FLOAT strokeWidth,
        StrokeStyle ^ strokeStyle
        );

    /// <summary>
    /// Draws the outline of a rectangle that has the specified dimensions and stroke width.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawRectangle)</para>
    /// </summary>
    /// <param name="rect">The rect parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    /// <param name="strokeWidth">The strokeWidth parameter.</param>
    void
    DrawRectangle(
        RectF rect,
        Brush ^ brush,
        FLOAT strokeWidth
        );
    /// <summary>
    /// Paints the interior of the specified rectangle. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::FillRectangle)</para>
    /// </summary>
    /// <param name="rect">The rect parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    void
    FillRectangle(
        RectF rect,
        Brush ^ brush
        );

    /// <summary>
    /// Draws the outline of the specified rounded rectangle.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawRoundedRectangle)</para>
    /// </summary>
    /// <param name="roundedRect">The roundedRect parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    /// <param name="strokeWidth">The strokeWidth parameter.</param>
    /// <param name="strokeStyle">The strokeStyle parameter.</param>
    void
    DrawRoundedRectangle(
        RoundedRect roundedRect,
        Brush ^ brush,
        FLOAT strokeWidth,
        StrokeStyle ^ strokeStyle
        );

    /// <summary>
    /// Draws the outline of the specified rounded rectangle.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawRoundedRectangle)</para>
    /// </summary>
    /// <param name="roundedRect">The roundedRect parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    /// <param name="strokeWidth">The strokeWidth parameter.</param>
    void
    DrawRoundedRectangle(
        RoundedRect roundedRect,
        Brush ^ brush,
        FLOAT strokeWidth
        );
    /// <summary>
    /// Paints the interior of the specified rounded rectangle.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::FillRoundedRectangle)</para>
    /// </summary>
    /// <param name="roundedRect">The roundedRect parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    void
    FillRoundedRectangle(
        RoundedRect roundedRect,
        Brush ^ brush
        );

    /// <summary>
    /// Draws the outline of an ellipse with the specified dimensions and stroke.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawEllipse)</para>
    /// </summary>
    /// <param name="ellipse">The ellipse parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    /// <param name="strokeWidth">The strokeWidth parameter.</param>
    /// <param name="strokeStyle">The strokeStyle parameter.</param>
    void
    DrawEllipse(
        Ellipse ellipse,
        Brush ^ brush,
        FLOAT strokeWidth,
        StrokeStyle ^ strokeStyle
        );

    /// <summary>
    /// Draws the outline of an ellipse with the specified dimensions and stroke width.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawEllipse)</para>
    /// </summary>
    /// <param name="ellipse">The ellipse parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    /// <param name="strokeWidth">The strokeWidth parameter.</param>
    void
    DrawEllipse(
        Ellipse ellipse,
        Brush ^ brush,
        FLOAT strokeWidth    
        );

    /// <summary>
    /// Paints the interior of the specified ellipse.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::FillEllipse)</para>
    /// </summary>
    /// <param name="ellipse">The ellipse parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    void
    FillEllipse(
        Ellipse ellipse,
        Brush ^ brush
        );

    /// <summary>
    /// Draws the outline of the specified geometry using the specified stroke style. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawGeometry)</para>
    /// </summary>
    /// <param name="geometry">The geometry parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    /// <param name="strokeWidth">The strokeWidth parameter.</param>
    /// <param name="strokeStyle">The strokeStyle parameter.</param>
    void
    DrawGeometry(
        Geometry ^ geometry,
        Brush ^ brush,
        FLOAT strokeWidth,
        StrokeStyle ^ strokeStyle
        );

    /// <summary>
    /// Draws the outline of the specified geometry using the specified stroke width. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawGeometry)</para>
    /// </summary>
    /// <param name="geometry">The geometry parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    /// <param name="strokeWidth">The strokeWidth parameter.</param>
    void
    DrawGeometry(
        Geometry ^ geometry,
        Brush ^ brush,
        FLOAT strokeWidth
        );

    /// <summary>
    /// Paints the interior of the specified geometry. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::FillGeometry)</para>
    /// </summary>
    /// <param name="geometry">The geometry parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    /// <param name="opacityBrush">An optionally specified opacity brush. Only the alpha channel of the corresponding brush will be sampled and will be applied to the entire fill of the geometry. If this brush is specified, the fill brush must be a bitmap brush with an extend mode of D2D1_EXTEND_MODE_CLAMP.</param>
    void
    FillGeometry(
        Geometry ^ geometry,
        Brush ^ brush,
        Brush ^ opacityBrush
        );

    /// <summary>
    /// Paints the interior of the specified geometry. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::FillGeometry)</para>
    /// </summary>
    /// <param name="geometry">The geometry parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    void
    FillGeometry(
        Geometry ^ geometry,
        Brush ^ brush
        );

    /// <summary>
    /// Fill a mesh. Since meshes can only render aliased content, the render target antialiasing
    /// mode must be set to aliased.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::FillMesh)</para>
    /// </summary>
    /// <param name="mesh">The mesh parameter.</param>
    /// <param name="brush">The brush parameter.</param>
    void
    FillMesh(
        Mesh ^ mesh,
        Brush ^ brush
        );

    /// <summary>
    /// Fill using the opacity channel of the supplied bitmap as a mask. The alpha channel
    /// of the bitmap is used to represent the coverage of the geometry at each pixel, and
    /// this is filled appropriately with the brush. The render target antialiasing mode
    /// must be set to aliased.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::FillOpacityMask)</para>
    /// </summary>
    /// <param name="opacityMask">The opacity mask to apply to the brush. The alpha value of each pixel in the region specified by sourceRectangle is multiplied with the alpha value of the brush after the brush has been mapped to the area defined by destinationRectangle.</param>
    /// <param name="brush">The brush used to paint the region of the render target specified by destinationRectangle.</param>
    /// <param name="content">The type of contain the opacity mask contains. The value is used to determine the color space in which the opacity mask is blended.</param>
    /// <param name="destinationRectangle">The dimensions of the area of the render target to paint, in device-independent pixels.</param>
    /// <param name="sourceRectangle">The dimensions of the area of the bitmap to use as the opacity mask, in device-independent pixels.</param>
    void
    FillOpacityMask(
        D2DBitmap ^ opacityMask,
        Brush ^ brush,
        OpacityMaskContent content,
        RectF destinationRectangle,
        RectF sourceRectangle
        );

    /// <summary>
    /// Fill using the opacity channel of the supplied bitmap as a mask. The alpha channel
    /// of the bitmap is used to represent the coverage of the geometry at each pixel, and
    /// this is filled appropriately with the brush. The render target antialiasing mode
    /// must be set to aliased.
    /// The brush uses the sourceRectangle area to draw at the orgin of the bitmap.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::FillOpacityMask)</para>
    /// </summary>
    /// <param name="opacityMask">The opacity mask to apply to the brush. The alpha value of each pixel in the region specified by sourceRectangle is multiplied with the alpha value of the brush after the brush has been mapped to the area defined by destinationRectangle.</param>
    /// <param name="brush">The brush used to paint the region of the render target specified by destinationRectangle.</param>
    /// <param name="content">The type of contain the opacity mask contains. The value is used to determine the color space in which the opacity mask is blended.</param>
    /// <param name="sourceRectangle">The dimensions of the area of the bitmap to use as the opacity mask, in device-independent pixels.</param>
    void
    FillOpacityMaskAtOrigin(
        D2DBitmap ^ opacityMask,
        Brush ^ brush,
        OpacityMaskContent content,
        RectF sourceRectangle
        );

    /// <summary>
    /// Fill using the opacity channel of the supplied bitmap as a mask. The alpha channel
    /// of the bitmap is used to represent the coverage of the geometry at each pixel, and
    /// this is filled appropriately with the brush. The render target antialiasing mode
    /// must be set to aliased.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::FillOpacityMask)</para>
    /// </summary>
    /// <param name="opacityMask">The opacity mask to apply to the brush. The alpha value of each pixel in the region specified by sourceRectangle is multiplied with the alpha value of the brush after the brush has been mapped to the area defined by destinationRectangle.</param>
    /// <param name="brush">The brush used to paint the region of the render target specified by destinationRectangle.</param>
    /// <param name="content">The type of contain the opacity mask contains. The value is used to determine the color space in which the opacity mask is blended.</param>
    /// <param name="destinationRectangle">The dimensions of the area of the render target to paint, in device-independent pixels.</param>
    void
    FillOpacityMask(
        D2DBitmap ^ opacityMask,
        Brush ^ brush,
        OpacityMaskContent content,
        RectF destinationRectangle
        );

    /// <summary>
    /// Fill using the opacity channel of the supplied bitmap as a mask. The alpha channel
    /// of the bitmap is used to represent the coverage of the geometry at each pixel, and
    /// this is filled appropriately with the brush. The render target antialiasing mode
    /// must be set to aliased.
    /// The brush paints a rectangle the same size as the opacityMask bitmap and positioned on the origin.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::FillOpacityMask)</para>
    /// </summary>
    /// <param name="opacityMask">The opacity mask to apply to the brush. The alpha value of each pixel in the region specified by sourceRectangle is multiplied with the alpha value of the brush after the brush has been mapped to the area defined by destinationRectangle.</param>
    /// <param name="brush">The brush used to paint the region of the render target specified by destinationRectangle.</param>
    /// <param name="content">The type of contain the opacity mask contains. The value is used to determine the color space in which the opacity mask is blended.</param>
    void
    FillOpacityMask(
        D2DBitmap ^ opacityMask,
        Brush ^ brush,
        OpacityMaskContent content
        );

    /// <summary>Draws the specified bitmap at the origin with no scaling 
    /// and applying nearest neighbor filtering if needed.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawBitmap)</para>
    /// </summary>
    void
    DrawBitmap(
        D2DBitmap ^ bitmap
        );

    /// <summary>Draws a portion of the specified bitmap after scaling it to the size of the specified rectangle.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawBitmap)</para>
    /// </summary>
    /// <param name="bitmap">The bitmap parameter.</param>
    /// <param name="opacity">The opacity parameter.</param>
    /// <param name="interpolationMode">The interpolationMode parameter.</param>
    /// <param name="destinationRectangle">The destinationRectangle parameter.</param>
    /// <param name="sourceRectangle">The sourceRectangle parameter.</param>
    void
    DrawBitmap(
        D2DBitmap ^ bitmap,
        FLOAT opacity,
        BitmapInterpolationMode interpolationMode,
        RectF destinationRectangle,
        RectF sourceRectangle
        );

    /// <summary>Draws the entire specified bitmap after scaling it to the size of the specified rectangle.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawBitmap)</para>
    /// </summary>
    /// <param name="bitmap">The bitmap parameter.</param>
    /// <param name="opacity">The opacity parameter.</param>
    /// <param name="interpolationMode">The interpolationMode parameter.</param>
    /// <param name="destinationRectangle">The destinationRectangle parameter.</param>
    void
    DrawBitmap(
        D2DBitmap ^ bitmap,
        FLOAT opacity,
        BitmapInterpolationMode interpolationMode,
        RectF destinationRectangle
        );

    /// <summary>Draws a portion of the specified bitmap after scaling it to the size of the specified rectangle.
    /// The selected portion of the bitmap will be drawn at the origin of the render target. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawBitmap)</para>
    /// </summary>
    /// <param name="bitmap">The bitmap parameter.</param>
    /// <param name="opacity">The opacity parameter.</param>
    /// <param name="interpolationMode">The interpolationMode parameter.</param>
    /// <param name="sourceRectangle">The sourceRectangle parameter.</param>
    void
    DrawBitmapAtOrigin(
        D2DBitmap ^ bitmap,
        FLOAT opacity,
        BitmapInterpolationMode interpolationMode,
        RectF sourceRectangle
        );

    /// <summary>Draws the entire specified bitmap after scaling it to the size of the specified rectangle.
    /// The selected portion of the bitmap will be drawn at the origin of the render target. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawBitmap)</para>
    /// </summary>
    /// <param name="bitmap">The bitmap parameter.</param>
    /// <param name="opacity">The opacity parameter.</param>
    /// <param name="interpolationMode">The interpolationMode parameter.</param>
    void
    DrawBitmap(
        D2DBitmap ^ bitmap,
        FLOAT opacity,
        BitmapInterpolationMode interpolationMode
        );


    /// <summary>
    /// Provide a GdiInteropRenderTarget version of this render target. 
    /// </summary>
    /// <remarks>
    /// Not all render targets support <see cref="GdiInteropRenderTarget"/>. 
    /// The render target must be GDI-compatible (the RenderTargetUsages.GdiCompatible flag was specified when creating the render target), 
    /// use the Graphics's Format.B8G8R8A8UNorm pixel format, and use the AlphaMode.Premultiplied or AlphaMode.Ignore alpha mode.
    /// Note that this method always succeeds; if the render target doesn't support the GdiInteropRenderTarget interface, 
    /// calling GetDC will fail. (For render targets created through the CreateCompatibleRenderTarget method, 
    /// the render target that created it must have these settings.) 
    /// To test whether a given render target supports GdiInteropRenderTarget, 
    /// create a <see cref="RenderTargetProperties"/> that specifies GDI compatibility and 
    /// the appropriate pixel format, then call the render target's IsSupported property 
    /// to see whether the render target is GDI-compatible.
    /// </remarks>
    property GdiInteropRenderTarget^ GdiInteropRenderTarget
    {
        Direct2D1::GdiInteropRenderTarget^ get(void);
    }

    /// <summary>
    /// The current transform of the render target. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::GetTransform, ID2D1RenderTarget::SetTransform)</para>
    /// </summary>
    property Matrix3x2F Transform
    {
        Matrix3x2F get();
        void set(Matrix3x2F);
    }

    /// <summary>
    /// The current antialiasing mode of the render target. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::GetAntiAliasMode, ID2D1RenderTarget::SetAntiAliasMode)</para>
    /// </summary>
    property Direct2D1::AntiAliasMode AntiAliasMode
    {
        Direct2D1::AntiAliasMode get();
        void set(Direct2D1::AntiAliasMode);
    }


    /// <summary>
    /// The current antialiasing mode for text and glyph drawing operations. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::GetTextAntiAliasMode, ID2D1RenderTarget::SetTextAntiAliasMode)</para>
    /// </summary>
    /// <param name="textAntiAliasMode">The textAntiAliasMode parameter.</param>
    property Direct2D1::TextAntiAliasMode TextAntiAliasMode
    {
        Direct2D1::TextAntiAliasMode get();
        void set(Direct2D1::TextAntiAliasMode);
    }

    /// <summary>
    /// Get or Set a tag to labelto the succeeding drawing operations. If an error occurs while rendering
    /// a primtive, the tags can be returned from the Flush or EndDraw call.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::GetTags, ID2D1RenderTarget::SetTags)</para>
    /// </summary>
    property Direct2D1::Tags Tags
    {
        Direct2D1::Tags get();
        void set(Direct2D1::Tags);
    }

    /// <summary>
    /// Start a layer of drawing calls. The way in which the layer must be resolved is specified
    /// first as well as the logical resource that stores the layer parameters. The supplied
    /// layer resource might grow if the specified content cannot fit inside it. The layer
    /// will grow monitonically on each axis.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::PushLayer)</para>
    /// </summary>
    /// <param name="layerParameters">The layerParameters parameter.</param>
    /// <param name="layer">The layer parameter.</param>
    void
    PushLayer(
        LayerParameters layerParameters,
        Layer ^ layer
        );

    /// <summary>
    /// Ends a layer that was defined with particular layer resources.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::PopLayer)</para>
    /// </summary>
    void
    PopLayer(
        );

    /// <summary>
    /// Executes all pending drawing commands. This method will throw if an error occur during
    /// the operation. To prevent exceptions, TryFlush() can be used instead.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::Flush)</para>
    /// </summary>
    /// <returns>The associated tags.</returns>
    Direct2D1::Tags
    Flush(
        );

    /// <summary>
    /// Executes all pending drawing commands.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::EndDraw)</para>
    /// </summary>
    /// <param name="tags">The associated tags.</param>
    /// <param name="errorCode">The associated tags.</param>
    /// <returns>True if successful; False otherwise. If unsuccessful, the tags and errorCode parameters should be used to find out which operation has failed.</returns>
    bool TryFlush([System::Runtime::InteropServices::Out] Direct2D1::Tags % tags, [System::Runtime::InteropServices::Out] ErrorCode % errorCode);

    /// <summary>
    /// Gets the current drawing state and saves it into the supplied IDrawingStatckBlock.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::SaveDrawingState)</para>
    /// </summary>
    /// <param name="drawingStateBlock">The drawingStateBlock parameter.</param>
    void
    SaveDrawingState(
        DrawingStateBlock ^ drawingStateBlock
        );

    /// <summary>
    /// Copies the state stored in the block interface.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::RestoreDrawingState)</para>
    /// </summary>
    /// <param name="drawingStateBlock">The drawingStateBlock parameter.</param>
    void
    RestoreDrawingState(
        DrawingStateBlock ^ drawingStateBlock
        );

    /// <summary>
    /// Pushes a clip. The clip can be antialiased. The clip must be axis aligned. If the
    /// current world transform is not axis preserving, then the bounding box of the transformed
    /// clip rect will be used. The clip will remain in effect until a PopAxisAligned clip
    /// call is made.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::PushAxisAlignedClip)</para>
    /// </summary>
    /// <param name="clipRect">The clipRect parameter.</param>
    /// <param name="antiAliasMode">The antiAliasMode parameter.</param>
    void
    PushAxisAlignedClip(
        RectF clipRect,
        Direct2D1::AntiAliasMode antiAliasMode
        );

    /// <summary>
    /// Removes the last axis-aligned clip from the render target. After this method is called, the clip is no longer applied to subsequent drawing operations.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::PopAxisAlignedClip)</para>
    /// </summary>
    /// <remarks>
    /// A PushAxisAlignedClip/PopAxisAlignedClip pair can occur around or within a PushLayer/PopLayer pair, but may not overlap. For example, a PushAxisAlignedClip, PushLayer, PopLayer, PopAxisAlignedClip sequence is valid, but a PushAxisAlignedClip, PushLayer, PopAxisAlignedClip, PopLayer sequence is not. 
    /// PopAxisAlignedClip must be called once for every call to PushAxisAlignedClip.
    /// </remarks>
    void
    PopAxisAlignedClip(
        );

    /// <summary>
    /// Clears the drawing area to the specified color. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::Clear)</para>
    /// </summary>
    /// <remarks>For render targets that support alpha, the clearColor is treated as straight alpha, regardless of whether the alpha mode is straight alpha or premultiplied alpha. For targets that ignore alpha, the alpha of the clear color is interpreted as fully opaque.
    /// If the render target has an active clip (specified by PushAxisAlignedClip), the clear command is only applied to the area within the clip region.
    /// </remarks>
    /// <param name="clearColor">The clearColor parameter.</param>
    void
    Clear(
        ColorF clearColor
        );

    /// <summary>
    /// Clears the drawing area to transparent black. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::Clear)</para>
    /// </summary>
    void
    Clear(
        );

    /// <summary>
    /// Start drawing on this render target. Draw calls can only be issued between a BeginDraw
    /// and EndDraw call.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::BeginDraw)</para>
    /// </summary>
    void
    BeginDraw(
        );

    /// <summary>
    /// Ends drawing on the render target. If an error occur while drawing, an exception will
    /// be thrown. To prevent exceptions, you can use the TryEndDraw() method instead.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::EndDraw)</para>
    /// </summary>
    /// <returns>The associated tags.</returns>
    Direct2D1::Tags
    EndDraw(
        );


    /// <summary>
    /// Ends drawing on the render target, error results can be retrieved at this time, or
    /// when calling Flush() or TryFlush().
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::EndDraw)</para>
    /// </summary>
    /// <param name="tags">The associated tags.</param>
    /// <param name="errorCode">The associated tags.</param>
    /// <returns>True if successful; False otherwise. If unsuccessful, the tags and errorCode parameters should be used to find out which operation has failed.</returns>
    /// <remarks>This method does not throw Direct2D exceptions if a drawing error occur while rendering shapes.</remarks>
    bool
    TryEndDraw([System::Runtime::InteropServices::Out] Direct2D1::Tags % tags, [System::Runtime::InteropServices::Out] ErrorCode % errorCode
        );

    /// <summary>
    /// The pixel format and alpha mode of the render target.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::GetPixelFormat)</para>
    /// </summary>
    property Direct2D1::PixelFormat PixelFormat
    {
        Direct2D1::PixelFormat get();
    }

    /// <summary>
    /// The DPI on the render target. This results in the render target being interpretted
    /// to a different scale. Neither DPI can be negative. If zero is specified for both,
    /// the system DPI is chosen. If one is zero and the other unspecified, the DPI is not
    /// changed.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::GetDpi, ID2D1RenderTarget::SetDpi)</para>
    /// </summary>
    /// <param name="dpi">The dpi value to set.</param>
    property DpiF Dpi
    {
        DpiF get();
        void set(DpiF);
    }

    /// <summary>
    /// Returns the size of the render target in DIPs.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::GetSize)</para>
    /// </summary>
    property SizeF Size
    {
        SizeF get();
    }

    /// <summary>
    /// Returns the size of the render target in pixels.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::GetPixelSize)</para>
    /// </summary>
    property SizeU PixelSize
    {
        SizeU get();
    }

    /// <summary>
    /// Returns the maximum bitmap and render target size that is guaranteed to be supported
    /// by the render target.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::GetMaximumBitmapSize)</para>
    /// </summary>
    property UINT32 MaximumBitmapSize
    {
        UINT32 get();
    }
        
    /// <summary>
    /// Returns true if the given properties are supported by this render target. The DPI
    /// is ignored. NOTE: If the render target type is software, then neither feature level 9
    /// nor feature level 10 will be considered to be supported.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::IsSupported)</para>
    /// </summary>
    /// <param name="renderTargetProperties">The renderTargetProperties parameter.</param>
    /// <returns>True if supported; otherwise false.</returns>
    Boolean
    IsSupported(
        RenderTargetProperties renderTargetProperties
        );

    /// <summary>Draws the specified text using the format information provided by a DWrite TextFormat object.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawText)</para>
    /// </summary>
    /// <param name="text">Characters to draw.</param>
    /// <param name="textFormat">An object that describes formatting details of the text to draw, such as the font, the font size, and flow direction.</param>
    /// <param name="layoutRect">The size and position of the area in which the text is drawn.</param>
    /// <param name="defaultForegroundBrush">The brush used to paint the text.</param>
    /// <param name="options">A value that indicates whether the text should be snapped to pixel boundaries and whether the text should be clipped to the layout rectangle. The default value is None, which indicates that text should be snapped to pixel boundaries and it should be clipped to the layout rectangle.</param>
    /// <param name="measuringMode">A value that indicates how glyph metrics are used to measure text when it is formatted. The default value is Natural mode.</param>
    void DrawText(
        String^ text,
        TextFormat^ textFormat,
        RectF layoutRect,
        Brush^ defaultForegroundBrush,
        DrawTextOptions options,
        MeasuringMode measuringMode);

    /// <summary>Draws the specified text using the format information provided by a DWrite TextFormat object.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawText)</para>
    /// </summary>
    /// <param name="text">Characters to draw.</param>
    /// <param name="textFormat">An object that describes formatting details of the text to draw, such as the font, the font size, and flow direction.</param>
    /// <param name="layoutRect">The size and position of the area in which the text is drawn.</param>
    /// <param name="defaultForegroundBrush">The brush used to paint the text.</param>
    /// <param name="options">A value that indicates whether the text should be snapped to pixel boundaries and whether the text should be clipped to the layout rectangle. The default value is None, which indicates that text should be snapped to pixel boundaries and it should be clipped to the layout rectangle.</param>
    void DrawText(
        String^ text,
        TextFormat^ textFormat,
        RectF layoutRect,
        Brush^ defaultForegroundBrush,
        DrawTextOptions options
        );

    /// <summary>Draws the specified text using the format information provided by a DWrite TextFormat object.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawText)</para>
    /// </summary>
    /// <param name="text">Characters to draw.</param>
    /// <param name="textFormat">An object that describes formatting details of the text to draw, such as the font, the font size, and flow direction.</param>
    /// <param name="layoutRect">The size and position of the area in which the text is drawn.</param>
    /// <param name="defaultForegroundBrush">The brush used to paint the text.</param>
    /// <param name="measuringMode">A value that indicates how glyph metrics are used to measure text when it is formatted. The default value is Natural mode.</param>
    void DrawText(
        String^ text,
        TextFormat^ textFormat,
        RectF layoutRect,
        Brush^ defaultForegroundBrush,    
        MeasuringMode measuringMode);

    /// <summary>Draws the specified text using the format information provided by a DWrite TextFormat object.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawText)</para>
    /// </summary>
    /// <param name="text">Characters to draw.</param>
    /// <param name="textFormat">An object that describes formatting details of the text to draw, such as the font, the font size, and flow direction.</param>
    /// <param name="layoutRect">The size and position of the area in which the text is drawn.</param>
    /// <param name="defaultForegroundBrush">The brush used to paint the text.</param>
    void DrawText(
        String^ text,
        TextFormat^ textFormat,
        RectF layoutRect,
        Brush^ defaultForegroundBrush);

    /// <summary>Draws the specified glyphs.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawGlyphRun)</para>
    /// </summary>
    /// <param name="baselineOrigin">The origin, in device-independent pixels, of the glyphs' baseline.</param>
    /// <param name="glyphRun">The glyphs to render.</param>
    /// <param name="foregroundBrush">The brush used to paint the specified glyphs.</param>
    /// <param name="measuringMode">A value that indicates how glyph metrics are used to measure text when it is formatted. The default value is Natural.</param>
    void DrawGlyphRun(
        Point2F baselineOrigin,
        array<DirectWrite::GlyphRun>^ glyphRun,
        Brush^ foregroundBrush,
        MeasuringMode measuringMode);

    /// <summary>Draws the specified glyphs.
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawGlyphRun)</para>
    /// </summary>
    /// <param name="baselineOrigin">The origin, in device-independent pixels, of the glyphs' baseline.</param>
    /// <param name="glyphRun">The glyphs to render.</param>
    /// <param name="foregroundBrush">The brush used to paint the specified glyphs.</param>
    void DrawGlyphRun(
        Point2F baselineOrigin,
        array<DirectWrite::GlyphRun>^ glyphRun,
        Brush^ foregroundBrush);

    /// <summary>
    /// Gets or set the render target's current text-rendering options.
    /// Text rendering options to apply to all text and glyph drawing operations. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::GetTextRenderingParams, ID2D1RenderTarget::SetTextRenderingParams)</para>
    /// </summary>
    /// <remarks>
    /// Note that the render target's text antialiasing mode overrides any antialiasing 
    /// mode specified by the RenderingParams object.
    /// </remarks>
    property RenderingParams^ TextRenderingParams
    {
        RenderingParams^ get(void);
        void set(RenderingParams^ textRenderingParams);
    }

    /// <summary>
    /// Draws the formatted text described by the specified IDWriteTextLayout object. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawTextLayout )</para>
    /// </summary>
    /// <param name="origin">The point, described in device-independent pixels, at which the upper-left corner of the text described by textLayout is drawn.</param>
    /// <param name="textLayout">The formatted text to draw.</param>
    /// <param name="defaultForegroundBrush">The brush used to paint any text in textLayout that does not already have a brush associated with it.</param>
    /// <param name="options">A value that indicates whether the text should be snapped to pixel boundaries and whether the text should be clipped to the layout rectangle. The default value is None, which indicates that text should be snapped to pixel boundaries and it should be clipped to the layout rectangle.</param>
    void DrawTextLayout(
        Point2F origin,
        TextLayout^ textLayout,
        Brush^ defaultForegroundBrush,
        DrawTextOptions options);

    /// <summary>
    /// Draws the formatted text described by the specified IDWriteTextLayout object. 
    /// <para>(Also see DirectX SDK: ID2D1RenderTarget::DrawTextLayout )</para>
    /// </summary>
    /// <param name="origin">The point, described in device-independent pixels, at which the upper-left corner of the text described by textLayout is drawn.</param>
    /// <param name="textLayout">The formatted text to draw.</param>
    /// <param name="defaultForegroundBrush">The brush used to paint any text in textLayout that does not already have a brush associated with it.</param>
    void DrawTextLayout(
        Point2F origin,
        TextLayout^ textLayout,
        Brush^ defaultForegroundBrush);

internal:
    RenderTarget();
    RenderTarget(
        IUnknown *pInner
        );

};

/// <summary>
/// Renders to an intermediate texture.
/// This <see cref="RenderTarget"/> is created by <see cref="RenderTarget::CreateCompatibleRenderTarget()"/> or one of its overloads.
/// <para>(Also see DirectX SDK: ID2D1BitmapRenderTarget)</para>
/// </summary>
public ref class BitmapRenderTarget : RenderTarget 
{
public:

    /// <summary>
    /// Retrieves the bitmap for this render target. The returned bitmap can be used for drawing operations.
    /// <para>(Also see DirectX SDK: ID2D1BitmapRenderTarget::GetBitmap)</para>
    /// </summary>
    property D2DBitmap^ Bitmap
    {
        D2DBitmap^ get(void);
    }

internal:

    BitmapRenderTarget();

    BitmapRenderTarget(
        IUnknown *pInner
        );

};

/// <summary>
/// Renders drawing instructions to a window.
/// <para>(Also see DirectX SDK: ID2D1HwndRenderTarget)</para>
/// </summary>
public ref class HwndRenderTarget : RenderTarget 
{
public:

    /// <summary>
    /// Indicates whether the HWND associated with this render target is occluded.
    /// <para>(Also see DirectX SDK: ID2D1HwndRenderTarget::CheckWindowState)</para>
    /// </summary>
    property Boolean IsOccluded
    {
        Boolean get();
    }

    /// <summary>
    /// Check the current WindowState.
    /// <para>(Also see DirectX SDK: ID2D1HwndRenderTarget::CheckWindowState)</para>
    /// </summary>
    /// <returns>The current WindowState</returns>
    WindowState CheckWindowState();

    /// <summary>
    /// Resize the buffer underlying the render target to the specified device pixel size. 
    /// <para>(Also see DirectX SDK: ID2D1HwndRenderTarget::Resize)</para>
    /// </summary>
    /// <remarks>
    /// This operation might fail if there is insufficent video memory or system memory, or if the render target is resized
    /// beyond the maximum bitmap size. If the method fails, the render target will be placed in a zombie state and an exception will be thrown when EndDraw is called.
    /// In addition an appropriate exception will be thrown from Resize.
    /// </remarks>
    /// <param name="pixelSize">The new pixel size.</param>
    void
    Resize(
        SizeU pixelSize
        );

    /// <summary> Get the handle to the window associated with this render target.
    /// <para>(Also see DirectX SDK: ID2D1HwndRenderTarget::GetHwnd)</para>
    /// </summary>
    property IntPtr WindowHandle 
    {
        IntPtr get();
    }

internal:

    HwndRenderTarget();

    HwndRenderTarget(
        IUnknown *pInner
        );

};

/// <summary>
/// Provides access to an device context that can accept GDI drawing commands. 
/// <para>(Also see DirectX SDK: ID2D1GdiInteropRenderTarget)</para>
/// </summary>
public interface class IGdiInteropRenderTarget 
{
public:
    /// <summary>
    /// Retrieves the device context associated with this render target. 
    /// <para>(Also see DirectX SDK: ID2D1GdiInteropRenderTarget::GetDC)</para>
    /// </summary>
    /// <param name="mode">The mode parameter.</param>
    /// <returns>Device context handle.</returns>
    virtual
    IntPtr
    GetDC(
        DCInitializeMode mode
        );

    /// <summary>
    /// Indicates that drawing with the device context retrieved using the GetDC method is finished. 
    /// <para>(Also see DirectX SDK: ID2D1GdiInteropRenderTarget::ReleaseDC)</para>
    /// </summary>
    /// <param name="updateRectangle">The update rectantgle.</param>
    virtual
    void
    ReleaseDC(
        Rect updateRectangle
        );

    /// <summary>
    /// Indicates that drawing with the device context retrieved using the GetDC method is finished. 
    /// <para>(Also see DirectX SDK: ID2D1GdiInteropRenderTarget::ReleaseDC)</para>
    /// </summary>
    virtual
    void
    ReleaseDC();

};

/// <summary>
/// Provides access to an device context that can accept GDI drawing commands. 
/// <para>(Also see DirectX SDK: ID2D1GdiInteropRenderTarget)</para>
/// </summary>
public ref class GdiInteropRenderTarget : DirectUnknown, IGdiInteropRenderTarget
{
public:

    /// <summary>
    /// Retrieves the device context associated with this render target. 
    /// <para>(Also see DirectX SDK: ID2D1GdiInteropRenderTarget::GetDC)</para>
    /// </summary>
    /// <param name="mode">The mode parameter.</param>
    /// <returns>Device context handle.</returns>
    virtual
    IntPtr
    GetDC(
        DCInitializeMode mode
        );

    /// <summary>
    /// Indicates that drawing with the device context retrieved using the GetDC method is finished. 
    /// <para>(Also see DirectX SDK: ID2D1GdiInteropRenderTarget::ReleaseDC)</para>
    /// </summary>
    /// <param name="updateRectangle">The update rectangle.</param>
    virtual
    void
    ReleaseDC(
        Rect updateRectangle
        );

    /// <summary>
    /// Indicates that drawing with the device context retrieved using the GetDC method is finished. 
    /// <para>(Also see DirectX SDK: ID2D1GdiInteropRenderTarget::ReleaseDC)</para>
    /// </summary>
    virtual
    void
    ReleaseDC(
        );
internal:

    GdiInteropRenderTarget();

    GdiInteropRenderTarget(
        IUnknown *pInner
        );

};

/// <summary>
/// Issues drawing commands to a GDI device context. 
/// <para>(Also see DirectX SDK: ID2D1DCRenderTarget)</para>
/// </summary>
public ref class DCRenderTarget : RenderTarget 
{
public:

    /// <summary>
    /// Binds the render target to the device context to which it issues drawing commands.
    /// <para>(Also see DirectX SDK: ID2D1DCRenderTarget::BindDC)</para>
    /// </summary>
    /// <param name="deviceContextHandle">The device context to which the render target issues drawing commands.</param>
    /// <param name="subRect">The dimensions of the handle to a device context (HDC) to which the render target is bound. When this rectangle changes, the render target updates its size to match.</param>
    void
    BindDC(
        IntPtr deviceContextHandle,
        Rect subRect
        );

internal:

    DCRenderTarget();

    DCRenderTarget(
        IUnknown *pInner
        );

};

/// <summary>
/// Describes the caps, miter limit, line join, and dash information for a stroke.
/// <para>(Also see DirectX SDK: ID2D1StrokeStyle)</para>
/// </summary>
public ref class StrokeStyle : D2DResource 
{
public:

    /// <summary>
    /// Retrieves the type of shape used at the beginning of a stroke. 
    /// <para>(Also see DirectX SDK: ID2D1StrokeStyle::GetStartCap)</para>
    /// </summary>
    property CapStyle StartCap
    {
        CapStyle get();
    }

    /// <summary>
    /// Retrieves the type of shape used at the end of a stroke.
    /// <para>(Also see DirectX SDK: ID2D1StrokeStyle::GetEndCap)</para>
    /// </summary>
    property CapStyle EndCap
    {
        CapStyle get();
    }

    /// <summary>
    /// Gets a value that specifies how the ends of each dash are drawn.
    /// <para>(Also see DirectX SDK: ID2D1StrokeStyle::GetDashCap)</para>
    /// </summary>
    property CapStyle DashCap
    {
        CapStyle get();
    }

    /// <summary>
    /// Retrieves the limit on the ratio of the miter length to half the stroke's thickness.
    /// <para>(Also see DirectX SDK: ID2D1StrokeStyle::GetMiterLimit)</para>
    /// </summary>
    property FLOAT MiterLimit
    {
        FLOAT get();
    }

    /// <summary>
    /// Retrieves the type of joint used at the vertices of a shape's outline. 
    /// <para>(Also see DirectX SDK: ID2D1StrokeStyle::GetLineJoin)</para>
    /// </summary>
    property Direct2D1::LineJoin LineJoin
    {
        Direct2D1::LineJoin get();
    }

    /// <summary>
    /// Retrieves a value that specifies how far in the dash sequence the stroke will start.
    /// <para>(Also see DirectX SDK: ID2D1StrokeStyle::GetDashOffset)</para>
    /// </summary>
    property FLOAT DashOffset
    {
        FLOAT get();
    }

    /// <summary>
    /// Gets a value that describes the stroke's dash pattern.
    /// <para>(Also see DirectX SDK: ID2D1StrokeStyle::GetDashStyle)</para>
    /// </summary>
    property Direct2D1::DashStyle DashStyle
    {
        Direct2D1::DashStyle get();
    }

    /// <summary>
    /// Retrieves the number of entries in the dashes array.
    /// <para>(Also see DirectX SDK: ID2D1StrokeStyle::GetDashesCount)</para>
    /// </summary>
    property UINT32 DashesCount
    {
        UINT32 get();
    }

    /// <summary>
    /// Returns the dashes from the object into an array. 
    /// <para>(Also see DirectX SDK: ID2D1StrokeStyle::GetDashes)</para>
    /// </summary>
    property IEnumerable<FLOAT>^ Dashes
    {
        IEnumerable<FLOAT>^ get();
    }

internal:

    StrokeStyle();

    StrokeStyle(
        IUnknown *pInner
        );

};

/// <summary>
/// Represents a geometry resource and defines a set of helper methods for manipulating and measuring geometric shapes. 
/// Classes that inherit from ID2D1Geometry define specific shapes.
/// <para>(Also see DirectX SDK: ID2D1Geometry)</para>
/// </summary>
public ref class Geometry : D2DResource 
{
public:

    /// <summary>
    /// Retrieve the bounds of this geometry, with an applied transform.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::GetBounds)</para>
    /// </summary>
    /// <param name="worldTransform">The transform to apply to this geometry before calculating its bounds.</param>
    /// <returns>The bounds of this geometry. If the bounds are empty, the first value of the bounding box will be NaN.</returns>
    RectF
    GetBounds(
        Matrix3x2F worldTransform
        );

    /// <summary>
    /// Retrieve the bounds of this geometry.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::GetBounds)</para>
    /// </summary>
    /// <returns>The bounds of this geometry. If the bounds are empty, the first value of the bounding box will be NaN.</returns>
    RectF
    GetBounds(
        );

    /// <summary>
    /// Get the bounds of the corresponding geometry after it has been widened or have an optional style applied.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::GetWidenedBounds)</para>
    /// </summary>
    /// <param name="strokeWidth">The amount by which to widen the geometry by stroking its outline.</param>
    /// <param name="strokeStyle">The style of the stroke that widens the geometry.</param>
    /// <param name="worldTransform">A transform to apply to the geometry after the geometry is transformed and after the geometry has been stroked.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <returns>The bounds of the widened geometry.</returns>
    RectF
    GetWidenedBounds(
        FLOAT strokeWidth,
        StrokeStyle^ strokeStyle,
        FLOAT flatteningTolerance,
        Matrix3x2F worldTransform
        );

    /// <summary>
    /// Get the bounds of the corresponding geometry after it has been widened or have an optional style applied.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::GetWidenedBounds)</para>
    /// </summary>
    /// <param name="strokeWidth">The amount by which to widen the geometry by stroking its outline.</param>
    /// <param name="strokeStyle">The style of the stroke that widens the geometry.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <returns>The bounds of the widened geometry.</returns>
    RectF
    GetWidenedBounds(
        FLOAT strokeWidth,
        StrokeStyle^ strokeStyle,
        FLOAT flatteningTolerance
        );

    /// <summary>
    /// Get the bounds of the corresponding geometry after it has been widened or have an optional style applied.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::GetWidenedBounds)</para>
    /// </summary>
    /// <param name="strokeWidth">The amount by which to widen the geometry by stroking its outline.</param>
    /// <param name="strokeStyle">The style of the stroke that widens the geometry.</param>
    /// <returns>The bounds of the widened geometry.</returns>
    RectF
    GetWidenedBounds(
        FLOAT strokeWidth,
        StrokeStyle^ strokeStyle
        );

    /// <summary>
    /// Determines whether the geometry's stroke contains the specified point.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::StrokeContainsPoint)</para>
    /// </summary>
    /// <param name="point">The point to test for containment.</param>
    /// <param name="strokeWidth">The thickness of the stroke to apply.</param>
    /// <param name="strokeStyle">The style of stroke to apply.</param>
    /// <param name="worldTransform">The transform to apply to the stroked geometry.</param>
    /// <param name="flatteningTolerance">The numeric accuracy with which the precise geometric path and path intersection is calculated. Points missing the stroke by less than the tolerance are still considered inside. Smaller values produce more accurate results but cause slower execution.</param>
    /// <returns>True if the geometry's stroke contains the specified point; otherwise, false.</returns>
    Boolean
    StrokeContainsPoint(
        Point2F point,
        FLOAT strokeWidth,
        StrokeStyle ^ strokeStyle,
        FLOAT flatteningTolerance,
        Matrix3x2F worldTransform
        );

    /// <summary>
    /// Determines whether the geometry's stroke contains the specified point.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::StrokeContainsPoint)</para>
    /// </summary>
    /// <param name="point">The point to test for containment.</param>
    /// <param name="strokeWidth">The thickness of the stroke to apply.</param>
    /// <param name="strokeStyle">The style of stroke to apply.</param>
    /// <param name="flatteningTolerance">The numeric accuracy with which the precise geometric path and path intersection is calculated. Points missing the stroke by less than the tolerance are still considered inside. Smaller values produce more accurate results but cause slower execution.</param>
    /// <returns>True if the geometry's stroke contains the specified point; otherwise, false.</returns>
    Boolean
    StrokeContainsPoint(
        Point2F point,
        FLOAT strokeWidth,
        StrokeStyle ^ strokeStyle,
        FLOAT flatteningTolerance
        );

    /// <summary>
    /// Determines whether the geometry's stroke contains the specified point.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::StrokeContainsPoint)</para>
    /// </summary>
    /// <param name="point">The point to test for containment.</param>
    /// <param name="strokeWidth">The thickness of the stroke to apply.</param>
    /// <param name="strokeStyle">The style of stroke to apply.</param>
    /// <returns>True if the geometry's stroke contains the specified point; otherwise, false.</returns>
    Boolean
    StrokeContainsPoint(
        Point2F point,
        FLOAT strokeWidth,
        StrokeStyle ^ strokeStyle
        );

    /// <summary>
    /// Indicates whether the area filled by the geometry would contain the specified point.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::FillContainsPoint)</para>
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <param name="worldTransform">The numeric accuracy with which the precise geometric path and path intersection is calculated. Points missing the fill by less than the tolerance are still considered inside. Smaller values produce more accurate results but cause slower execution.</param>
    /// <param name="flatteningTolerance">The flatteningTolerance parameter.</param>
    /// <returns>True if the area filled by the geometry contains point; otherwise, false.</returns>
    Boolean
    FillContainsPoint(
        Point2F point,
        FLOAT flatteningTolerance,
        Matrix3x2F worldTransform
        );

    /// <summary>
    /// Indicates whether the area filled by the geometry would contain the specified point.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::FillContainsPoint)</para>
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <param name="flatteningTolerance">The flatteningTolerance parameter.</param>
    /// <returns>True if the area filled by the geometry contains point; otherwise, false.</returns>
    Boolean
    FillContainsPoint(
        Point2F point,
        FLOAT flatteningTolerance
        );

    /// <summary>
    /// Indicates whether the area filled by the geometry would contain the specified point.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::FillContainsPoint)</para>
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the area filled by the geometry contains point; otherwise, false.</returns>
    Boolean
    FillContainsPoint(
        Point2F point
        );

    /// <summary>
    /// Compare how one geometry intersects or contains another geometry.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::CompareWithGeometry)</para>
    /// </summary>
    /// <param name="inputGeometry">The geometry to test.</param>
    /// <param name="inputGeometryTransform">The transform to apply to inputGeometry.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometries. Smaller values produce more accurate results but cause slower execution.</param>
    /// <returns>GeometryRelation enumeration that that describes how this geometry is related to inputGeometry.</returns>
    GeometryRelation
    CompareWithGeometry(
        Geometry ^ inputGeometry,
        FLOAT flatteningTolerance,
        Matrix3x2F inputGeometryTransform
        );

    /// <summary>
    /// Compare how one geometry intersects or contains another geometry.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::CompareWithGeometry)</para>
    /// </summary>
    /// <param name="inputGeometry">The geometry to test.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometries. Smaller values produce more accurate results but cause slower execution.</param>
    /// <returns>GeometryRelation enumeration that that describes how this geometry is related to inputGeometry.</returns>
    GeometryRelation
    CompareWithGeometry(
        Geometry ^ inputGeometry,
        FLOAT flatteningTolerance
        );

    /// <summary>
    /// Compare how one geometry intersects or contains another geometry.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::CompareWithGeometry)</para>
    /// </summary>
    /// <param name="inputGeometry">The geometry to test.</param>
    /// <returns>GeometryRelation enumeration that that describes how this geometry is related to inputGeometry.</returns>
    GeometryRelation
    CompareWithGeometry(
        Geometry ^ inputGeometry
        );

    /// <summary>
    /// Computes the area of the geometry after it has been transformed by the specified matrix and flattened using the default tolerance.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::ComputeArea)</para>
    /// </summary>
    /// <param name="worldTransform">The transform to apply to this geometry before computing its area.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <returns>The area of the transformed, flattened version of this geometry.</returns>
    FLOAT
    ComputeArea(
        FLOAT flatteningTolerance,
        Matrix3x2F worldTransform
        );

    /// <summary>
    /// Computes the area of the geometry.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::ComputeArea)</para>
    /// </summary>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <returns>The area of the transformed, flattened version of this geometry.</returns>
    FLOAT
    ComputeArea(
        FLOAT flatteningTolerance
        );

    /// <summary>
    /// Computes the area of the geometry.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::ComputeArea)</para>
    /// </summary>
    /// <returns>The area of the transformed, flattened version of this geometry.</returns>
    FLOAT
    ComputeArea(
        );

    /// <summary>
    /// Calculates the length of the geometry as though each segment were unrolled into a line.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::ComputeLength)</para>
    /// </summary>
    /// <param name="worldTransform">The transform to apply to the geometry before calculating its length.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <returns>The length of the geometry. For closed geometries, the length includes an implicit closing segment.</returns>
    FLOAT
    ComputeLength(
        FLOAT flatteningTolerance,
        Matrix3x2F worldTransform
        );

    /// <summary>
    /// Calculates the length of the geometry as though each segment were unrolled into a line.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::ComputeLength)</para>
    /// </summary>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <returns>The length of the geometry. For closed geometries, the length includes an implicit closing segment.</returns>
    FLOAT
    ComputeLength(
        FLOAT flatteningTolerance
        );

    /// <summary>
    /// Calculates the length of the geometry as though each segment were unrolled into a line.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::ComputeLength)</para>
    /// </summary>
    /// <returns>The length of the geometry. For closed geometries, the length includes an implicit closing segment.</returns>
    FLOAT
    ComputeLength(
        );

    /// <summary>
    /// Calculates the point and tangent vector at the specified distance along the geometry after it has been transformed by the specified matrix and flattened using the default tolerance.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::ComputePointAtLength)</para>
    /// </summary>
    /// <param name="length">The distance along the geometry of the point and tangent to find. If this distance is less then 0, this method calculates the first point in the geometry. If this distance is greater than the length of the geometry, this method calculates the last point in the geometry.</param>
    /// <param name="worldTransform">The transform to apply to the geometry before calculating the specified point and tangent.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <returns>The location and tangent at the specified distance along the geometry. If the geometry is empty, these contain NaN as their x and y values.</returns>
    PointAndTangent
    ComputePointAtLength(
        FLOAT length,
        FLOAT flatteningTolerance,
        Matrix3x2F worldTransform
        );

    /// <summary>
    /// Calculates the point and tangent vector at the specified distance along the geometry after it has been transformed by the specified matrix and flattened using the default tolerance.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::ComputePointAtLength)</para>
    /// </summary>
    /// <param name="length">The distance along the geometry of the point and tangent to find. If this distance is less then 0, this method calculates the first point in the geometry. If this distance is greater than the length of the geometry, this method calculates the last point in the geometry.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <returns>The location and tangent at the specified distance along the geometry. If the geometry is empty, these contain NaN as their x and y values.</returns>
    PointAndTangent ComputePointAtLength(FLOAT length, FLOAT flatteningTolerance);

    /// <summary>
    /// Calculates the point and tangent vector at the specified distance along the geometry after it has been transformed by the specified matrix and flattened using the default tolerance.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::ComputePointAtLength)</para>
    /// </summary>
    /// <param name="length">The distance along the geometry of the point and tangent to find. If this distance is less then 0, this method calculates the first point in the geometry. If this distance is greater than the length of the geometry, this method calculates the last point in the geometry.</param>
    /// <returns>The location and tangent at the specified distance along the geometry. If the geometry is empty, these contain NaN as their x and y values.</returns>
    PointAndTangent ComputePointAtLength(FLOAT length);

    /// <summary>
    /// Creates a simplified version of the geometry that contains only lines and (optionally) cubic Bezier curves and writes the result to an <see cref="ISimplifiedGeometrySink"/> object.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::Simplify)</para>
    /// </summary>
    /// <param name="simplificationOption">A value that specifies whether the simplified geometry should contain curves.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <param name="geometrySink">The geometry sink to which the simplified geometry is appended.</param>
    /// <param name="worldTransform">The transform to apply to the simplified geometry.</param>
    void
    Simplify(
        GeometrySimplificationOption simplificationOption,
        ISimplifiedGeometrySink ^ geometrySink,
        FLOAT flatteningTolerance,
        Matrix3x2F worldTransform
        );

    /// <summary>
    /// Creates a simplified version of the geometry that contains only lines and (optionally) cubic Bezier curves and writes the result to an <see cref="ISimplifiedGeometrySink"/> object.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::Simplify)</para>
    /// </summary>
    /// <param name="simplificationOption">A value that specifies whether the simplified geometry should contain curves.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <param name="geometrySink">The geometry sink to which the simplified geometry is appended.</param>
    void
    Simplify(
        GeometrySimplificationOption simplificationOption,
        ISimplifiedGeometrySink ^ geometrySink,
        FLOAT flatteningTolerance
        );

    /// <summary>
    /// Creates a simplified version of the geometry that contains only lines and (optionally) cubic Bezier curves and writes the result to an <see cref="ISimplifiedGeometrySink"/> object.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::Simplify)</para>
    /// </summary>
    /// <param name="simplificationOption">A value that specifies whether the simplified geometry should contain curves.</param>
    /// <param name="geometrySink">The geometry sink to which the simplified geometry is appended.</param>
    void
    Simplify(
        GeometrySimplificationOption simplificationOption,
        ISimplifiedGeometrySink ^ geometrySink
        );

    /// <summary>
    /// Combines this geometry with the specified geometry and stores the result in an <see cref="ISimplifiedGeometrySink"/>.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::CombineWithGeometry)</para>
    /// </summary>
    /// <param name="inputGeometry">The geometry to combine with this instance.</param>
    /// <param name="combineMode">The type of combine operation to perform.</param>
    /// <param name="geometrySink">The result of the combine operation.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometries. Smaller values produce more accurate results but cause slower execution.</param>
    /// <param name="inputGeometryTransform">The transform to apply to inputGeometry before combining.</param>
    void
    CombineWithGeometry(
        Geometry ^ inputGeometry,
        CombineMode combineMode,
        ISimplifiedGeometrySink^ geometrySink,
        FLOAT flatteningTolerance,
        Matrix3x2F inputGeometryTransform
        );

    /// <summary>
    /// Combines this geometry with the specified geometry and stores the result in an <see cref="ISimplifiedGeometrySink"/>.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::CombineWithGeometry)</para>
    /// </summary>
    /// <param name="inputGeometry">The geometry to combine with this instance.</param>
    /// <param name="combineMode">The type of combine operation to perform.</param>
    /// <param name="geometrySink">The result of the combine operation.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometries. Smaller values produce more accurate results but cause slower execution.</param>
    void
    CombineWithGeometry(
        Geometry ^ inputGeometry,
        CombineMode combineMode,
        ISimplifiedGeometrySink ^ geometrySink,
        FLOAT flatteningTolerance
        );

    /// <summary>
    /// Combines this geometry with the specified geometry and stores the result in an <see cref="ISimplifiedGeometrySink"/>.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::CombineWithGeometry)</para>
    /// </summary>
    /// <param name="inputGeometry">The geometry to combine with this instance.</param>
    /// <param name="combineMode">The type of combine operation to perform.</param>
    /// <param name="geometrySink">The result of the combine operation.</param>
    void
    CombineWithGeometry(
        Geometry ^ inputGeometry,
        CombineMode combineMode,
        ISimplifiedGeometrySink ^ geometrySink
        );

    /// <summary>
    /// Computes the outline of the geometry and writes the result to an <see cref="ISimplifiedGeometrySink"/>.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::Outline)</para>
    /// </summary>
    /// <param name="geometrySink">The <see cref="ISimplifiedGeometrySink"/> to which the geometry's transformed outline is appended.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <param name="worldTransform">The transform to apply to the geometry outline.</param>
    void
    Outline(
        ISimplifiedGeometrySink ^ geometrySink,
        FLOAT flatteningTolerance,
        Matrix3x2F worldTransform
        );

    /// <summary>
    /// Computes the outline of the geometry and writes the result to an <see cref="ISimplifiedGeometrySink"/>.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::Outline)</para>
    /// </summary>
    /// <param name="geometrySink">The <see cref="ISimplifiedGeometrySink"/> to which the geometry's transformed outline is appended.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    void
    Outline(
        ISimplifiedGeometrySink ^ geometrySink,
        FLOAT flatteningTolerance
        );

    /// <summary>
    /// Computes the outline of the geometry and writes the result to an <see cref="ISimplifiedGeometrySink"/>.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::Outline)</para>
    /// </summary>
    /// <param name="geometrySink">The <see cref="ISimplifiedGeometrySink"/> to which the geometry's transformed outline is appended.</param>
    void
    Outline(
        ISimplifiedGeometrySink ^ geometrySink
        );

    /// <summary>
    /// Creates a set of clockwise-wound triangles that cover the geometry after it has been transformed 
    /// using the specified matrix and flattened using the specified tolerance.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::Tessellate)</para>
    /// </summary>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <param name="tessellationSink">The <see cref="ITessellationSink"/> to which the tessellated is appended.</param>
    /// <param name="worldTransform">The transform to apply to this geometry.</param>
    void
    Tessellate(
        ITessellationSink ^ tessellationSink,
        FLOAT flatteningTolerance,
        Matrix3x2F worldTransform
        );

    /// <summary>
    /// Creates a set of clockwise-wound triangles that cover the geometry after it has been transformed 
    /// using the specified matrix and flattened using the specified tolerance.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::Tessellate)</para>
    /// </summary>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <param name="tessellationSink">The <see cref="ITessellationSink"/> to which the tessellated is appended.</param>
    void
    Tessellate(
        ITessellationSink ^ tessellationSink,
        FLOAT flatteningTolerance
        );

    /// <summary>
    /// Creates a set of clockwise-wound triangles that cover the geometry after it has been transformed 
    /// using the specified matrix and flattened using the specified tolerance.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::Tessellate)</para>
    /// </summary>
    /// <param name="tessellationSink">The <see cref="ITessellationSink"/> to which the tessellated is appended.</param>
    void
    Tessellate(
        ITessellationSink ^ tessellationSink
        );

    /// <summary>
    /// Widens the geometry by the specified stroke and writes the result to an <see cref="ISimplifiedGeometrySink"/>.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::Widen)</para>
    /// </summary>
    /// <param name="strokeWidth">The amount by which to widen the geometry.</param>
    /// <param name="strokeStyle">The style of stroke to apply to the geometry. Leave null for default stroke style.</param>
    /// <param name="geometrySink">The <see cref="ISimplifiedGeometrySink"/> to which the widened geometry is appended.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    /// <param name="worldTransform">The transform to apply to the geometry after widening it.</param>
    void
    Widen(
        FLOAT strokeWidth,
        StrokeStyle ^ strokeStyle,
        ISimplifiedGeometrySink ^ geometrySink,
        FLOAT flatteningTolerance,
        Matrix3x2F worldTransform
        );

    /// <summary>
    /// Widens the geometry by the specified stroke and writes the result to an <see cref="ISimplifiedGeometrySink"/>.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::Widen)</para>
    /// </summary>
    /// <param name="strokeWidth">The amount by which to widen the geometry.</param>
    /// <param name="strokeStyle">The style of stroke to apply to the geometry. Leave null for default stroke style.</param>
    /// <param name="geometrySink">The <see cref="ISimplifiedGeometrySink"/> to which the widened geometry is appended.</param>
    /// <param name="flatteningTolerance">The maximum bounds on the distance between points in the polygonal approximation of the geometry. Smaller values produce more accurate results but cause slower execution.</param>
    void
    Widen(
        FLOAT strokeWidth,
        StrokeStyle ^ strokeStyle,
        ISimplifiedGeometrySink ^ geometrySink,
        FLOAT flatteningTolerance
        );

    /// <summary>
    /// Widens the geometry by the specified stroke and writes the result to an <see cref="ISimplifiedGeometrySink"/>.
    /// <para>(Also see DirectX SDK: ID2D1Geometry::Widen)</para>
    /// </summary>
    /// <param name="strokeWidth">The amount by which to widen the geometry.</param>
    /// <param name="strokeStyle">The style of stroke to apply to the geometry. Leave null for default stroke style.</param>
    /// <param name="geometrySink">The <see cref="ISimplifiedGeometrySink"/> to which the widened geometry is appended.</param>
    void
    Widen(
        FLOAT strokeWidth,
        StrokeStyle ^ strokeStyle,
        ISimplifiedGeometrySink ^ geometrySink
        );

internal:

    Geometry();

    Geometry(
        IUnknown *pInner
        );

};

/// <summary>
/// Describes a two-dimensional rectangle. 
/// <para>(Also see DirectX SDK: ID2D1RectangleGeometry)</para>
/// </summary>
public ref class RectangleGeometry : Geometry 
{
public:

    /// <summary>
    /// Retrieves the rectangle that describes the rectangle geometry's dimensions.
    /// <para>(Also see DirectX SDK: ID2D1RectangleGeometry::GetRect)</para>
    /// </summary>
    property RectF Rectangle
    {
        RectF get();
    }

internal:

    RectangleGeometry();

    RectangleGeometry(
        IUnknown *pInner
        );

};

/// <summary>
/// Describes a rounded rectangle.
/// <para>(Also see DirectX SDK: ID2D1RoundedRectangleGeometry)</para>
/// </summary>
public ref class RoundedRectangleGeometry : Geometry 
{
public:

    /// <summary>
    /// Retrieves a rounded rectangle that describes this rounded rectangle geometry.
    /// <para>(Also see DirectX SDK: ID2D1RoundedRectangleGeometry::GetRoundedRect)</para>
    /// </summary>
    property RoundedRect RoundedRectangle
    {
        RoundedRect get();
    }

internal:

    RoundedRectangleGeometry();

    RoundedRectangleGeometry(
        IUnknown *pInner
        );

};

/// <summary>
/// Represents an ellipse. 
/// <para>(Also see DirectX SDK: ID2D1EllipseGeometry)</para>
/// </summary>
public ref class EllipseGeometry : Geometry 
{
public:

    /// <summary>
    /// Gets the <see cref="Ellipse"/> structure that describes this ellipse geometry.
    /// <para>(Also see DirectX SDK: ID2D1EllipseGeometry::GetEllipse)</para>
    /// </summary>
    property Direct2D1::Ellipse Ellipse
    {
        Direct2D1::Ellipse get();
    }

internal:
    EllipseGeometry();

    EllipseGeometry(
        IUnknown *pInner
        );

};

/// <summary>
/// Describes a geometric path that does not contain quadratic bezier curves or arcs. 
/// Implement this interface when you want to read back the complete contents of a geometry. 
/// For other scenarios, use the implementation provided by Direct2D: <see cref="SimplifiedGeometrySink"/>
/// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink)</para>
/// </summary>
public interface class ISimplifiedGeometrySink 
{
public:
    /// <summary>
    /// Specifies the method used to determine which points are inside the geometry described by this geometry sink and which points are outside.
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::SetFillMode)</para>
    /// </summary>
    /// <param name="fillMode">The method used to determine whether a given point is part of the geometry.</param>
    virtual
    void
    SetFillMode(
        FillMode fillMode
        );

    /// <summary>
    /// Specifies stroke and join options to be applied to new segments added to the geometry sink.
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::SetSegmentFlags)</para>
    /// </summary>
    /// <param name="vertexOptions">Stroke and join options to be applied to new segments added to the geometry sink.</param>
    CA_SUPPRESS_MESSAGE("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId="Flags", Justification = "Interface is declared to match naming from unmanaged interface")
	virtual
    void
    SetSegmentFlags(
        PathSegmentOptions vertexOptions
        );

    /// <summary>
    /// Starts a new figure at the specified point.
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::BeginFigure)</para>
    /// </summary>
    /// <param name="startPoint">The point at which to begin the new figure.</param>
    /// <param name="figureBegin">Whether the new figure should be hollow or filled.</param>
    virtual
    void
    BeginFigure(
        Point2F startPoint,
        FigureBegin figureBegin
        );

    /// <summary>
    /// Creates a sequence of lines using the specified points and adds them to the geometry sink.
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::AddLines)</para>
    /// </summary>
    /// <param name="points">A collection of one or more points that describe the lines to draw. 
    /// A line is drawn from the geometry sink's current point 
    /// (the end point of the last segment drawn or the location specified by BeginFigure) to the first point in the array. if the array contains additional points, a line is drawn from the first point to the second point in the array, from the second point to the third point, and so on. 
    /// </param>
    virtual
    void
    AddLines(
        IEnumerable<Point2F> ^ points
        );

    /// <summary>
    /// Creates a sequence of cubic Bezier curves and adds them to the geometry sink. 
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::AddBeziers)</para>
    /// </summary>
    /// <param name="beziers">A collection of of Bezier segments that describes the Bezier curves to create. 
    /// A curve is drawn from the geometry sink's current point (the end point of the last segment drawn or 
    /// the location specified by BeginFigure) to the end point of the first Bezier segment in the array. 
    /// If the collection contains additional Bezier segments, each subsequent Bezier segment uses the end point 
    /// of the preceding Bezier segment as its start point.</param>
    virtual
    void
    AddBeziers(
        IEnumerable<BezierSegment> ^ beziers
        );

    /// <summary>
    /// Ends the current figure; optionally, closes it.
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::EndFigure)</para>
    /// </summary>
    /// <param name="figureEnd">A value that indicates whether the current figure is closed. If the figure is closed, a line is drawn between the current point and the start point specified by BeginFigure.</param>
    virtual
    void
    EndFigure(
        FigureEnd figureEnd
        );

    /// <summary>
    /// Closes the geometry sink, throwing an exception if is in an error state, and resets the sink's error state. 
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::Close)</para>
    /// </summary>
    virtual
    void
    Close(
        );
};

/// <summary>
/// Describes a geometric path that does not contain quadratic bezier curves or arcs. 
/// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink)</para>
/// </summary>
public ref class SimplifiedGeometrySink : DirectUnknown, ISimplifiedGeometrySink
{
public:
    /// <summary>
    /// Specifies the method used to determine which points are inside the geometry described by this geometry sink and which points are outside.
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::SetFillMode)</para>
    /// </summary>
    /// <param name="fillMode">The method used to determine whether a given point is part of the geometry.</param>
    virtual
    void
    SetFillMode(
        FillMode fillMode
        );

    /// <summary>
    /// Specifies stroke and join options to be applied to new segments added to the geometry sink.
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::SetSegmentFlags)</para>
    /// </summary>
    /// <param name="vertexOptions">Stroke and join options to be applied to new segments added to the geometry sink.</param>
    virtual
    void
    SetSegmentFlags(
        PathSegmentOptions vertexOptions
        );

    /// <summary>
    /// Starts a new figure at the specified point.
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::BeginFigure)</para>
    /// </summary>
    /// <param name="startPoint">The point at which to begin the new figure.</param>
    /// <param name="figureBegin">Whether the new figure should be hollow or filled.</param>
    virtual
    void
    BeginFigure(
        Point2F startPoint,
        FigureBegin figureBegin
        );

    /// <summary>
    /// Creates a sequence of lines using the specified points and adds them to the geometry sink.
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::AddLines)</para>
    /// </summary>
    /// <param name="points">A collection of one or more points that describe the lines to draw. 
    /// A line is drawn from the geometry sink's current point 
    /// (the end point of the last segment drawn or the location specified by BeginFigure) to the first point in the array. if the array contains additional points, a line is drawn from the first point to the second point in the array, from the second point to the third point, and so on. 
    /// </param>
    virtual
    void
    AddLines(
        IEnumerable<Point2F> ^ points
        );

    /// <summary>
    /// Creates a sequence of cubic Bezier curves and adds them to the geometry sink. 
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::AddBeziers)</para>
    /// </summary>
    /// <param name="beziers">A collection of of Bezier segments that describes the Bezier curves to create. 
    /// A curve is drawn from the geometry sink's current point (the end point of the last segment drawn or 
    /// the location specified by BeginFigure) to the end point of the first Bezier segment in the array. 
    /// If the collection contains additional Bezier segments, each subsequent Bezier segment uses the end point 
    /// of the preceding Bezier segment as its start point.</param>
    virtual
    void
    AddBeziers(
        IEnumerable<BezierSegment> ^ beziers
        );

    /// <summary>
    /// Ends the current figure; optionally, closes it.
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::EndFigure)</para>
    /// </summary>
    /// <param name="figureEnd">A value that indicates whether the current figure is closed. If the figure is closed, a line is drawn between the current point and the start point specified by BeginFigure.</param>
    virtual
    void
    EndFigure(
        FigureEnd figureEnd
        );

    /// <summary>
    /// Closes the geometry sink, throwing an exception if is in an error state, and resets the sink's error state. 
    /// <para>(Also see DirectX SDK: ID2D1SimplifiedGeometrySink::Close)</para>
    /// </summary>
    virtual
    void
    Close(
        );

internal:

    SimplifiedGeometrySink();

    SimplifiedGeometrySink(
        IUnknown *pInner
        );

};

/// <summary>
/// Describes a geometric path that can contain lines, arcs, cubic Bezier curves, and quadratic Bezier curves. 
/// Implement this interface when you want to read back the complete contents of a geometry. 
/// For other scenarios, use the implementation provided by Direct2D: <see cref="GeometrySink"/>
/// <para>(Also see DirectX SDK: ID2D1GeometrySink)</para>
/// </summary>
public interface class IGeometrySink : ISimplifiedGeometrySink
{
public:
    /// <summary>
    /// Creates a line segment between the current point and the specified end point and adds it to the geometry sink. 
    /// <para>(Also see DirectX SDK: ID2D1GeometrySink::AddLine)</para>
    /// </summary>
    /// <param name="point">The end point of the line to draw.</param>
    virtual
    void
    AddLine(
        Point2F point
        );

    /// <summary>
    /// Creates a cubic Bezier curve between the current point and the specified end point and adds it to the geometry sink.
    /// <para>(Also see DirectX SDK: ID2D1GeometrySink::AddBezier)</para>
    /// </summary>
    /// <param name="bezier">The Bezier curve to add.</param>
    virtual
    void
    AddBezier(
        BezierSegment bezier
        );

    /// <summary>
    /// Creates a quadratic Bezier curve between the current point and the specified end point and adds it to the geometry sink.
    /// <para>(Also see DirectX SDK: ID2D1GeometrySink::AddQuadraticBezier)</para>
    /// </summary>
    /// <param name="bezier">The quadratic Bezier curve to add.</param>
    virtual
    void
    AddQuadraticBezier(
        QuadraticBezierSegment bezier
        );

    /// <summary>
    /// Adds a sequence of quadratic Bezier segments as a collection in a single call.
    /// <para>(Also see DirectX SDK: ID2D1GeometrySink::AddQuadraticBeziers)</para>
    /// </summary>
    /// <param name="beziers">A collection of a sequence of quadratic Bezier segments.</param>
    virtual
    void
    AddQuadraticBeziers(
        IEnumerable<QuadraticBezierSegment> ^ beziers
        );

    /// <summary>
    /// Creates a single arc and adds it to the path geometry.
    /// <para>(Also see DirectX SDK: ID2D1GeometrySink::AddArc)</para>
    /// </summary>
    /// <param name="arc">The arc to add.</param>
    virtual
    void
    AddArc(
        ArcSegment arc
        );
};

/// <summary>
/// Describes a geometric path that can contain lines, arcs, cubic Bezier curves, and quadratic Bezier curves. 
/// <para>(Also see DirectX SDK: ID2D1GeometrySink)</para>
/// </summary>
public ref class GeometrySink : SimplifiedGeometrySink, IGeometrySink
{
public:

    /// <summary>
    /// Creates a line segment between the current point and the specified end point and adds it to the geometry sink. 
    /// <para>(Also see DirectX SDK: ID2D1GeometrySink::AddLine)</para>
    /// </summary>
    /// <param name="point">The end point of the line to draw.</param>
    virtual
    void
    AddLine(
        Point2F point
        );

    /// <summary>
    /// Creates a cubic Bezier curve between the current point and the specified end point and adds it to the geometry sink.
    /// <para>(Also see DirectX SDK: ID2D1GeometrySink::AddBezier)</para>
    /// </summary>
    /// <param name="bezier">The Bezier curve to add.</param>
    virtual
    void
    AddBezier(
        BezierSegment bezier
        );

    /// <summary>
    /// Creates a quadratic Bezier curve between the current point and the specified end point and adds it to the geometry sink.
    /// <para>(Also see DirectX SDK: ID2D1GeometrySink::AddQuadraticBezier)</para>
    /// </summary>
    /// <param name="bezier">The quadratic Bezier curve to add.</param>
    virtual
    void
    AddQuadraticBezier(
        QuadraticBezierSegment bezier
        );

    /// <summary>
    /// Adds a sequence of quadratic Bezier segments as a collection in a single call.
    /// <para>(Also see DirectX SDK: ID2D1GeometrySink::AddQuadraticBeziers)</para>
    /// </summary>
    /// <param name="beziers">A collection of a sequence of quadratic Bezier segments.</param>
    virtual
    void
    AddQuadraticBeziers(
        IEnumerable<QuadraticBezierSegment> ^ beziers
        );

    /// <summary>
    /// Creates a single arc and adds it to the path geometry.
    /// <para>(Also see DirectX SDK: ID2D1GeometrySink::AddArc)</para>
    /// </summary>
    /// <param name="arc">The arc to add.</param>
    virtual
    void
    AddArc(
        ArcSegment arc
        );

internal:

GeometrySink();

GeometrySink(
    IUnknown *pInner
    );

};

/// <summary>
/// Populates a <see cref="Mesh"/> object with triangles. 
/// Implement this interface when you want to read back the results of the Tessellate method. 
/// For other scenarios, use the implementation provided by Direct2D: <see cref="TessellationSink"/>
/// <para>(Also see DirectX SDK: ID2D1TessellationSink)</para>
/// </summary>
public interface class ITessellationSink 
{
public:
    /// <summary>
    /// Copies the specified triangles to the sink.
    /// <para>(Also see DirectX SDK: ID2D1TessellationSink::AddTriangles)</para>
    /// </summary>
    /// <param name="triangles">The triangle collection to add to sink.</param>
    virtual
    void
    AddTriangles(
        IEnumerable<Triangle> ^ triangles
        );

    /// <summary>
    /// Closes the sink.
    /// <para>(Also see DirectX SDK: ID2D1TessellationSink::Close)</para>
    /// </summary>
    virtual
    void
    Close(
        );

};

/// <summary>
/// Populates a <see cref="Mesh"/> object with triangles. 
/// <para>(Also see DirectX SDK: ID2D1TessellationSink)</para>
/// </summary>
public ref class TessellationSink : DirectUnknown, ITessellationSink
{
public:
    /// <summary>
    /// Copies the specified triangles to the sink.
    /// <para>(Also see DirectX SDK: ID2D1TessellationSink::AddTriangles)</para>
    /// </summary>
    /// <param name="triangles">The triangle collection to add to sink.</param>
    virtual
    void
    AddTriangles(
        IEnumerable<Triangle> ^ triangles
        );

    /// <summary>
    /// Closes the sink.
    /// <para>(Also see DirectX SDK: ID2D1TessellationSink::Close)</para>
    /// </summary>
    virtual
    void
    Close(
        );

internal:

    TessellationSink();

    TessellationSink(
        IUnknown *pInner
        );
};

/// <summary>
/// Represents a complex shape that may be composed of arcs, curves, and lines. 
/// <para>(Also see DirectX SDK: ID2D1PathGeometry)</para>
/// </summary>
public ref class PathGeometry : Geometry 
{
public:

    /// <summary>
    /// Opens a geometry sink that will be used to create this path geometry.
    /// <para>(Also see DirectX SDK: ID2D1PathGeometry::Open)</para>
    /// </summary>
    /// <returns>Returns the geometry sink that is used to populate the path geometry with figures and segments.</returns>
    /// <remarks>
    /// Because path geometries are immutable and can only be populated once, it is an error to call Open on a path geometry more than once.
    /// <para>Note that the fill mode defaults to Alternate. To set the fill mode, call SetFillMode before the first call to BeginFigure. Failure to do so will put the geometry sink in an error state.</para> 
    /// </remarks>
    GeometrySink ^
    Open(
        );

    /// <summary>
    /// The number of segments in the path geometry.
    /// <para>(Also see DirectX SDK: ID2D1PathGeometry::GetSegmentCount)</para>
    /// </summary>
    property UINT32 SegmentCount
    {
        UINT32 get();
    }

    /// <summary>
    /// The number of figures in the path geometry. 
    /// <para>(Also see DirectX SDK: ID2D1PathGeometry::GetFigureCount)</para>
    /// </summary>
    property UINT32 FigureCount
    {
        UINT32 get();
    }


    /// <summary>
    /// Copies the contents of the path geometry to the specified <see cref="IGeometrySink"/>.
    /// <para>(Also see DirectX SDK: ID2D1PathGeometry::Stream)</para>
    /// </summary>
    /// <param name="geometrySink">The sink to which the path geometry's contents are copied. Modifying this sink does not change the contents of this path geometry.</param>
    void
    Stream(
        IGeometrySink ^ geometrySink
        );
internal:

    PathGeometry();

    PathGeometry(
        IUnknown *pInner
        );

};

/// <summary>
/// Represents a composite geometry, composed of other <see cref="Geometry"/> objects. 
/// <para>(Also see DirectX SDK: ID2D1GeometryGroup)</para>
/// </summary>
public ref class GeometryGroup : Geometry 
{
public:

    /// <summary>
    /// Indicates how the intersecting areas of the geometries contained in this geometry group are combined.
    /// <para>(Also see DirectX SDK: ID2D1GeometryGroup::GetFillMode)</para>
    /// </summary>
    property Direct2D1::FillMode FillMode
    {
        Direct2D1::FillMode get();
    }

    /// <summary>
    /// Indicates the number of geometry objects in the geometry group.
    /// <para>(Also see DirectX SDK: ID2D1GeometryGroup::GetSourceGeometryCount)</para>
    /// </summary>
    property UINT32 SourceGeometryCount
    {
        UINT32 get();
    }

    /// <summary>
    /// Retrieves the geometries in the geometry group.
    /// <para>(Also see DirectX SDK: ID2D1GeometryGroup::GetSourceGeometries)</para>
    /// </summary>
    /// <returns>A ReadOnlyCollection of <see cref="Geometry"/> objects.</returns>
    property ReadOnlyCollection<Geometry^>^ Geometries
    {
        ReadOnlyCollection<Geometry^>^ get(void);
    }

internal:

    GeometryGroup();

    GeometryGroup(
        IUnknown *pInner
        );

};

/// <summary>
/// Represents a geometry that has been transformed.
/// <para>(Also see DirectX SDK: ID2D1TransformedGeometry)</para>
/// </summary>
public ref class TransformedGeometry : Geometry 
{
public:

    /// <summary>
    /// Retrieves the source <see cref="Geometry"/> of this transformed geometry object. 
    /// <para>(Also see DirectX SDK: ID2D1TransformedGeometry::GetSourceGeometry)</para>
    /// </summary>
    property Geometry^ Source
    {
        Geometry^ get(void);
    }

    /// <summary>
    /// The matrix used to transform the <see cref="TransformedGeometry" /> object's source geometry. 
    /// <para>(Also see DirectX SDK: ID2D1TransformedGeometry::GetTransform)</para>
    /// </summary>
    property Matrix3x2F Transform
    {
        Matrix3x2F get();
    }

internal:

    TransformedGeometry();

    TransformedGeometry(
        IUnknown *pInner
        );

};

/// <summary>
/// Represents a set of vertices that form a list of triangles. 
/// <para>(Also see DirectX SDK: ID2D1Mesh)</para>
/// </summary>
public ref class Mesh : D2DResource 
{
public:

    /// <summary>
    /// Opens the mesh for population.
    /// <para>(Also see DirectX SDK: ID2D1Mesh::Open)</para>
    /// </summary>
    /// <returns>TessellationSink</returns>
    TessellationSink ^
    Open(
        );

internal:

    Mesh();

    Mesh(
        IUnknown *pInner
        );

};

/// <summary>
/// Represents the backing store required to render a layer. 
/// <para>(Also see DirectX SDK: ID2D1Layer)</para>
/// </summary>
public ref class Layer : D2DResource 
{
public:

    /// <summary>
    /// The size of the layer in device-independent pixels.
    /// <para>(Also see DirectX SDK: ID2D1Layer::GetSize)</para>
    /// </summary>
    property SizeF Size
    {
        SizeF get();
    }

internal:

    Layer();

    Layer(
        IUnknown *pInner
        );

    };

/// <summary>
/// Creates Direct2D resources.
/// <para>(Also see DirectX SDK: ID2D1Factory)</para>
/// </summary>
public ref class D2DFactory : DirectUnknown 
{
public:

    /// <summary>
    /// Cause the factory to refresh any system metrics that it might have been snapped on
    /// factory creation.
    /// <para>(Also see DirectX SDK: ID2D1Factory::ReloadSystemMetrics)</para>
    /// </summary>
    void
    ReloadSystemMetrics(
        );

    /// <summary>
    /// The current desktop DPI. To refresh this, call ReloadSystemMetrics.
    /// <para>(Also see DirectX SDK: ID2D1Factory::GetDesktopDpi)</para>
    /// </summary>
    property DpiF DesktopDpi
    {
        DpiF get();
    }

    /// <summary>
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateRectangleGeometry)</para>
    /// </summary>
    /// <param name="rectangle">The rectangle parameter.</param>
    /// <returns>RectangleGeometry</returns>
    RectangleGeometry ^
    CreateRectangleGeometry(
        RectF rectangle
        );

    /// <summary>
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateRoundedRectangleGeometry)</para>
    /// </summary>
    /// <param name="roundedRectangle">The roundedRectangle parameter.</param>
    /// <returns>RoundedRectangleGeometry</returns>
    RoundedRectangleGeometry ^
    CreateRoundedRectangleGeometry(
        RoundedRect roundedRectangle
        );

    /// <summary>
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateEllipseGeometry)</para>
    /// </summary>
    /// <param name="ellipse">The ellipse parameter.</param>
    /// <returns>EllipseGeometry</returns>
    EllipseGeometry ^
    CreateEllipseGeometry(
        Ellipse ellipse
        );

    /// <summary>
    /// Create a geometry which holds other geometries.
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateGeometryGroup)</para>
    /// </summary>
    /// <param name="fillMode">The fillMode parameter.</param>
    /// <param name="geometries">The geometries parameter.</param>
    /// <returns>GeometryGroup</returns>
    GeometryGroup ^
    CreateGeometryGroup(
        FillMode fillMode,
        IEnumerable<Geometry ^> ^ geometries
        );

    /// <summary>
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateTransformedGeometry)</para>
    /// </summary>
    /// <param name="sourceGeometry">The sourceGeometry parameter.</param>
    /// <param name="transform">The transform parameter.</param>
    /// <returns>TransformedGeometry</returns>
    TransformedGeometry ^
    CreateTransformedGeometry(
        Geometry ^ sourceGeometry,
        Matrix3x2F transform
        );

    /// <summary>
    /// Returns an initially empty path geometry interface. A geometry sink is created off
    /// the interface to populate it.
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreatePathGeometry)</para>
    /// </summary>
    /// <returns>PathGeometry</returns>
    PathGeometry ^
    CreatePathGeometry(
        );

    /// <summary>
    /// Allows a non-default stroke style to be specified for a given geometry at draw time.
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateStrokeStyle)</para>
    /// </summary>
    /// <param name="strokeStyleProperties">The strokeStyleProperties parameter.</param>
    /// <param name="dashes">The dashes parameter.</param>
    /// <returns>StrokeStyle</returns>
    StrokeStyle ^
    CreateStrokeStyle(
        StrokeStyleProperties strokeStyleProperties,
        cli::array<FLOAT> ^ dashes
        );

    /// <summary>
    /// Allows a non-default stroke style to be specified for a given geometry at draw time.
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateStrokeStyle)</para>
    /// </summary>
    /// <param name="strokeStyleProperties">The strokeStyleProperties parameter.</param>
    /// <returns>StrokeStyle</returns>
    StrokeStyle ^
    CreateStrokeStyle(
        StrokeStyleProperties strokeStyleProperties
        );

    /// <summary>
    /// Creates a render target which is a source of bitmaps.
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateWicBitmapRenderTarget)</para>
    /// </summary>
    /// <param name="target">The target parameter.</param>
    /// <param name="renderTargetProperties">The renderTargetProperties parameter.</param>
    /// <returns>RenderTarget</returns>
    RenderTarget ^
    CreateWicBitmapRenderTarget(
        ImagingBitmap^ target,
        RenderTargetProperties renderTargetProperties
        );

    /// <summary>
    /// Creates a render target that appears on the display.
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateHwndRenderTarget)</para>
    /// </summary>
    /// <param name="renderTargetProperties">The renderTargetProperties parameter.</param>
    /// <param name="hwndRenderTargetProperties">The hwndRenderTargetProperties parameter.</param>
    /// <returns>HwndRenderTarget</returns>
    HwndRenderTarget ^
    CreateHwndRenderTarget(
        RenderTargetProperties renderTargetProperties,
        HwndRenderTargetProperties hwndRenderTargetProperties
        );

    /// <summary>
    /// Creates a render target that draws to a Graphics Surface. The device that owns the surface
    /// is used for rendering.
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateDxgiSurfaceRenderTarget)</para>
    /// </summary>
    /// <param name="surface">The dxgiSurface parameter.</param>
    /// <param name="renderTargetProperties">The renderTargetProperties parameter.</param>
    /// <returns>RenderTarget</returns>
    RenderTarget ^
    CreateGraphicsSurfaceRenderTarget(
        Surface^ surface,
        RenderTargetProperties renderTargetProperties
        );

    /// <summary>
    /// Creates a render target that draws to a GDI device context.
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateDCRenderTarget)</para>
    /// </summary>
    /// <param name="renderTargetProperties">The renderTargetProperties parameter.</param>
    /// <returns>DCRenderTarget</returns>
    DCRenderTarget ^
    CreateDCRenderTarget(
        RenderTargetProperties renderTargetProperties
        );


    /// <summary>
    /// Creates a <see cref="DrawingStateBlock"/> that can be used with the <see cref="RenderTarget::SaveDrawingState"/> and <see cref="RenderTarget::RestoreDrawingState"/> methods of a <see cref="RenderTarget"/>.
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateDCRenderTarget)</para>
    /// </summary>
    /// <param name="drawingStateDescription">A structure that contains antialiasing, transform, and tags information.</param>
    /// <param name="textRenderingParams">Text parameters that indicate how text should be rendered. this This parameter can be null.</param>
    /// <returns>The new drawing state block created by this method.</returns>
    DrawingStateBlock ^
    CreateDrawingStateBlock(
        DrawingStateDescription drawingStateDescription,
        RenderingParams^ textRenderingParams
        );

    /// <summary>
    /// Creates a <see cref="DrawingStateBlock"/> that can be used with the <see cref="RenderTarget::SaveDrawingState"/> and <see cref="RenderTarget::RestoreDrawingState"/> methods of a <see cref="RenderTarget"/>.
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateDCRenderTarget)</para>
    /// </summary>
    /// <param name="drawingStateDescription">A structure that contains antialiasing, transform, and tags information.</param>
    /// <returns>The new drawing state block created by this method.</returns>
    DrawingStateBlock ^
    CreateDrawingStateBlock(    
        DrawingStateDescription drawingStateDescription
        );

    /// <summary>
    /// Creates a <see cref="DrawingStateBlock"/> that can be used with the <see cref="RenderTarget::SaveDrawingState"/> and <see cref="RenderTarget::RestoreDrawingState"/> methods of a <see cref="RenderTarget"/>.
    /// <para>(Also see DirectX SDK: ID2D1Factory::CreateDCRenderTarget)</para>
    /// </summary>
    /// <returns>The new drawing state block created by this method.</returns>
    DrawingStateBlock ^
    CreateDrawingStateBlock(
        );

    /// <summary>
    /// Creates a factory object that can be used to create Direct2D resources.
    /// <para>(Also see DirectX SDK: D2D1CreateFactory)</para>
    /// </summary>
    /// <returns>The new factory object.</returns>
    static D2DFactory^ CreateFactory();

    /// <summary>
    /// Creates a factory object that can be used to create Direct2D resources.
    /// <para>(Also see DirectX SDK: D2D1CreateFactory)</para>
    /// </summary>
    /// <param name="factoryType">The type of factory to create.</param>
    /// <returns>The new factory object.</returns>
    static D2DFactory^ CreateFactory(
        D2DFactoryType factoryType
        );

    /// <summary>
    /// Creates a factory object that can be used to create Direct2D resources.
    /// <para>(Also see DirectX SDK: D2D1CreateFactory)</para>
    /// </summary>
    /// <param name="factoryType">The type of factory to create.</param>
    /// <param name="factoryOptions">Options defining the level of detail provided to the debugging layer.</param>
    /// <returns>The new factory object.</returns>
    static D2DFactory^ CreateFactory(
        D2DFactoryType factoryType,
        FactoryOptions factoryOptions
        );

internal:

D2DFactory();

D2DFactory(
    IUnknown *pInner
    );

};

} } } }

