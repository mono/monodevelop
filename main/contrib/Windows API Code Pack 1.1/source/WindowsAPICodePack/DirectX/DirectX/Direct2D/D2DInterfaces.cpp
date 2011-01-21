// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include <vector>

#include "WIC/WICBitmapSource.h"
#include "WIC/WICBitmap.h"
#include "WIC/WICBitmapLock.h"
#include "DXGI/DXGISurface.h"
#include "DirectWrite/DWriteTextFormat.h"
#include "DirectWrite/DWriteTextLayout.h"
#include "DirectWrite/DWriteRenderingParams.h"

using namespace std;
using namespace System::Globalization;
using namespace System::Runtime::InteropServices;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::WindowsImagingComponent;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct2D1 {

D2DResource::D2DResource(
    ) : DirectUnknown()
{
}

D2DResource::D2DResource(
    IUnknown *pInner
    ) : DirectUnknown(pInner)
{
}

D2DFactory^ D2DResource::Factory::get(void)
{
    ID2D1Factory * factory = NULL;
    CastInterface<ID2D1Resource>()->GetFactory(&factory);
    return factory ? gcnew D2DFactory(factory) : nullptr;
}

D2DBitmap::D2DBitmap(
    ) : D2DResource()
{
}

D2DBitmap::D2DBitmap(
    IUnknown *pInner
    ) : D2DResource(pInner)
{
}

SizeF
D2DBitmap::Size::get()
{
    D2D1_SIZE_F tempSize;

    tempSize = CastInterface<ID2D1Bitmap>()->GetSize();

    SizeF returnSize;

    returnSize.CopyFrom(tempSize);

    return returnSize;
}

SizeU
D2DBitmap::PixelSize::get()
{
    D2D1_SIZE_U tempSize;

    tempSize = CastInterface<ID2D1Bitmap>()->GetPixelSize();

    SizeU returnSize;

    returnSize.CopyFrom(tempSize);

    return returnSize;
}

Direct2D1::PixelFormat
D2DBitmap::PixelFormat::get(
    )
{
    D2D1_PIXEL_FORMAT tempFormat;

    tempFormat = CastInterface<ID2D1Bitmap>()->GetPixelFormat();

    Direct2D1::PixelFormat returnFormat;

    returnFormat.CopyFrom(tempFormat);

    return returnFormat;
}

DpiF
D2DBitmap::Dpi::get(
    )
{
    FLOAT dpiX;
    FLOAT dpiY;

        CastInterface<ID2D1Bitmap>()->GetDpi(
            &dpiX,
            &dpiY
            );

    return DpiF(dpiX, dpiY);
}


void
D2DBitmap::CopyFromBitmap(
    D2DBitmap ^ bitmap,
    Point2U destinationPoint,
    RectU sourceRect
    )
{
    D2D1_POINT_2U tempPoint;
    destinationPoint.CopyTo(&tempPoint);

    D2D1_RECT_U tempRect;
    sourceRect.CopyTo(&tempRect);

    Validate::VerifyResult(
        CastInterface<ID2D1Bitmap>()->CopyFromBitmap(
            &tempPoint,
            bitmap->CastInterface<ID2D1Bitmap>(),
            &tempRect
            ));
}

void
D2DBitmap::CopyFromBitmap(
    D2DBitmap ^ bitmap,
    Point2U destinationPoint
    )
{
    D2D1_POINT_2U tempPoint;

    destinationPoint.CopyTo(&tempPoint);

    Validate::VerifyResult(
        CastInterface<ID2D1Bitmap>()->CopyFromBitmap(
            &tempPoint,
            bitmap->CastInterface<ID2D1Bitmap>(),
            NULL
            ));
}

void
D2DBitmap::CopyFromBitmap(
    D2DBitmap ^ bitmap,
    RectU sourceRect
    )
{
    D2D1_RECT_U tempRect;

    sourceRect.CopyTo(&tempRect);

    Validate::VerifyResult(
        CastInterface<ID2D1Bitmap>()->CopyFromBitmap(
            NULL,
            bitmap->CastInterface<ID2D1Bitmap>(),
            &tempRect
            ));
}

void
D2DBitmap::CopyFromBitmap(
    D2DBitmap ^ bitmap
    )
{
    Validate::VerifyResult(
        CastInterface<ID2D1Bitmap>()->CopyFromBitmap(
            NULL,
            bitmap->CastInterface<ID2D1Bitmap>(),
            NULL
            ));
}

void
D2DBitmap::CopyFromRenderTarget(
    RenderTarget ^ renderTarget,
    Point2U destinationPoint,
    RectU sourceRect
    )
{
    D2D1_POINT_2U tempPoint;
    destinationPoint.CopyTo(&tempPoint);

    D2D1_RECT_U tempRect;
    sourceRect.CopyTo(&tempRect);

    Validate::VerifyResult(
        CastInterface<ID2D1Bitmap>()->CopyFromRenderTarget(
            &tempPoint,
            renderTarget->CastInterface<ID2D1RenderTarget>(),
            &tempRect
            ));
}

void
D2DBitmap::CopyFromRenderTarget(
    RenderTarget ^ renderTarget
    )
{
    Validate::VerifyResult(
        CastInterface<ID2D1Bitmap>()->CopyFromRenderTarget(
            NULL,
            renderTarget->CastInterface<ID2D1RenderTarget>(),
            NULL
            ));
}

void
D2DBitmap::CopyFromRenderTarget(
    RenderTarget ^ renderTarget,
    Point2U destinationPoint
    )
{
    D2D1_POINT_2U tempPoint;
    destinationPoint.CopyTo(&tempPoint);

    Validate::VerifyResult(
        CastInterface<ID2D1Bitmap>()->CopyFromRenderTarget(
            &tempPoint,
            renderTarget->CastInterface<ID2D1RenderTarget>(),
            NULL
            ));
}

void
D2DBitmap::CopyFromRenderTarget(
    RenderTarget ^ renderTarget,
    RectU sourceRect
    )
{
    D2D1_RECT_U tempRect;
    sourceRect.CopyTo(&tempRect);

    Validate::VerifyResult(
        CastInterface<ID2D1Bitmap>()->CopyFromRenderTarget(
            NULL,
            renderTarget->CastInterface<ID2D1RenderTarget>(),
            &tempRect
            ));
}

void
D2DBitmap::CopyFromMemory(
    RectU destinationRect,
    IntPtr sourceData,
    UINT32 pitch
    )
{
    D2D1_RECT_U tempRect;
    destinationRect.CopyTo(&tempRect);

    Validate::VerifyResult(
        CastInterface<ID2D1Bitmap>()->CopyFromMemory(
            &tempRect,
            sourceData.ToPointer(),
            pitch
            ));
}

void
D2DBitmap::CopyFromMemory(
    IntPtr sourceData,
    UINT32 pitch
    )
{
    Validate::VerifyResult(
        CastInterface<ID2D1Bitmap>()->CopyFromMemory(
            NULL,
            sourceData.ToPointer(),
            pitch
            ));
}

void
D2DBitmap::CopyFromMemory(
    RectU destinationRect,
    array<unsigned char>^ sourceData,
    UINT32 pitch
    )
{
    D2D1_RECT_U tempRect;
    destinationRect.CopyTo(&tempRect);

    pin_ptr<unsigned char> sourceDataPtr = &sourceData[0];

    Validate::VerifyResult(
        CastInterface<ID2D1Bitmap>()->CopyFromMemory(
            &tempRect,
            sourceDataPtr,
            pitch
            ));
}

void
D2DBitmap::CopyFromMemory(
    array<unsigned char>^ sourceData,
    UINT32 pitch
    )
{
    pin_ptr<unsigned char> sourceDataPtr = &sourceData[0];

    Validate::VerifyResult(
        CastInterface<ID2D1Bitmap>()->CopyFromMemory(
            NULL,
            sourceDataPtr,
            pitch
            ));
}

bool GradientStopCollection::Contains(GradientStop item)
{
	EnsureGradientStops();

	return gradientStops->Contains(item);
}

void GradientStopCollection::CopyTo(array<GradientStop>^ destinationArray, int arrayIndex)
{
	EnsureGradientStops();

	return gradientStops->CopyTo(destinationArray, arrayIndex);
}

IEnumerator<GradientStop>^ GradientStopCollection::GetEnumerator(void)
{
	EnsureGradientStops();

	return gradientStops->GetEnumerator();
}

System::Collections::IEnumerator^ GradientStopCollection::GetNonGenericEnumerator(void)
{
	EnsureGradientStops();

	return this->GetEnumerator();
}

int GradientStopCollection::IndexOf(GradientStop item)
{
	EnsureGradientStops();

	return gradientStops->IndexOf(item);
}

int GradientStopCollection::Count::get(void)
{
    return CastInterface<ID2D1GradientStopCollection>()->GetGradientStopCount();
}

GradientStop GradientStopCollection::default::get(int index)
{
	EnsureGradientStops();

	return gradientStops[index];
}


void GradientStopCollection::EnsureGradientStops(void)
{
	if (gradientStops != nullptr)
	{
		return;
	}

    gradientStops = gcnew List<GradientStop>();

    UINT32 count = Count;
    
    if (count > 0)
    {
        vector<D2D1_GRADIENT_STOP> tempStops(count);

        ZeroMemory(&tempStops[0], sizeof(D2D1_GRADIENT_STOP) * (count));
        
        CastInterface<ID2D1GradientStopCollection>()->GetGradientStops(
            &tempStops[0],
            count
            );

        for(UINT i = 0; i <  count; ++i)
        {
            GradientStop gradientStop;
            gradientStop.CopyFrom(tempStops[i]);
            gradientStops->Add(gradientStop);
        }
    }
}

Gamma
GradientStopCollection::ColorInterpolationGamma::get(
    )
{
    return static_cast<Gamma>(CastInterface<ID2D1GradientStopCollection>()->GetColorInterpolationGamma());
}

Direct2D1::ExtendMode
GradientStopCollection::ExtendMode::get(
    )
{
    return static_cast<Direct2D1::ExtendMode>(CastInterface<ID2D1GradientStopCollection>()->GetExtendMode());
}

Brush::Brush(
    ) : D2DResource()
{
}

Brush::Brush(
    IUnknown *pInner
    ) : D2DResource(pInner)
{
}

void
Brush::Opacity::set(
    FLOAT opacity)
{
        CastInterface<ID2D1Brush>()->SetOpacity(
            opacity
            );
}

void
Brush::Transform::set(
    Matrix3x2F transform
    )
{
    D2D1_MATRIX_3X2_F tempTransform;

    transform.CopyTo(&tempTransform);

        CastInterface<ID2D1Brush>()->SetTransform(
            &tempTransform
            );
}

FLOAT
Brush::Opacity::get(
    )
{
    return CastInterface<ID2D1Brush>()->GetOpacity(
            );
}

Matrix3x2F
Brush::Transform::get(
    )
{
    D2D1_MATRIX_3X2_F tempTransform;

    CastInterface<ID2D1Brush>()->GetTransform(
        &tempTransform
        );

    Matrix3x2F transform;
    transform.CopyFrom(tempTransform);

    return transform;

}

BitmapBrush::BitmapBrush(
    ) : Brush()
{
}

BitmapBrush::BitmapBrush(
    IUnknown *pInner
    ) : Brush(pInner)
{
}

void
BitmapBrush::ExtendModeX::set(
    ExtendMode extendModeX
    )
{
    CastInterface<ID2D1BitmapBrush>()->SetExtendModeX(
        static_cast<D2D1_EXTEND_MODE>(extendModeX)
        );
}

void
BitmapBrush::ExtendModeY::set(
    ExtendMode extendModeY
    )
{
    CastInterface<ID2D1BitmapBrush>()->SetExtendModeY(
        static_cast<D2D1_EXTEND_MODE>(extendModeY)
        );
}

void
BitmapBrush::InterpolationMode::set(
    BitmapInterpolationMode interpolationMode
    )
{
    CastInterface<ID2D1BitmapBrush>()->SetInterpolationMode(
        static_cast<D2D1_BITMAP_INTERPOLATION_MODE>(interpolationMode)
        );
}

void BitmapBrush::Bitmap::set(D2DBitmap ^ bitmap)
{
    CastInterface<ID2D1BitmapBrush>()->SetBitmap(
        bitmap->CastInterface<ID2D1Bitmap>());
}

ExtendMode
BitmapBrush::ExtendModeX::get(
    )
{
    return static_cast<ExtendMode>(CastInterface<ID2D1BitmapBrush>()->GetExtendModeX());
}

ExtendMode
BitmapBrush::ExtendModeY::get(
    )
{
    return static_cast<ExtendMode>(CastInterface<ID2D1BitmapBrush>()->GetExtendModeY());
}

BitmapInterpolationMode
BitmapBrush::InterpolationMode::get(
    )
{
    return static_cast<BitmapInterpolationMode>(CastInterface<ID2D1BitmapBrush>()->GetInterpolationMode());
}

D2DBitmap^ BitmapBrush::Bitmap::get(void)
{
    ID2D1Bitmap * ptr = NULL;
    CastInterface<ID2D1BitmapBrush>()->GetBitmap(&ptr);
    return ptr ? gcnew D2DBitmap(ptr) : nullptr;
}

SolidColorBrush::SolidColorBrush(
    ) : Brush()
{
}

SolidColorBrush::SolidColorBrush(
    IUnknown *pInner
    ) : Brush(pInner)
{
}

void
SolidColorBrush::Color::set(
    ColorF color
    )
{
    D2D1_COLOR_F tempColor;
    color.CopyTo(&tempColor);

    CastInterface<ID2D1SolidColorBrush>()->SetColor(
        &tempColor
        );
}

ColorF
SolidColorBrush::Color::get(
    )
{
    D2D1_COLOR_F tempColor;

    tempColor = CastInterface<ID2D1SolidColorBrush>()->GetColor();

    ColorF returnColor;

    returnColor.CopyFrom(tempColor);

    return returnColor;
}

LinearGradientBrush::LinearGradientBrush(
    ) : Brush()
{
}

LinearGradientBrush::LinearGradientBrush(
    IUnknown *pInner
    ) : Brush(pInner)
{
}

void
LinearGradientBrush::StartPoint::set(
    Point2F startPoint
    )
{
    D2D1_POINT_2F tempPoint;
    startPoint.CopyTo(&tempPoint);

    CastInterface<ID2D1LinearGradientBrush>()->SetStartPoint(
        tempPoint
        );
}

void
LinearGradientBrush::EndPoint::set(
    Point2F endPoint
    )
{
    D2D1_POINT_2F tempPoint;
    endPoint.CopyTo(&tempPoint);

    CastInterface<ID2D1LinearGradientBrush>()->SetEndPoint(
        tempPoint
        );
}

Point2F
LinearGradientBrush::StartPoint::get(
    )
{
    D2D1_POINT_2F tempPoint = CastInterface<ID2D1LinearGradientBrush>()->GetStartPoint();
    Point2F returnPoint;

    returnPoint.CopyFrom(tempPoint);

    return returnPoint;
}

Point2F
LinearGradientBrush::EndPoint::get(
    )
{
    D2D1_POINT_2F tempPoint = CastInterface<ID2D1LinearGradientBrush>()->GetEndPoint();
    Point2F returnPoint;

    returnPoint.CopyFrom(tempPoint);

    return returnPoint;
}

GradientStopCollection^ LinearGradientBrush::GradientStops::get(void)
{
    ID2D1GradientStopCollection * ptr = NULL;

    CastInterface<ID2D1LinearGradientBrush>()->GetGradientStopCollection(&ptr);

    return ptr != NULL ? gcnew GradientStopCollection(ptr) : nullptr;
}

RadialGradientBrush::RadialGradientBrush(
    ) : Brush()
{
}

RadialGradientBrush::RadialGradientBrush(
    IUnknown *pInner
    ) : Brush(pInner)
{
}

void
RadialGradientBrush::Center::set(
    Point2F center
    )
{
    D2D1_POINT_2F tempPoint;

    center.CopyTo(&tempPoint);

    CastInterface<ID2D1RadialGradientBrush>()->SetCenter(tempPoint);
}

void
RadialGradientBrush::GradientOriginOffset::set(
    Point2F gradientOriginOffset
    )
{
    D2D1_POINT_2F tempOffset;
    gradientOriginOffset.CopyTo(&tempOffset);

    CastInterface<ID2D1RadialGradientBrush>()->SetGradientOriginOffset(
        tempOffset
        );
}

void
RadialGradientBrush::RadiusX::set(
    FLOAT radiusX
    )
{

    CastInterface<ID2D1RadialGradientBrush>()->SetRadiusX(
        radiusX
        );


}

void
RadialGradientBrush::RadiusY::set(
    FLOAT radiusY
    )
{

    CastInterface<ID2D1RadialGradientBrush>()->SetRadiusY(
        radiusY
        );


}

Point2F
RadialGradientBrush::Center::get(
    )
{
    D2D1_POINT_2F tempPoint = CastInterface<ID2D1RadialGradientBrush>()->GetCenter();
    Point2F returnPoint;

    returnPoint.CopyFrom(tempPoint);

    return returnPoint;
}

Point2F
RadialGradientBrush::GradientOriginOffset::get(
    )
{
    D2D1_POINT_2F tempPoint = CastInterface<ID2D1RadialGradientBrush>()->GetGradientOriginOffset();
    Point2F returnPoint;

    returnPoint.CopyFrom(tempPoint);

    return returnPoint;
}

FLOAT
RadialGradientBrush::RadiusX::get(
    )
{
    return CastInterface<ID2D1RadialGradientBrush>()->GetRadiusX();
}

FLOAT
RadialGradientBrush::RadiusY::get(
    )
{
    return CastInterface<ID2D1RadialGradientBrush>()->GetRadiusY();
}

GradientStopCollection^ RadialGradientBrush::GradientStops::get(void)
{
    ID2D1GradientStopCollection * ptr = NULL;

    CastInterface<ID2D1RadialGradientBrush>()->GetGradientStopCollection(&ptr);

    return ptr != NULL ? gcnew GradientStopCollection(ptr) : nullptr;
}

DrawingStateBlock::DrawingStateBlock(
    ) : D2DResource()
{
}

DrawingStateBlock::DrawingStateBlock(
    IUnknown *pInner
    ) : D2DResource(pInner)
{
}

DrawingStateDescription
DrawingStateBlock::Description::get(
    )
{
    D2D1_DRAWING_STATE_DESCRIPTION tempDescription;

        CastInterface<ID2D1DrawingStateBlock>()->GetDescription(
            &tempDescription
            );

    DrawingStateDescription stateDescription;
    stateDescription.CopyFrom(tempDescription);

    return stateDescription;

}

void
DrawingStateBlock::Description::set(
    DrawingStateDescription stateDescription
    )
{
    D2D1_DRAWING_STATE_DESCRIPTION tempDescription;
    stateDescription.CopyTo(&tempDescription);

        CastInterface<ID2D1DrawingStateBlock>()->SetDescription(
            &tempDescription
            );
}

RenderingParams^ DrawingStateBlock::TextRenderingParams::get(void)
{
    IDWriteRenderingParams* renderingParams = NULL;
    CastInterface<ID2D1DrawingStateBlock>()->GetTextRenderingParams(&renderingParams);

    return renderingParams ? gcnew RenderingParams(renderingParams) : nullptr;
}

void DrawingStateBlock::TextRenderingParams::set(RenderingParams^ textRenderingParams)
{
    CastInterface<ID2D1DrawingStateBlock>()->SetTextRenderingParams(
        textRenderingParams == nullptr ? NULL : textRenderingParams->CastInterface<IDWriteRenderingParams>()
        );
}

RenderTarget::RenderTarget(
    ) : D2DResource()
{
}

RenderTarget::RenderTarget(
    IUnknown *pInner
    ) : D2DResource(pInner)
{
}

D2DBitmap ^
RenderTarget::CreateBitmap(
    SizeU size,
    BitmapProperties bitmapProperties
    )
{
    D2D1_SIZE_U tempSize;
    size.CopyTo(&tempSize);

    D2D1_BITMAP_PROPERTIES tempProperties;
    bitmapProperties.CopyTo(&tempProperties);

    ID2D1Bitmap * bitmapPtr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateBitmap(
            tempSize,
            NULL,
            0,
            &tempProperties,
            &bitmapPtr
            ));

    return bitmapPtr ? gcnew D2DBitmap(bitmapPtr): nullptr;
}

D2DBitmap ^
RenderTarget::CreateBitmap(
    SizeU size,
    IntPtr sourceData,
    UINT32 pitch,
    BitmapProperties bitmapProperties
    )
{
    D2D1_SIZE_U tempSize;
    size.CopyTo(&tempSize);

    D2D1_BITMAP_PROPERTIES tempProperties;
    bitmapProperties.CopyTo(&tempProperties);

    ID2D1Bitmap * bitmapPtr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateBitmap(
            tempSize,
            sourceData.ToPointer(),
            pitch,
            &tempProperties,
            &bitmapPtr
            ));

    return bitmapPtr ? gcnew D2DBitmap(bitmapPtr): nullptr;
}


D2DBitmap^ RenderTarget::CreateBitmap(
    SizeU size,
    array<unsigned char>^ source,
    UINT32 pitch,
    BitmapProperties bitmapProperties
    )
{
    D2D1_SIZE_U tempSize;
    size.CopyTo(&tempSize);

    D2D1_BITMAP_PROPERTIES tempProperties;
    bitmapProperties.CopyTo(&tempProperties);

    pin_ptr<unsigned char> bytePtr = &source[0];

    ID2D1Bitmap * bitmapPtr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateBitmap(
            tempSize,
            bytePtr,
            pitch,
            &tempProperties,
            &bitmapPtr
            ));

    return bitmapPtr ? gcnew D2DBitmap(bitmapPtr): nullptr;
}

D2DBitmap ^
RenderTarget::CreateBitmapFromWicBitmap(
    BitmapSource^ wicBitmapSource,
    BitmapProperties bitmapProperties
    )
{
    D2D1_BITMAP_PROPERTIES tempProperties;
    bitmapProperties.CopyTo(&tempProperties);

    ID2D1Bitmap * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateBitmapFromWicBitmap(
            wicBitmapSource->CastInterface<IWICBitmapSource>(),
            &tempProperties,
            &ptr
            ));

    return ptr ? gcnew D2DBitmap(ptr): nullptr;
}


D2DBitmap ^
RenderTarget::CreateBitmapFromWicBitmap(
    BitmapSource^ wicBitmapSource
    )
{
    ID2D1Bitmap * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateBitmapFromWicBitmap(
            wicBitmapSource->CastInterface<IWICBitmapSource>(),
            NULL,
            &ptr
            ));

    return ptr ? gcnew D2DBitmap(ptr): nullptr;
}

D2DBitmap ^
RenderTarget::CreateSharedBitmap(
    ImagingBitmapLock^ wicBitmap,
    BitmapProperties bitmapProperties
    )
{
    D2D1_BITMAP_PROPERTIES tempProperties;
    bitmapProperties.CopyTo(&tempProperties);

    ID2D1Bitmap * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateSharedBitmap(
            __uuidof(IWICBitmapLock),
            (void*) wicBitmap->CastInterface<IWICBitmapLock>(),
            &tempProperties,
            &ptr
            ));
    return ptr ? gcnew D2DBitmap(ptr): nullptr;
}

D2DBitmap ^
RenderTarget::CreateSharedBitmap(
    ImagingBitmapLock^ wicBitmap
    )
{
ID2D1Bitmap * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateSharedBitmap(
            __uuidof(IWICBitmapLock),
            (void*) wicBitmap->CastInterface<IWICBitmapLock>(),
            NULL,
            &ptr
            ));
    return ptr ? gcnew D2DBitmap(ptr): nullptr;
}

D2DBitmap ^
RenderTarget::CreateSharedBitmap(
    Surface^ surface,
    BitmapProperties bitmapProperties
    )
{
    D2D1_BITMAP_PROPERTIES tempProperties;
    bitmapProperties.CopyTo(&tempProperties);

    ID2D1Bitmap * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateSharedBitmap(
            __uuidof(IDXGISurface),
            (void*) surface->CastInterface<IDXGISurface>(),
            &tempProperties,
            &ptr
            ));
    return ptr ? gcnew D2DBitmap(ptr): nullptr;
}

D2DBitmap ^
RenderTarget::CreateSharedBitmap(
    Surface^ surface
    )
{
    ID2D1Bitmap * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateSharedBitmap(
            __uuidof(IDXGISurface),
            (void*) surface->CastInterface<IDXGISurface>(),
            NULL,
            &ptr
            ));
    return ptr ? gcnew D2DBitmap(ptr): nullptr;
}

D2DBitmap ^
RenderTarget::CreateSharedBitmap(
    D2DBitmap ^ bitmap,
    BitmapProperties bitmapProperties
    )
{
    D2D1_BITMAP_PROPERTIES tempProperties;
    bitmapProperties.CopyTo(&tempProperties);

    ID2D1Bitmap * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateSharedBitmap(
            __uuidof(ID2D1Bitmap),
            (void*) bitmap->CastInterface<ID2D1Bitmap>(),
            &tempProperties,
            &ptr
            ));
    return ptr ? gcnew D2DBitmap(ptr): nullptr;
}

D2DBitmap ^
RenderTarget::CreateSharedBitmap(
    D2DBitmap ^ bitmap
    )
{
    ID2D1Bitmap * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateSharedBitmap(
            __uuidof(ID2D1Bitmap),
            (void*) bitmap->CastInterface<ID2D1Bitmap>(),
            NULL,
            &ptr
            ));
    return ptr ? gcnew D2DBitmap(ptr): nullptr;
}


BitmapBrush ^
RenderTarget::CreateBitmapBrush(
    D2DBitmap ^ bitmap,
    BitmapBrushProperties bitmapBrushProperties,
    BrushProperties brushProperties
    )
{
    D2D1_BITMAP_BRUSH_PROPERTIES tempBitmapProperties;
    bitmapBrushProperties.CopyTo(&tempBitmapProperties);

    D2D1_BRUSH_PROPERTIES tempProperties;
    brushProperties.CopyTo(&tempProperties);

    ID2D1BitmapBrush * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateBitmapBrush(
            bitmap->CastInterface<ID2D1Bitmap>(),
            &tempBitmapProperties,
            &tempProperties,
            &ptr
            ));


    return ptr ? gcnew BitmapBrush(ptr): nullptr;
}

BitmapBrush ^
RenderTarget::CreateBitmapBrush(
    D2DBitmap ^ bitmap,
    BitmapBrushProperties bitmapBrushProperties
    )
{
    D2D1_BITMAP_BRUSH_PROPERTIES tempProperties;
    bitmapBrushProperties.CopyTo(&tempProperties);

    ID2D1BitmapBrush * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateBitmapBrush(
            bitmap->CastInterface<ID2D1Bitmap>(),
            &tempProperties,
            NULL,
            &ptr
            ));


    return ptr ? gcnew BitmapBrush(ptr): nullptr;
}
BitmapBrush ^
RenderTarget::CreateBitmapBrush(
    D2DBitmap ^ bitmap,
    BrushProperties brushProperties
    )
{
    D2D1_BRUSH_PROPERTIES tempProperties;
    brushProperties.CopyTo(&tempProperties);

    ID2D1BitmapBrush * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateBitmapBrush(
            bitmap->CastInterface<ID2D1Bitmap>(),
            NULL,
            &tempProperties,
            &ptr
            ));


    return ptr ? gcnew BitmapBrush(ptr): nullptr;
}
BitmapBrush ^
RenderTarget::CreateBitmapBrush(
    D2DBitmap ^ bitmap
    )
{
    ID2D1BitmapBrush * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateBitmapBrush(
            bitmap->CastInterface<ID2D1Bitmap>(),
            NULL,
            NULL,
            &ptr
            ));


    return ptr ? gcnew BitmapBrush(ptr): nullptr;
}

SolidColorBrush ^
RenderTarget::CreateSolidColorBrush(
    ColorF color,
    BrushProperties brushProperties
    )
{
    D2D1_COLOR_F tempColor;
    color.CopyTo(&tempColor);
    D2D1_BRUSH_PROPERTIES tempProperties;
    brushProperties.CopyTo(&tempProperties);

    ID2D1SolidColorBrush * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateSolidColorBrush(
            &tempColor,
            &tempProperties,
            &ptr
            ));

    return ptr ? gcnew SolidColorBrush(ptr): nullptr;
}

SolidColorBrush ^
RenderTarget::CreateSolidColorBrush(
    ColorF color
    )
{
    D2D1_COLOR_F tempColor;
    color.CopyTo(&tempColor);

    ID2D1SolidColorBrush * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateSolidColorBrush(
            &tempColor,
            NULL,
            &ptr
            ));

    return ptr ? gcnew SolidColorBrush(ptr): nullptr;
}

GradientStopCollection ^
RenderTarget::CreateGradientStopCollection(
    IEnumerable<GradientStop> ^ gradientStops,
    Gamma colorInterpolationGamma,
    ExtendMode extendMode
    )
{
    vector<D2D1_GRADIENT_STOP> gradientStopsVector;
    ID2D1GradientStopCollection * ptr = NULL;

    for each (GradientStop gradientStop in gradientStops)
    {
        D2D1_GRADIENT_STOP tempStops;
        gradientStop.CopyTo(&tempStops);
        gradientStopsVector.push_back(tempStops);
    }

    if (gradientStopsVector.size() == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "gradientStops");
    }

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateGradientStopCollection(
            &gradientStopsVector[0],
            static_cast<UINT>(gradientStopsVector.size()),
            static_cast<D2D1_GAMMA>(colorInterpolationGamma),
            static_cast<D2D1_EXTEND_MODE>(extendMode),
            &ptr
            ));


    return ptr ? gcnew GradientStopCollection(ptr) : nullptr;
}

LinearGradientBrush ^
RenderTarget::CreateLinearGradientBrush(
    LinearGradientBrushProperties linearGradientBrushProperties,
    GradientStopCollection ^ gradientStopCollection,
    BrushProperties brushProperties
    )
{
    D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES tempGradientProperties;
    linearGradientBrushProperties.CopyTo(&tempGradientProperties);

    D2D1_BRUSH_PROPERTIES tempProperties;
    brushProperties.CopyTo(&tempProperties);

    ID2D1LinearGradientBrush * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateLinearGradientBrush(
            &tempGradientProperties,
            &tempProperties,
            gradientStopCollection->CastInterface<ID2D1GradientStopCollection>(),
            &ptr
            ));

    return ptr ? gcnew LinearGradientBrush(ptr): nullptr;
}


LinearGradientBrush ^
RenderTarget::CreateLinearGradientBrush(
    LinearGradientBrushProperties linearGradientBrushProperties,
    GradientStopCollection ^ gradientStopCollection
    )
{
    D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES tempProperties;
    linearGradientBrushProperties.CopyTo(&tempProperties);

    ID2D1LinearGradientBrush * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateLinearGradientBrush(
            &tempProperties,
            NULL,
            gradientStopCollection->CastInterface<ID2D1GradientStopCollection>(),
            &ptr
            ));

    return ptr ? gcnew LinearGradientBrush(ptr): nullptr;
}


RadialGradientBrush ^
RenderTarget::CreateRadialGradientBrush(
    RadialGradientBrushProperties radialGradientBrushProperties,
    GradientStopCollection ^ gradientStopCollection,
    BrushProperties brushProperties
    )
{
    D2D1_RADIAL_GRADIENT_BRUSH_PROPERTIES tempGradientProperties;
    radialGradientBrushProperties.CopyTo(&tempGradientProperties);

    D2D1_BRUSH_PROPERTIES tempProperties;
    brushProperties.CopyTo(&tempProperties);

    ID2D1RadialGradientBrush * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateRadialGradientBrush(
            &tempGradientProperties,
            &tempProperties,
            gradientStopCollection->CastInterface<ID2D1GradientStopCollection>(),
            &ptr
            ));

    return ptr ? gcnew RadialGradientBrush(ptr) : nullptr;
}


RadialGradientBrush ^
RenderTarget::CreateRadialGradientBrush(
    RadialGradientBrushProperties radialGradientBrushProperties,
    GradientStopCollection ^ gradientStopCollection
    )
{
    D2D1_RADIAL_GRADIENT_BRUSH_PROPERTIES tempProperties;
    radialGradientBrushProperties.CopyTo(&tempProperties);

    ID2D1RadialGradientBrush * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateRadialGradientBrush(
            &tempProperties,
            NULL,
            gradientStopCollection->CastInterface<ID2D1GradientStopCollection>(),
            &ptr
            ));

    return ptr ? gcnew RadialGradientBrush(ptr) : nullptr;
}

BitmapRenderTarget ^
RenderTarget::CreateCompatibleRenderTarget(
    CompatibleRenderTargetOptions options,
    SizeF desiredSize,
    SizeU desiredPixelSize
    )
{
    D2D1_SIZE_F tempSize;
    desiredSize.CopyTo(&tempSize);

    D2D1_SIZE_U tempPixelSize;
    desiredPixelSize.CopyTo(&tempPixelSize);

    ID2D1BitmapRenderTarget * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateCompatibleRenderTarget(
            &tempSize,
            &tempPixelSize,
            NULL,
            static_cast<D2D1_COMPATIBLE_RENDER_TARGET_OPTIONS>(options),
            &ptr
            ));

    return ptr ? gcnew BitmapRenderTarget(ptr) : nullptr;
}

BitmapRenderTarget ^
RenderTarget::CreateCompatibleRenderTarget(
    CompatibleRenderTargetOptions options,
    SizeF desiredSize
    )
{
    D2D1_SIZE_F tempSize;
    desiredSize.CopyTo(&tempSize);

    ID2D1BitmapRenderTarget * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateCompatibleRenderTarget(
            &tempSize,
            NULL,
            NULL,
            static_cast<D2D1_COMPATIBLE_RENDER_TARGET_OPTIONS>(options),
            &ptr
            ));

    return ptr ? gcnew BitmapRenderTarget(ptr) : nullptr;
}

BitmapRenderTarget ^
RenderTarget::CreateCompatibleRenderTarget(
    CompatibleRenderTargetOptions options,
    SizeU desiredPixelSize
    )
{
    D2D1_SIZE_U tempSize;
    desiredPixelSize.CopyTo(&tempSize);

    ID2D1BitmapRenderTarget * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateCompatibleRenderTarget(
            NULL,
            &tempSize,
            NULL,
            static_cast<D2D1_COMPATIBLE_RENDER_TARGET_OPTIONS>(options),
            &ptr
            ));

    return ptr ? gcnew BitmapRenderTarget(ptr) : nullptr;
}

BitmapRenderTarget ^
RenderTarget::CreateCompatibleRenderTarget(
    CompatibleRenderTargetOptions options
    )
{
    ID2D1BitmapRenderTarget * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateCompatibleRenderTarget(
            NULL,
            NULL,
            NULL,
            static_cast<D2D1_COMPATIBLE_RENDER_TARGET_OPTIONS>(options),
            &ptr
            ));

    return ptr ? gcnew BitmapRenderTarget(ptr) : nullptr;
}

BitmapRenderTarget ^
RenderTarget::CreateCompatibleRenderTarget()
{
    ID2D1BitmapRenderTarget * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateCompatibleRenderTarget(
            NULL,
            NULL,
            NULL,
            D2D1_COMPATIBLE_RENDER_TARGET_OPTIONS_NONE,
            &ptr
            ));

    return ptr ? gcnew BitmapRenderTarget(ptr) : nullptr;
}

BitmapRenderTarget ^
RenderTarget::CreateCompatibleRenderTarget(
    CompatibleRenderTargetOptions options,
    Direct2D1::PixelFormat desiredFormat,
    SizeF desiredSize,
    SizeU desiredPixelSize
    )
{
    D2D1_SIZE_F tempSize;
    desiredSize.CopyTo(&tempSize);

    D2D1_SIZE_U tempPixelSize;
    desiredPixelSize.CopyTo(&tempPixelSize);

    D2D1_PIXEL_FORMAT tempFormat;
    desiredFormat.CopyTo(&tempFormat);

    ID2D1BitmapRenderTarget * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateCompatibleRenderTarget(
            &tempSize,
            &tempPixelSize,
            &tempFormat,
            static_cast<D2D1_COMPATIBLE_RENDER_TARGET_OPTIONS>(options),
            &ptr
            ));

    return ptr ? gcnew BitmapRenderTarget(ptr) : nullptr;
}

BitmapRenderTarget ^
RenderTarget::CreateCompatibleRenderTarget(
    CompatibleRenderTargetOptions options,
    Direct2D1::PixelFormat desiredFormat,
    SizeF desiredSize
    )
{
    D2D1_SIZE_F tempSize;
    desiredSize.CopyTo(&tempSize);

    D2D1_PIXEL_FORMAT tempFormat;
    desiredFormat.CopyTo(&tempFormat);

    ID2D1BitmapRenderTarget * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateCompatibleRenderTarget(
            &tempSize,
            NULL,
            &tempFormat,
            static_cast<D2D1_COMPATIBLE_RENDER_TARGET_OPTIONS>(options),
            &ptr
            ));

    return ptr ? gcnew BitmapRenderTarget(ptr) : nullptr;
}

BitmapRenderTarget ^
RenderTarget::CreateCompatibleRenderTarget(
    CompatibleRenderTargetOptions options,
    Direct2D1::PixelFormat desiredFormat,
    SizeU desiredPixelSize
    )
{
    D2D1_SIZE_U tempSize;
    desiredPixelSize.CopyTo(&tempSize);

    D2D1_PIXEL_FORMAT tempFormat;
    desiredFormat.CopyTo(&tempFormat);

    ID2D1BitmapRenderTarget * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateCompatibleRenderTarget(
            NULL,
            &tempSize,
            &tempFormat,
            static_cast<D2D1_COMPATIBLE_RENDER_TARGET_OPTIONS>(options),
            &ptr
            ));

    return ptr ? gcnew BitmapRenderTarget(ptr) : nullptr;
}

BitmapRenderTarget ^
RenderTarget::CreateCompatibleRenderTarget(
    CompatibleRenderTargetOptions options,
    Direct2D1::PixelFormat desiredFormat
    )
{

    D2D1_PIXEL_FORMAT tempFormat;
    desiredFormat.CopyTo(&tempFormat);

    ID2D1BitmapRenderTarget * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateCompatibleRenderTarget(
            NULL,
            NULL,
            &tempFormat,
            static_cast<D2D1_COMPATIBLE_RENDER_TARGET_OPTIONS>(options),
            &ptr
            ));

    return ptr ? gcnew BitmapRenderTarget(ptr) : nullptr;
}

Layer ^
RenderTarget::CreateLayer(
    SizeF size
    )
{
    D2D1_SIZE_F tempSize;
    size.CopyTo(&tempSize);

    ID2D1Layer * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateLayer(
            &tempSize,
            &ptr
            ));


    return ptr ? gcnew Layer(ptr) : nullptr;
}

Layer ^
RenderTarget::CreateLayer(
    )
{
    ID2D1Layer * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateLayer(
            NULL,
            &ptr
            ));


    return ptr ? gcnew Layer(ptr) : nullptr;
}

Mesh ^
RenderTarget::CreateMesh(
    )
{

    ID2D1Mesh * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1RenderTarget>()->CreateMesh(
            &ptr
            ));
    return ptr ? gcnew Mesh(ptr) : nullptr;
}

void
RenderTarget::DrawLine(
    Point2F point1,
    Point2F point2,
    Brush ^ brush,
    FLOAT strokeWidth,
    StrokeStyle ^ strokeStyle
    )
{

    pin_ptr<Point2F> tempPoint1 = &point1;
    pin_ptr<Point2F> tempPoint2 = &point2;

    CastInterface<ID2D1RenderTarget>()->DrawLine(
        *((D2D1_POINT_2F*)tempPoint1),
        *((D2D1_POINT_2F*)tempPoint2),
        brush->CastInterface<ID2D1Brush>(),
        strokeWidth,
        strokeStyle ? strokeStyle->CastInterface<ID2D1StrokeStyle>() : NULL
        );
}

void
RenderTarget::DrawLine(
    Point2F point0,
    Point2F point1,
    Brush ^ brush,
    FLOAT strokeWidth
    )
{
    DrawLine(point0, point1, brush, strokeWidth, nullptr);
}
void
RenderTarget::DrawRectangle(
    RectF rect,
    Brush ^ brush,
    FLOAT strokeWidth,
    StrokeStyle ^ strokeStyle
    )
{
    pin_ptr<RectF> rectPtr = &rect; 

    CastInterface<ID2D1RenderTarget>()->DrawRectangle(
        (D2D1_RECT_F*)(rectPtr),
        brush->CastInterface<ID2D1Brush>(),
        strokeWidth,
        strokeStyle ? strokeStyle->CastInterface<ID2D1StrokeStyle>() : NULL
        );
}

void
RenderTarget::DrawRectangle(
    RectF rect,
    Brush ^ brush,
    FLOAT strokeWidth
    )
{
    DrawRectangle(rect, brush, strokeWidth, nullptr);
}
void
RenderTarget::FillRectangle(
    RectF rect,
    Brush ^ brush
    )
{
    pin_ptr<RectF> tempRect = &rect; 

    CastInterface<ID2D1RenderTarget>()->FillRectangle(
        (D2D1_RECT_F*)tempRect,
        brush->CastInterface<ID2D1Brush>()
        );
}

void
RenderTarget::DrawRoundedRectangle(
    RoundedRect roundedRect,
    Brush ^ brush,
    FLOAT strokeWidth,
    StrokeStyle ^ strokeStyle
    )
{
    pin_ptr<RoundedRect> tempRect = &roundedRect; 

    CastInterface<ID2D1RenderTarget>()->DrawRoundedRectangle(
        (D2D1_ROUNDED_RECT*)tempRect,
        brush->CastInterface<ID2D1Brush>(),
        strokeWidth,
        strokeStyle ? strokeStyle->CastInterface<ID2D1StrokeStyle>() : NULL
        );
}

void
RenderTarget::DrawRoundedRectangle(
    RoundedRect roundedRect,
    Brush ^ brush,
    FLOAT strokeWidth    
    )
{
    DrawRoundedRectangle(roundedRect, brush, strokeWidth, nullptr);
}
void
RenderTarget::FillRoundedRectangle(
    RoundedRect roundedRect,
    Brush ^ brush
    )
{
    pin_ptr<RoundedRect> tempRect = &roundedRect; 

    CastInterface<ID2D1RenderTarget>()->FillRoundedRectangle(
        (D2D1_ROUNDED_RECT*)tempRect,
        brush->CastInterface<ID2D1Brush>()
        );


}

void
RenderTarget::DrawEllipse(
    Ellipse ellipse,
    Brush ^ brush,
    FLOAT strokeWidth,
    StrokeStyle ^ strokeStyle
    )
{
    D2D1_ELLIPSE tempEllipse;
    ellipse.CopyTo(&tempEllipse);

        CastInterface<ID2D1RenderTarget>()->DrawEllipse(
            &tempEllipse,
            brush->CastInterface<ID2D1Brush>(),
            strokeWidth,
            strokeStyle ? strokeStyle->CastInterface<ID2D1StrokeStyle>() : NULL
            );
}

void
RenderTarget::DrawEllipse(
    Ellipse ellipse,
    Brush ^ brush,
    FLOAT strokeWidth
    )
{
    DrawEllipse(ellipse, brush, strokeWidth, nullptr);
}
void
RenderTarget::FillEllipse(
    Ellipse ellipse,
    Brush ^ brush
    )
{
    D2D1_ELLIPSE tempEllipse;
    ellipse.CopyTo(&tempEllipse);

        CastInterface<ID2D1RenderTarget>()->FillEllipse(
            &tempEllipse,
            brush->CastInterface<ID2D1Brush>()
            );
}

void
RenderTarget::DrawGeometry(
    Geometry ^ geometry,
    Brush ^ brush,
    FLOAT strokeWidth,
    StrokeStyle ^ strokeStyle
    )
{
    CastInterface<ID2D1RenderTarget>()->DrawGeometry(
        geometry->CastInterface<ID2D1Geometry>(),
        brush->CastInterface<ID2D1Brush>(),
        strokeWidth,
        strokeStyle ? strokeStyle->CastInterface<ID2D1StrokeStyle>() : NULL
        );
}

void
RenderTarget::DrawGeometry(
    Geometry ^ geometry,
    Brush ^ brush,
    FLOAT strokeWidth
    )
{
    DrawGeometry(geometry, brush, strokeWidth, nullptr);
}

void
RenderTarget::FillGeometry(
    Geometry ^ geometry,
    Brush ^ brush,
    Brush ^ opacityBrush
    )
{

    CastInterface<ID2D1RenderTarget>()->FillGeometry(
        geometry->CastInterface<ID2D1Geometry>(),
        brush->CastInterface<ID2D1Brush>(),
        opacityBrush ? opacityBrush->CastInterface<ID2D1Brush>() : NULL
        );
}

void
RenderTarget::FillGeometry(
    Geometry ^ geometry,
    Brush ^ brush
    )
{
    FillGeometry(geometry, brush, nullptr);
}

void
RenderTarget::FillMesh(
    Mesh ^ mesh,
    Brush ^ brush
    )
{
    CastInterface<ID2D1RenderTarget>()->FillMesh(
        mesh->CastInterface<ID2D1Mesh>(),
        brush->CastInterface<ID2D1Brush>()
        );
}

void
RenderTarget::FillOpacityMask(
    D2DBitmap ^ opacityMask,
    Brush ^ brush,
    OpacityMaskContent content,
    RectF destinationRectangle,
    RectF sourceRectangle
    )
{
    D2D1_RECT_F tempSourceRectangle;
    destinationRectangle.CopyTo(&tempSourceRectangle);

    D2D1_RECT_F tempDestinationRectangle;
    sourceRectangle.CopyTo(&tempDestinationRectangle);

    CastInterface<ID2D1RenderTarget>()->FillOpacityMask(
        opacityMask->CastInterface<ID2D1Bitmap>(),
        brush->CastInterface<ID2D1Brush>(),
        static_cast<D2D1_OPACITY_MASK_CONTENT>(content),
        &tempDestinationRectangle,
        &tempSourceRectangle
        );
}

void
RenderTarget::FillOpacityMaskAtOrigin(
    D2DBitmap ^ opacityMask,
    Brush ^ brush,
    OpacityMaskContent content,
    RectF sourceRectangle
    )
{
    D2D1_RECT_F tempRectangle;
    sourceRectangle.CopyTo(&tempRectangle);

    CastInterface<ID2D1RenderTarget>()->FillOpacityMask(
        opacityMask->CastInterface<ID2D1Bitmap>(),
        brush->CastInterface<ID2D1Brush>(),
        static_cast<D2D1_OPACITY_MASK_CONTENT>(content),
        NULL,
        &tempRectangle
        );
}

void
RenderTarget::FillOpacityMask(
    D2DBitmap ^ opacityMask,
    Brush ^ brush,
    OpacityMaskContent content,
    RectF destinationRectangle
    )
{
    D2D1_RECT_F tempRectangle;
    destinationRectangle.CopyTo(&tempRectangle);

    CastInterface<ID2D1RenderTarget>()->FillOpacityMask(
        opacityMask->CastInterface<ID2D1Bitmap>(),
        brush->CastInterface<ID2D1Brush>(),
        static_cast<D2D1_OPACITY_MASK_CONTENT>(content),
        &tempRectangle,
        NULL
        );
}

void
RenderTarget::FillOpacityMask(
    D2DBitmap ^ opacityMask,
    Brush ^ brush,
    OpacityMaskContent content
    )
{
    CastInterface<ID2D1RenderTarget>()->FillOpacityMask(
        opacityMask->CastInterface<ID2D1Bitmap>(),
        brush->CastInterface<ID2D1Brush>(),
        static_cast<D2D1_OPACITY_MASK_CONTENT>(content),
        NULL,
        NULL
        );
}

void
RenderTarget::DrawBitmap(
    D2DBitmap ^ bitmap,
    FLOAT opacity,
    BitmapInterpolationMode interpolationMode,
    RectF destinationRectangle,
    RectF sourceRectangle
    )
{
    D2D1_RECT_F tempDestinationRectangle;
    destinationRectangle.CopyTo(&tempDestinationRectangle);
        
    D2D1_RECT_F tempSourceRectangle;
    sourceRectangle.CopyTo(&tempSourceRectangle);

    CastInterface<ID2D1RenderTarget>()->DrawBitmap(
        bitmap->CastInterface<ID2D1Bitmap>(),
        &tempDestinationRectangle,
        opacity,
        static_cast<D2D1_BITMAP_INTERPOLATION_MODE>(interpolationMode),
        &tempSourceRectangle
        );
}

void
RenderTarget::DrawBitmap(
    D2DBitmap ^ bitmap
    )
{
    CastInterface<ID2D1RenderTarget>()->DrawBitmap(
        bitmap->CastInterface<ID2D1Bitmap>(),
        NULL,
        1.0,
        D2D1_BITMAP_INTERPOLATION_MODE_NEAREST_NEIGHBOR,
        NULL
        );
}

void
RenderTarget::DrawBitmap(
    D2DBitmap ^ bitmap,
    FLOAT opacity,
    BitmapInterpolationMode interpolationMode,
    RectF destinationRectangle
    )
{
    D2D1_RECT_F tempRectangle;
    destinationRectangle.CopyTo(&tempRectangle);
        
    CastInterface<ID2D1RenderTarget>()->DrawBitmap(
        bitmap->CastInterface<ID2D1Bitmap>(),
        &tempRectangle,
        opacity,
        static_cast<D2D1_BITMAP_INTERPOLATION_MODE>(interpolationMode),
        NULL
        );
}

void
RenderTarget::DrawBitmapAtOrigin(
    D2DBitmap ^ bitmap,
    FLOAT opacity,
    BitmapInterpolationMode interpolationMode,
    RectF sourceRectangle
    )
{
    D2D1_RECT_F tempRectangle;
    sourceRectangle.CopyTo(&tempRectangle);

    CastInterface<ID2D1RenderTarget>()->DrawBitmap(
        bitmap->CastInterface<ID2D1Bitmap>(),
        NULL,
        opacity,
        static_cast<D2D1_BITMAP_INTERPOLATION_MODE>(interpolationMode),
        &tempRectangle
        );
}

void
RenderTarget::DrawBitmap(
    D2DBitmap ^ bitmap,
    FLOAT opacity,
    BitmapInterpolationMode interpolationMode
    )
{
    CastInterface<ID2D1RenderTarget>()->DrawBitmap(
        bitmap->CastInterface<ID2D1Bitmap>(),
        NULL,
        opacity,
        static_cast<D2D1_BITMAP_INTERPOLATION_MODE>(interpolationMode),
        NULL
        );
}

GdiInteropRenderTarget^ RenderTarget::GdiInteropRenderTarget::get(void)
{
    ID2D1GdiInteropRenderTarget* gdiRenderTargt = NULL;
    CastInterface<IUnknown>()->QueryInterface(__uuidof(ID2D1GdiInteropRenderTarget), (void**) &gdiRenderTargt);

    return gdiRenderTargt ? gcnew Direct2D1::GdiInteropRenderTarget(gdiRenderTargt) : nullptr;
}

void
RenderTarget::Transform::set(
    Matrix3x2F transform
    )
{
    D2D1_MATRIX_3X2_F tempTransform;
    transform.CopyTo(&tempTransform);

    CastInterface<ID2D1RenderTarget>()->SetTransform(
        &tempTransform
        );
}

Matrix3x2F
RenderTarget::Transform::get(
    )
{
    D2D1_MATRIX_3X2_F tempTransform;

        CastInterface<ID2D1RenderTarget>()->GetTransform(
            &tempTransform
            );

    Matrix3x2F transform;
    transform.CopyFrom(tempTransform);
    return transform;
}

void
RenderTarget::AntiAliasMode::set(
    Direct2D1::AntiAliasMode antiAliasMode
    )
{
    CastInterface<ID2D1RenderTarget>()->SetAntialiasMode(
        static_cast<D2D1_ANTIALIAS_MODE>(antiAliasMode)
        );
}

Direct2D1::AntiAliasMode
RenderTarget::AntiAliasMode::get(
    )
{
    return static_cast<Direct2D1::AntiAliasMode>(CastInterface<ID2D1RenderTarget>()->GetAntialiasMode());
}

void
RenderTarget::TextAntiAliasMode::set(
    Direct2D1::TextAntiAliasMode textAntiAliasMode
    )
{
    CastInterface<ID2D1RenderTarget>()->SetTextAntialiasMode(
        static_cast<D2D1_TEXT_ANTIALIAS_MODE>(textAntiAliasMode)
        );
}

Direct2D1::TextAntiAliasMode
RenderTarget::TextAntiAliasMode::get(
    )
{
    return static_cast<Direct2D1::TextAntiAliasMode>(CastInterface<ID2D1RenderTarget>()->GetTextAntialiasMode());
}

void
RenderTarget::Tags::set(
    Direct2D1::Tags tags
    )
{
    CastInterface<ID2D1RenderTarget>()->SetTags(
        static_cast<D2D1_TAG>(tags.Tag1),
        static_cast<D2D1_TAG>(tags.Tag2)
        );
}

Direct2D1::Tags
RenderTarget::Tags::get(
    )
{
    D2D1_TAG tag1, tag2;

    CastInterface<ID2D1RenderTarget>()->GetTags(
        &tag1,
        &tag2
        );

    return Direct2D1::Tags(static_cast<UINT64>(tag1), static_cast<UINT64>(tag2));
}

void
RenderTarget::PushLayer(
    LayerParameters layerParameters,
    Layer ^ layer
    )
{
    D2D1_LAYER_PARAMETERS tempParameters;
    layerParameters.CopyTo(&tempParameters);

    CastInterface<ID2D1RenderTarget>()->PushLayer(
        &tempParameters,
        layer->CastInterface<ID2D1Layer>()
        );
}

void
RenderTarget::PopLayer(
    )
{
    CastInterface<ID2D1RenderTarget>()->PopLayer(
        );
}

Direct2D1::Tags
RenderTarget::Flush(
    )
{
    D2D1_TAG tag1, tag2;

    HRESULT hr = 
        CastInterface<ID2D1RenderTarget>()->Flush(
            &tag1,
            &tag2
            );

    // REVIEW: should generalize this pattern in a "VerifyResult"-like
    // method.  Should try to eliminate all direct callers of
    // GetExceptionForHR() except the Verify... methods.
    if (!SUCCEEDED(hr))
    {
        Exception^ e = Validate::GetExceptionForHR(hr);

        throw gcnew Direct2DException(
            String::Format(CultureInfo::CurrentCulture,
                "Flush has failed with error: {0} Tags=({1}, {2}).",
                e->Message, static_cast<UInt64>(tag1), static_cast<UInt64>(tag2)),
                e);

    }
    else
    {
        return Direct2D1::Tags(static_cast<UINT64>(tag1), static_cast<UINT64>(tag2));
    }
}

bool RenderTarget::TryFlush([Out] Direct2D1::Tags % tags, [Out] ErrorCode % errorCode)
{
    D2D1_TAG tag1, tag2;

    HRESULT hr = CastInterface<ID2D1RenderTarget>()->Flush(&tag1, &tag2);

    errorCode = static_cast<ErrorCode>(hr);
    tags = Direct2D1::Tags(tag1, tag2);
    
    return SUCCEEDED(hr) ? true : false;

}

void
RenderTarget::SaveDrawingState(
    DrawingStateBlock ^ drawingStateBlock
    )
{
    CastInterface<ID2D1RenderTarget>()->SaveDrawingState(
        drawingStateBlock->CastInterface<ID2D1DrawingStateBlock>()
        );
}

void
RenderTarget::RestoreDrawingState(
    DrawingStateBlock ^ drawingStateBlock
    )
{
    CastInterface<ID2D1RenderTarget>()->RestoreDrawingState(
        drawingStateBlock->CastInterface<ID2D1DrawingStateBlock>()
        );
}

void
RenderTarget::PushAxisAlignedClip(
    RectF clipRect,
    Direct2D1::AntiAliasMode antiAliasMode
    )
{
    D2D1_RECT_F tempRect;
    clipRect.CopyTo(&tempRect);

    CastInterface<ID2D1RenderTarget>()->PushAxisAlignedClip(
        &tempRect,
        static_cast<D2D1_ANTIALIAS_MODE>(antiAliasMode)
        );
}

void
RenderTarget::PopAxisAlignedClip(
    )
{
    CastInterface<ID2D1RenderTarget>()->PopAxisAlignedClip(
        );
}

void
RenderTarget::Clear(
    ColorF clearColor
    )
{
    D2D1_COLOR_F tempColor;
    clearColor.CopyTo(&tempColor);

    CastInterface<ID2D1RenderTarget>()->Clear(
        &tempColor
        );
}

void
RenderTarget::Clear(
    )
{

    CastInterface<ID2D1RenderTarget>()->Clear(
        NULL
        );
}

void
RenderTarget::BeginDraw(
    )
{
        CastInterface<ID2D1RenderTarget>()->BeginDraw(
            );
}

Direct2D1::Tags
RenderTarget::EndDraw(
    )
{
    D2D1_TAG tag1, tag2;

    HRESULT hr = 
        CastInterface<ID2D1RenderTarget>()->EndDraw(
            &tag1,
            &tag2
            );

    if (!SUCCEEDED(hr))
    {
        Exception^ e = Validate::GetExceptionForHR(hr);

        throw gcnew Direct2DException(
            String::Format(CultureInfo::CurrentCulture,
                "EndDraw has failed with error: {0} Tags=({1},{2}).",
                e->Message, static_cast<UInt64>(tag1), static_cast<UInt64>(tag2)),
                e);
    }
    else
    {
        return Direct2D1::Tags(static_cast<UINT64>(tag1), static_cast<UINT64>(tag2));
    }
    
}

bool RenderTarget::TryEndDraw([Out] Direct2D1::Tags % tags, [Out] ErrorCode % errorCode)
{
    D2D1_TAG tag1, tag2;

    HRESULT hr = CastInterface<ID2D1RenderTarget>()->EndDraw(&tag1, &tag2);

    errorCode = static_cast<ErrorCode>(hr);
    tags = Direct2D1::Tags(tag1, tag2);
    
    return SUCCEEDED(hr) ? true : false;
}


Direct2D1::PixelFormat
RenderTarget::PixelFormat::get(
    )
{
    D2D1_PIXEL_FORMAT tempFormat;

    tempFormat =
        CastInterface<ID2D1RenderTarget>()->GetPixelFormat(
            );


    Direct2D1::PixelFormat returnFormat;
    returnFormat.CopyFrom(tempFormat);
    return returnFormat;
}

void
RenderTarget::Dpi::set(DpiF dpi)
{
    CastInterface<ID2D1RenderTarget>()->SetDpi(
        dpi.X,
        dpi.Y
        );
}

DpiF
RenderTarget::Dpi::get()
{
    FLOAT dpiX;
    FLOAT dpiY;

        CastInterface<ID2D1RenderTarget>()->GetDpi(
            &dpiX,
            &dpiY
            );

   return DpiF(dpiX, dpiY);

}

SizeF
RenderTarget::Size::get(
    )
{
    D2D1_SIZE_F tempSize;

    tempSize =
        CastInterface<ID2D1RenderTarget>()->GetSize(
            );


    SizeF returnSize;
    returnSize.CopyFrom(tempSize);
    return returnSize;
}

SizeU
RenderTarget::PixelSize::get(
    )
{
    D2D1_SIZE_U tempSize;

    tempSize =
        CastInterface<ID2D1RenderTarget>()->GetPixelSize(
            );


    SizeU returnSize;
    returnSize.CopyFrom(tempSize);
    return returnSize;
}

UINT32
RenderTarget::MaximumBitmapSize::get(
    )
{
    return CastInterface<ID2D1RenderTarget>()->GetMaximumBitmapSize();
}

Boolean
RenderTarget::IsSupported(
    RenderTargetProperties renderTargetProperties
    )
{
    D2D1_RENDER_TARGET_PROPERTIES tempProperties;
    renderTargetProperties.CopyTo(&tempProperties);

    return 
        CastInterface<ID2D1RenderTarget>()->IsSupported(
            &tempProperties
            ) != 0;
}

void RenderTarget::DrawText(
    String^ text,
    TextFormat^ textFormat,
    RectF layoutRect,
    Brush^ defaultForegroundBrush,
    DrawTextOptions options,
    MeasuringMode measuringMode)
{
    UINT strLen = text->Length;
    D2D1_RECT_F tempRect;
    layoutRect.CopyTo(&tempRect);

    IntPtr chars = Marshal::StringToCoTaskMemUni( text );
    try
    {
        CastInterface<ID2D1RenderTarget>()->DrawText(
            (WCHAR *)chars.ToPointer(),
            strLen,
            textFormat->CastInterface<IDWriteTextFormat>(),
            &tempRect,
            defaultForegroundBrush->CastInterface<ID2D1Brush>(),
            static_cast<D2D1_DRAW_TEXT_OPTIONS>(options),
            static_cast<DWRITE_MEASURING_MODE>(measuringMode));
    }
    finally
    {
        Marshal::FreeCoTaskMem(chars);
    }
}


void RenderTarget::DrawText(
    String^ text,
    TextFormat^ textFormat,
    RectF layoutRect,
    Brush^ defaultForegroundBrush,
    DrawTextOptions options
    )
{
    DrawText(text, textFormat, layoutRect, defaultForegroundBrush, options, MeasuringMode::Natural);
}

void RenderTarget::DrawText(
    String^ text,
    TextFormat^ textFormat,
    RectF layoutRect,
    Brush^ defaultForegroundBrush,    
    MeasuringMode measuringMode)
{
    DrawText(text, textFormat, layoutRect, defaultForegroundBrush, DrawTextOptions::None, measuringMode);
}

void RenderTarget::DrawText(
    String^ text,
    TextFormat^ textFormat,
    RectF layoutRect,
    Brush^ defaultForegroundBrush)
{
    DrawText(text, textFormat, layoutRect, defaultForegroundBrush, DrawTextOptions::None, MeasuringMode::Natural);
}

void RenderTarget::DrawGlyphRun(
    Point2F baselineOrigin,
    array<GlyphRun>^ glyphRun,
    Brush^ foregroundBrush,
    MeasuringMode measuringMode)
{
    if (Object::ReferenceEquals(glyphRun, nullptr))
    {
        throw gcnew ArgumentNullException("glyphRun");
    }

    if (Object::ReferenceEquals(foregroundBrush, nullptr))
    {
        throw gcnew ArgumentNullException("foregroundBrush");
    }

    D2D1_POINT_2F tempPoint;
    baselineOrigin.CopyTo(&tempPoint);

    DWRITE_GLYPH_RUN * pGlyphRun = new DWRITE_GLYPH_RUN[glyphRun->Length];
    for (int i = 0; i < glyphRun->Length; i++)
    {
        glyphRun[i].CopyTo(&pGlyphRun[i]); 
    }

    try
    {
        CastInterface<ID2D1RenderTarget>()->DrawGlyphRun(
            tempPoint,
            pGlyphRun,
            foregroundBrush->CastInterface<ID2D1Brush>(),
            static_cast<DWRITE_MEASURING_MODE>(measuringMode));
    }
    finally
    {
        if (glyphRun->Length > 0)
        {
            for (int i = 0; i < glyphRun->Length; i++)
            {
                delete [] pGlyphRun[i].glyphAdvances;
                delete [] pGlyphRun[i].glyphIndices;
                delete [] pGlyphRun[i].glyphOffsets;
            }
            
            delete [] pGlyphRun;
        }
    }
}

void RenderTarget::DrawGlyphRun(
    Point2F baselineOrigin,
    array<GlyphRun>^ glyphRun,
    Brush^ foregroundBrush)
{
    DrawGlyphRun(baselineOrigin, glyphRun, foregroundBrush, MeasuringMode::Natural);
}

RenderingParams^ RenderTarget::TextRenderingParams::get(void)
{
    IDWriteRenderingParams* renderingParams = NULL;
    CastInterface<ID2D1RenderTarget>()->GetTextRenderingParams(&renderingParams);

    return renderingParams ? gcnew RenderingParams(renderingParams) : nullptr;
}

void RenderTarget::TextRenderingParams::set(RenderingParams^ textRenderingParams)
{
    CastInterface<ID2D1RenderTarget>()->SetTextRenderingParams(
        textRenderingParams == nullptr ? NULL : textRenderingParams->CastInterface<IDWriteRenderingParams>()
        );
}

void RenderTarget::DrawTextLayout(
    Point2F origin,
    TextLayout^ textLayout,
    Brush^ defaultForegroundBrush,
    DrawTextOptions options)
{
    D2D1_POINT_2F tempPoint;
    origin.CopyTo(&tempPoint);

    CastInterface<ID2D1RenderTarget>()->DrawTextLayout(
        tempPoint, 
        textLayout->CastInterface<IDWriteTextLayout>(), 
        defaultForegroundBrush->CastInterface<ID2D1Brush>(),
        static_cast<D2D1_DRAW_TEXT_OPTIONS>(options));
}

void RenderTarget::DrawTextLayout(
    Point2F origin,
    TextLayout^ textLayout,
    Brush^ defaultForegroundBrush)
{
    D2D1_POINT_2F tempPoint;
    origin.CopyTo(&tempPoint);

    CastInterface<ID2D1RenderTarget>()->DrawTextLayout(
        tempPoint, 
        textLayout->CastInterface<IDWriteTextLayout>(), 
        defaultForegroundBrush->CastInterface<ID2D1Brush>());
}

BitmapRenderTarget::BitmapRenderTarget(
    ) : RenderTarget()
{
}

BitmapRenderTarget::BitmapRenderTarget(
    IUnknown *pInner
    ) : RenderTarget(pInner)
{
}

D2DBitmap^ BitmapRenderTarget::Bitmap::get(void)
{
    ID2D1Bitmap * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1BitmapRenderTarget>()->GetBitmap(
            &ptr
            ));

    return ptr ? gcnew D2DBitmap(ptr) : nullptr;
}

HwndRenderTarget::HwndRenderTarget(
    ) : RenderTarget()
{
}

HwndRenderTarget::HwndRenderTarget(
    IUnknown *pInner
    ) : RenderTarget(pInner)
{
}

Boolean HwndRenderTarget::IsOccluded::get()
{
    if (CastInterface<ID2D1HwndRenderTarget>()->CheckWindowState() & D2D1_WINDOW_STATE_OCCLUDED)
        return true;
    else
        return false;
}

WindowState
HwndRenderTarget::CheckWindowState(
    )
{
    return static_cast<WindowState>(CastInterface<ID2D1HwndRenderTarget>()->CheckWindowState());
}

void
HwndRenderTarget::Resize(
    SizeU pixelSize
    )
{
    D2D1_SIZE_U tempSize;
    pixelSize.CopyTo(&tempSize);

    Validate::VerifyResult(
        CastInterface<ID2D1HwndRenderTarget>()->Resize(
            &tempSize
            ));
}

IntPtr
HwndRenderTarget::WindowHandle::get(
    )
{
    return IntPtr(CastInterface<ID2D1HwndRenderTarget>()->GetHwnd());
}

GdiInteropRenderTarget::GdiInteropRenderTarget(
    ) : DirectUnknown()
{
}

GdiInteropRenderTarget::GdiInteropRenderTarget(
    IUnknown *pInner
    ) : DirectUnknown(pInner)
{
}

IntPtr
GdiInteropRenderTarget::GetDC(
    DCInitializeMode mode
    )
{

    HDC hdc;
    Validate::VerifyResult(
        CastInterface<ID2D1GdiInteropRenderTarget>()->GetDC(
            static_cast<D2D1_DC_INITIALIZE_MODE>(mode),
            &hdc
            ));

    return IntPtr(hdc);
}

void
GdiInteropRenderTarget::ReleaseDC(
    Rect updaterect
    )
{
    RECT tempRect;
    updaterect.CopyTo(&tempRect);

    Validate::VerifyResult(
        CastInterface<ID2D1GdiInteropRenderTarget>()->ReleaseDC(
            &tempRect
            ));
}

void
GdiInteropRenderTarget::ReleaseDC(
    )
{
    Validate::VerifyResult(
        CastInterface<ID2D1GdiInteropRenderTarget>()->ReleaseDC(
            NULL
            ));
}

DCRenderTarget::DCRenderTarget(
    ) : RenderTarget()
{
}

DCRenderTarget::DCRenderTarget(
    IUnknown *pInner
    ) : RenderTarget(pInner)
{
}

void
DCRenderTarget::BindDC(
    IntPtr hDC,
    Rect pSubRect
    )
{
    RECT tempRect;
    pSubRect.CopyTo(&tempRect);

    Validate::VerifyResult(
        CastInterface<ID2D1DCRenderTarget>()->BindDC(
            static_cast<HDC>(hDC.ToPointer()),
            &tempRect
            ));
}

StrokeStyle::StrokeStyle(
    ) : D2DResource()
{
}

StrokeStyle::StrokeStyle(
    IUnknown *pInner
    ) : D2DResource(pInner)
{
}

CapStyle
StrokeStyle::StartCap::get(
    )
{
    return static_cast<CapStyle>(CastInterface<ID2D1StrokeStyle>()->GetStartCap());
}

CapStyle
StrokeStyle::EndCap::get(
    )
{
    return static_cast<CapStyle>(CastInterface<ID2D1StrokeStyle>()->GetEndCap());
}

CapStyle
StrokeStyle::DashCap::get(
    )
{
    return static_cast<CapStyle>(CastInterface<ID2D1StrokeStyle>()->GetDashCap());
}

FLOAT
StrokeStyle::MiterLimit::get(
    )
{
    return CastInterface<ID2D1StrokeStyle>()->GetMiterLimit();
}

Direct2D1::LineJoin
StrokeStyle::LineJoin::get(
    )
{
    return static_cast<Direct2D1::LineJoin>(CastInterface<ID2D1StrokeStyle>()->GetLineJoin());
}

FLOAT
StrokeStyle::DashOffset::get(
    )
{
    return CastInterface<ID2D1StrokeStyle>()->GetDashOffset();
}

Direct2D1::DashStyle
StrokeStyle::DashStyle::get(
    )
{
    return static_cast<Direct2D1::DashStyle>(CastInterface<ID2D1StrokeStyle>()->GetDashStyle());
}

UINT32
StrokeStyle::DashesCount::get(
    )
{
    return CastInterface<ID2D1StrokeStyle>()->GetDashesCount();
}

IEnumerable<FLOAT>^ StrokeStyle::Dashes::get(void)
{
    array<FLOAT> ^dashes = nullptr;

    UINT count = CastInterface<ID2D1StrokeStyle>()->GetDashesCount();;

    dashes = gcnew array<FLOAT>(count);

    pin_ptr<FLOAT> tempDashes = &dashes[0];

    CastInterface<ID2D1StrokeStyle>()->GetDashes(
        tempDashes,
        dashes->Length
        );

    return Array::AsReadOnly(dashes);
}

Geometry::Geometry(
    ) : D2DResource()
{
}

Geometry::Geometry(
    IUnknown *pInner
    ) : D2DResource(pInner)
{
}

RectF
Geometry::GetBounds(
    Matrix3x2F worldTransform
    )
{
    D2D1_MATRIX_3X2_F tempTransform;
    worldTransform.CopyTo(&tempTransform);

    D2D1_RECT_F tempRect;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->GetBounds(
            &tempTransform,
            &tempRect
            ));

    RectF bounds;
    bounds.CopyFrom(tempRect);

    return bounds;

}

RectF
Geometry::GetBounds(
    )
{
    D2D1_RECT_F tempRect;
    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->GetBounds(
            NULL,
            &tempRect
            ));

    RectF bounds;
    bounds.CopyFrom(tempRect);

    return bounds;

}
RectF
Geometry::GetWidenedBounds(
    FLOAT strokeWidth,
    StrokeStyle ^ strokeStyle,
    FLOAT flatteningTolerance,
    Matrix3x2F worldTransform
    )
{
    D2D1_MATRIX_3X2_F tempTransform;
    worldTransform.CopyTo(&tempTransform);
    D2D1_RECT_F tempRect;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->GetWidenedBounds(
            strokeWidth,
            strokeStyle ? strokeStyle->CastInterface<ID2D1StrokeStyle>() : NULL,
            &tempTransform,
            flatteningTolerance,
            &tempRect
            ));

    RectF bounds;
    bounds.CopyFrom(tempRect);
    return bounds;

}

RectF
Geometry::GetWidenedBounds(
    FLOAT strokeWidth,
    StrokeStyle ^ strokeStyle,
    FLOAT flatteningTolerance
    )
{
    D2D1_RECT_F tempRect;
    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->GetWidenedBounds(
            strokeWidth,
            strokeStyle ? strokeStyle->CastInterface<ID2D1StrokeStyle>() : NULL,
            NULL,
            flatteningTolerance,
            &tempRect
            ));

    RectF bounds;
    bounds.CopyFrom(tempRect);
    return bounds;

}


RectF
Geometry::GetWidenedBounds(
    FLOAT strokeWidth,
    StrokeStyle ^ strokeStyle
    )
{
    D2D1_RECT_F tempRect;
    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->GetWidenedBounds(
            strokeWidth,
            strokeStyle ? strokeStyle->CastInterface<ID2D1StrokeStyle>() : NULL,
            NULL,
            &tempRect
            ));

    RectF bounds;
    bounds.CopyFrom(tempRect);
    return bounds;
}

Boolean
Geometry::StrokeContainsPoint(
    Point2F point,
    FLOAT strokeWidth,
    StrokeStyle ^ strokeStyle,
    FLOAT flatteningTolerance,
    Matrix3x2F worldTransform
    )
{
    D2D1_POINT_2F tempPoint;
    point.CopyTo(&tempPoint);
    D2D1_MATRIX_3X2_F tempTransform;
    worldTransform.CopyTo(&tempTransform);
    BOOL contains;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->StrokeContainsPoint(
            tempPoint,
            strokeWidth,
            strokeStyle ? strokeStyle->CastInterface<ID2D1StrokeStyle>() : NULL,
            &tempTransform,
            flatteningTolerance,
            &contains
            ));
    return contains != 0;
}

Boolean
Geometry::StrokeContainsPoint(
    Point2F point,
    FLOAT strokeWidth,
    StrokeStyle ^ strokeStyle,
    FLOAT flatteningTolerance
    )
{
    D2D1_POINT_2F tempPoint;
    point.CopyTo(&tempPoint);
    BOOL contains;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->StrokeContainsPoint(
            tempPoint,
            strokeWidth,
            strokeStyle ? strokeStyle->CastInterface<ID2D1StrokeStyle>() : NULL,
            NULL,
            flatteningTolerance,
            &contains
            ));

    return contains != 0;
}

Boolean
Geometry::StrokeContainsPoint(
    Point2F point,
    FLOAT strokeWidth,
    StrokeStyle ^ strokeStyle
    )
{
    D2D1_POINT_2F tempPoint;
    point.CopyTo(&tempPoint);
    BOOL contains;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->StrokeContainsPoint(
            tempPoint,
            strokeWidth,
            strokeStyle ? strokeStyle->CastInterface<ID2D1StrokeStyle>() : NULL,
            NULL,
            &contains
            ));

    return contains != 0;
}

Boolean
Geometry::FillContainsPoint(
    Point2F point,
    FLOAT flatteningTolerance,
    Matrix3x2F worldTransform
    )
{
    D2D1_POINT_2F tempPoint;
    point.CopyTo(&tempPoint);
    D2D1_MATRIX_3X2_F tempTransform;
    worldTransform.CopyTo(&tempTransform);
    BOOL contains;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->FillContainsPoint(
            tempPoint,
            &tempTransform,
            flatteningTolerance,
            &contains
            ));


    return contains != 0;
}

Boolean
Geometry::FillContainsPoint(
    Point2F point,
    FLOAT flatteningTolerance
    )
{
    D2D1_POINT_2F tempPoint;
    point.CopyTo(&tempPoint);

    BOOL contains;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->FillContainsPoint(
            tempPoint,
            NULL,
            flatteningTolerance,
            &contains
            ));


    return contains != 0;
}

Boolean
Geometry::FillContainsPoint(
    Point2F point
    )
{
    D2D1_POINT_2F tempPoint;
    point.CopyTo(&tempPoint);

    BOOL contains;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->FillContainsPoint(
            tempPoint,
            NULL,
            &contains
            ));


    return contains != 0;
}

GeometryRelation
Geometry::CompareWithGeometry(
    Geometry ^ inputGeometry,
    FLOAT flatteningTolerance,
    Matrix3x2F inputGeometryTransform
    )
{
    D2D1_MATRIX_3X2_F tempTransform;
    inputGeometryTransform.CopyTo(&tempTransform);
    GeometryRelation relation;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->CompareWithGeometry(
            inputGeometry->CastInterface<ID2D1Geometry>(),
            &tempTransform,
            flatteningTolerance,
            reinterpret_cast<D2D1_GEOMETRY_RELATION *>(static_cast<GeometryRelation *>(&relation))
            ));


    return relation;
}

GeometryRelation
Geometry::CompareWithGeometry(
    Geometry ^ inputGeometry,
    FLOAT flatteningTolerance
    )
{
    D2D1_GEOMETRY_RELATION relation;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->CompareWithGeometry(
            inputGeometry->CastInterface<ID2D1Geometry>(),
            NULL,
            flatteningTolerance,
            &relation
            ));


    return static_cast<GeometryRelation>(relation);
}

GeometryRelation
Geometry::CompareWithGeometry(
    Geometry ^ inputGeometry
    )
{
    D2D1_GEOMETRY_RELATION relation;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->CompareWithGeometry(
            inputGeometry->CastInterface<ID2D1Geometry>(),
            NULL,
            &relation
            ));

    return static_cast<GeometryRelation>(relation);
}

FLOAT
Geometry::ComputeArea(
    FLOAT flatteningTolerance,
    Matrix3x2F worldTransform
    )
{
    D2D1_MATRIX_3X2_F tempTransform;
    worldTransform.CopyTo(&tempTransform);
    
    FLOAT tempArea;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->ComputeArea(
            &tempTransform,
            flatteningTolerance,
            &tempArea
            ));

   return tempArea;
}

FLOAT
Geometry::ComputeArea(
    FLOAT flatteningTolerance
    )
{
    FLOAT tempArea;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->ComputeArea(
            NULL,
            flatteningTolerance,
            &tempArea
            ));

   return tempArea;
}

FLOAT
Geometry::ComputeArea(
    )
{
    FLOAT tempArea;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->ComputeArea(
            NULL,
            &tempArea
            ));

   return tempArea;
}

FLOAT
Geometry::ComputeLength(
    FLOAT flatteningTolerance,
    Matrix3x2F worldTransform
    )
{
    D2D1_MATRIX_3X2_F tempTransform;
    worldTransform.CopyTo(&tempTransform);

    FLOAT tempLength;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->ComputeLength(
            &tempTransform,
            flatteningTolerance,
            &tempLength
            ));

   return tempLength;

}

FLOAT
Geometry::ComputeLength(
    FLOAT flatteningTolerance
    )
{
    FLOAT tempLength;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->ComputeLength(
            NULL,
            flatteningTolerance,
            &tempLength
            ));

   return tempLength;
}

FLOAT
Geometry::ComputeLength(
    )
{
    FLOAT tempLength;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->ComputeLength(
            NULL,
            &tempLength
            ));

   return tempLength;
}

PointAndTangent Geometry::ComputePointAtLength(
    FLOAT length, FLOAT flatteningTolerance, Matrix3x2F worldTransform)
{
    D2D1_MATRIX_3X2_F tempTransform;
    worldTransform.CopyTo(&tempTransform);

    D2D1_POINT_2F tempPoint;
    D2D1_POINT_2F tempVector;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->ComputePointAtLength(
            length,
            &tempTransform,
            flatteningTolerance,
            &tempPoint,
            &tempVector
            ));

    Point2F point, transform;

    transform.CopyFrom(tempVector);
    point.CopyFrom(tempPoint);

    return PointAndTangent(point, transform);
}

PointAndTangent Geometry::ComputePointAtLength(FLOAT length, FLOAT flatteningTolerance)
{
    D2D1_POINT_2F tempPoint;
    D2D1_POINT_2F tempVector;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->ComputePointAtLength(
            length,
            NULL,
            flatteningTolerance,
            &tempPoint,
            &tempVector
            ));

    Point2F point, tangent;

    tangent.CopyFrom(tempVector);
    point.CopyFrom(tempPoint);

    return PointAndTangent(point, tangent);
}

PointAndTangent Geometry::ComputePointAtLength(FLOAT length)
{
    D2D1_POINT_2F tempPoint;
    D2D1_POINT_2F tempVector;

    Validate::VerifyResult(
        CastInterface<ID2D1Geometry>()->ComputePointAtLength(
            length,
            NULL,
            &tempPoint,
            &tempVector
            ));

    Point2F point, tangent;

    tangent.CopyFrom(tempVector);
    point.CopyFrom(tempPoint);

    return PointAndTangent(point, tangent);
}

RectangleGeometry::RectangleGeometry(
    ) : Geometry()
{
}

RectangleGeometry::RectangleGeometry(
    IUnknown *pInner
    ) : Geometry(pInner)
{
}

RectF
RectangleGeometry::Rectangle::get(
    )
{
    D2D1_RECT_F tempRect;

    CastInterface<ID2D1RectangleGeometry>()->GetRect(
        &tempRect
        );

    RectF rect;
    rect.CopyFrom(tempRect);
    return rect;

}

RoundedRectangleGeometry::RoundedRectangleGeometry(
    ) : Geometry()
{
}

RoundedRectangleGeometry::RoundedRectangleGeometry(
    IUnknown *pInner
    ) : Geometry(pInner)
{
}

Direct2D1::RoundedRect
RoundedRectangleGeometry::RoundedRectangle::get(    
    )
{
    D2D1_ROUNDED_RECT tempRect;

    CastInterface<ID2D1RoundedRectangleGeometry>()->GetRoundedRect(
        &tempRect
        );

    Direct2D1::RoundedRect roundedRect;
    roundedRect.CopyFrom(tempRect);
    return roundedRect;

}

EllipseGeometry::EllipseGeometry(
    ) : Geometry()
{
}

EllipseGeometry::EllipseGeometry(
    IUnknown *pInner
    ) : Geometry(pInner)
{
}

Direct2D1::Ellipse
EllipseGeometry::Ellipse::get(
    )
{
    D2D1_ELLIPSE tempEllipse;

    CastInterface<ID2D1EllipseGeometry>()->GetEllipse(
        &tempEllipse
        );

    Direct2D1::Ellipse ellipse;
    ellipse.CopyFrom(tempEllipse);
    return ellipse;
}

SimplifiedGeometrySink::SimplifiedGeometrySink(
    ) : DirectUnknown()
{
}

SimplifiedGeometrySink::SimplifiedGeometrySink(
    IUnknown *pInner
    ) : DirectUnknown(pInner)
{
}

void
SimplifiedGeometrySink::SetFillMode(
    FillMode fillMode
    )
{

    CastInterface<ID2D1SimplifiedGeometrySink>()->SetFillMode(
        static_cast<D2D1_FILL_MODE>(fillMode)
        );
}

void
SimplifiedGeometrySink::SetSegmentFlags(
    PathSegmentOptions vertexOptions
    )
{
    CastInterface<ID2D1SimplifiedGeometrySink>()->SetSegmentFlags(
        static_cast<D2D1_PATH_SEGMENT>(vertexOptions)
        );
}

void
SimplifiedGeometrySink::BeginFigure(
    Point2F startPoint,
    FigureBegin figureBegin
    )
{
    D2D1_POINT_2F tempPoint;
    startPoint.CopyTo(&tempPoint);

    CastInterface<ID2D1SimplifiedGeometrySink>()->BeginFigure(
        tempPoint,
        static_cast<D2D1_FIGURE_BEGIN>(figureBegin)
        );
}

void
SimplifiedGeometrySink::AddLines(
    IEnumerable<Point2F> ^ points
    )
{
    if (Object::ReferenceEquals(points, nullptr))
    {
        throw gcnew ArgumentNullException("points");
    }

    vector<D2D1_POINT_2F> pointsVector;
    
    for each(Point2F point in points)
    {
        pointsVector.push_back(D2D1_POINT_2F());
        point.CopyTo(&pointsVector.back());
    }
    
    if (pointsVector.size() == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "points");
    }

    CastInterface<ID2D1SimplifiedGeometrySink>()->AddLines(
        &pointsVector[0], static_cast<UINT>(pointsVector.size()));
}

void
SimplifiedGeometrySink::AddBeziers(
    IEnumerable<BezierSegment> ^ beziers
    )
{
    if (Object::ReferenceEquals(beziers, nullptr))
    {
        throw gcnew ArgumentNullException("beziers");
    }

    vector<D2D1_BEZIER_SEGMENT> beziersVector;
    
    for each(BezierSegment bezier in beziers)
    {
        beziersVector.push_back(D2D1_BEZIER_SEGMENT());
        bezier.CopyTo(&beziersVector.back());
    }

    if (beziersVector.size() == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "beziers");
    }

    CastInterface<ID2D1SimplifiedGeometrySink>()->AddBeziers(
        &beziersVector[0], static_cast<UINT>(beziersVector.size()));
}

void
SimplifiedGeometrySink::EndFigure(
    FigureEnd figureEnd
    )
{
    CastInterface<ID2D1SimplifiedGeometrySink>()->EndFigure(
        static_cast<D2D1_FIGURE_END>(figureEnd)
        );
}

void
SimplifiedGeometrySink::Close(
    )
{

Validate::VerifyResult(
    CastInterface<ID2D1SimplifiedGeometrySink>()->Close(
    ));
}

GeometrySink::GeometrySink(
    ) : SimplifiedGeometrySink()
{
}

GeometrySink::GeometrySink(
    IUnknown *pInner
    ) : SimplifiedGeometrySink(pInner)
{
}

void
GeometrySink::AddLine(
    Point2F point
    )
{
    D2D1_POINT_2F tempPoint;
    point.CopyTo(&tempPoint);

    CastInterface<ID2D1GeometrySink>()->AddLine(
        tempPoint
        );
}

void
GeometrySink::AddBezier(
    BezierSegment bezier
    )
{
    D2D1_BEZIER_SEGMENT tempBezier;
    bezier.CopyTo(&tempBezier);

    CastInterface<ID2D1GeometrySink>()->AddBezier(
        &tempBezier
        );
}

void
GeometrySink::AddQuadraticBezier(
    QuadraticBezierSegment bezier
    )
{
    D2D1_QUADRATIC_BEZIER_SEGMENT tempBezier;
    bezier.CopyTo(&tempBezier);

    CastInterface<ID2D1GeometrySink>()->AddQuadraticBezier(
        &tempBezier
        );
}

void
GeometrySink::AddQuadraticBeziers(
    IEnumerable<QuadraticBezierSegment> ^ beziers
    )
{
    if (Object::ReferenceEquals(beziers, nullptr))
    {
        throw gcnew ArgumentNullException("beziers");
    }

    vector<D2D1_QUADRATIC_BEZIER_SEGMENT> beziersVector;
    
    for each(QuadraticBezierSegment bezier in beziers)
    {
        beziersVector.push_back(D2D1_QUADRATIC_BEZIER_SEGMENT());
        bezier.CopyTo(&beziersVector.back());
    }

    if (beziersVector.size() == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "beziers");
    }

    CastInterface<ID2D1GeometrySink>()->AddQuadraticBeziers(
        &beziersVector[0], static_cast<UINT>(beziersVector.size()));
}

void
GeometrySink::AddArc(
    ArcSegment arc
    )
{
    D2D1_ARC_SEGMENT tempArc;
    arc.CopyTo(&tempArc);

    CastInterface<ID2D1GeometrySink>()->AddArc(
        &tempArc
        );
}

TessellationSink::TessellationSink(
    ) : DirectUnknown()
{
}

TessellationSink::TessellationSink(
    IUnknown *pInner
    ) : DirectUnknown(pInner)
{
}

void
TessellationSink::AddTriangles(
    IEnumerable<Triangle> ^ triangles
    )
{
    if (Object::ReferenceEquals(triangles, nullptr))
    {
        throw gcnew ArgumentNullException("triangles");
    }

    vector<D2D1_TRIANGLE> trianglesVector;
    
    for each(Triangle triangle in triangles)
    {
        trianglesVector.push_back(D2D1_TRIANGLE());
        triangle.CopyTo(&trianglesVector.back());
    }

    if (trianglesVector.size() == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "triangles");
    }

    CastInterface<ID2D1TessellationSink>()->AddTriangles(
        &trianglesVector[0], static_cast<UINT>(trianglesVector.size()));
}

void
TessellationSink::Close(
    )
{
    Validate::VerifyResult(
        CastInterface<ID2D1TessellationSink>()->Close(
        ));
}

PathGeometry::PathGeometry(
    ) : Geometry()
{
}

PathGeometry::PathGeometry(
    IUnknown *pInner
    ) : Geometry(pInner)
{
}

GeometrySink ^
PathGeometry::Open(
    )
{
    ID2D1GeometrySink * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1PathGeometry>()->Open(
            &ptr
            ));

    return ptr ? gcnew GeometrySink(ptr) : nullptr;
}

UINT32
PathGeometry::SegmentCount::get(
    )
{
    UINT32 count;

    Validate::VerifyResult(
        CastInterface<ID2D1PathGeometry>()->GetSegmentCount(
            &count
            ));


    return count;
}

UINT32
PathGeometry::FigureCount::get(
    )
{
    UINT32 count;

    Validate::VerifyResult(
        CastInterface<ID2D1PathGeometry>()->GetFigureCount(
            &count
            ));


    return count;
}

GeometryGroup::GeometryGroup(
    ) : Geometry()
{
}

GeometryGroup::GeometryGroup(
    IUnknown *pInner
    ) : Geometry(pInner)
{
}

Direct2D1::FillMode
GeometryGroup::FillMode::get(
    )
{
    return static_cast<Direct2D1::FillMode>(CastInterface<ID2D1GeometryGroup>()->GetFillMode());
}

UINT32
GeometryGroup::SourceGeometryCount::get(
    )
{
    return CastInterface<ID2D1GeometryGroup>()->GetSourceGeometryCount();
}

ReadOnlyCollection<Geometry ^>^ GeometryGroup::Geometries::get(void)
{
    UINT count = CastInterface<ID2D1GeometryGroup>()->GetSourceGeometryCount();
    vector<ID2D1Geometry*> sourcesVector(count);

    if (count > 0)
    {
        CastInterface<ID2D1GeometryGroup>()->GetSourceGeometries(
            &sourcesVector[0],
            count
            );
    }

    return Utilities::Convert::GetCollection<Geometry, ID2D1Geometry>(count, sourcesVector);
}

TransformedGeometry::TransformedGeometry(
    ) : Geometry()
{
}

TransformedGeometry::TransformedGeometry(
    IUnknown *pInner
    ) : Geometry(pInner)
{
}

Geometry^ TransformedGeometry::Source::get(void)
{
    ID2D1Geometry * ptr = NULL;

    CastInterface<ID2D1TransformedGeometry>()->GetSourceGeometry(&ptr);

    return ptr != NULL ? gcnew Geometry(ptr) : nullptr;
}

Matrix3x2F
TransformedGeometry::Transform::get(
    )
{
    D2D1_MATRIX_3X2_F tempTransform;

        CastInterface<ID2D1TransformedGeometry>()->GetTransform(
            &tempTransform
            );

    Matrix3x2F transform;
    transform.CopyFrom(tempTransform);

    return transform;

}

Mesh::Mesh(
    ) : D2DResource()
{
}

Mesh::Mesh(
    IUnknown *pInner
    ) : D2DResource(pInner)
{
}

TessellationSink ^
Mesh::Open(
    )
{
    ID2D1TessellationSink * ptr = NULL;
    Validate::VerifyResult(CastInterface<ID2D1Mesh>()->Open(&ptr));
    return ptr ? gcnew TessellationSink(ptr) : nullptr;
}

Layer::Layer(
    ) : D2DResource()
{
}

Layer::Layer(
    IUnknown *pInner
    ) : D2DResource(pInner)
{
}

SizeF
Layer::Size::get(
    )
{
    D2D1_SIZE_F tempSize;

    tempSize =
        CastInterface<ID2D1Layer>()->GetSize(
            );

    SizeF returnSize;
    returnSize.CopyFrom(tempSize);
    return returnSize;
}

D2DFactory::D2DFactory(
    ) : DirectUnknown()
{
}

D2DFactory::D2DFactory(
    IUnknown *pInner
    ) : DirectUnknown(pInner)
{
}

void
D2DFactory::ReloadSystemMetrics(
    )
{
    Validate::VerifyResult(
        CastInterface<ID2D1Factory>()->ReloadSystemMetrics(
            ));
}

DpiF
D2DFactory::DesktopDpi::get(
    )
{
    FLOAT dpiX;
    FLOAT dpiY;

    CastInterface<ID2D1Factory>()->GetDesktopDpi(
        &dpiX,
        &dpiY
        );

   return DpiF(dpiX, dpiY);

}

RectangleGeometry ^
D2DFactory::CreateRectangleGeometry(
    RectF rectangle
    )
{
    D2D1_RECT_F tempRect;
    rectangle.CopyTo(&tempRect);

    ID2D1RectangleGeometry * ptr = NULL;

    Validate::VerifyResult(
        CastInterface<ID2D1Factory>()->CreateRectangleGeometry(
            &tempRect,
            &ptr
            ));

    return ptr ? gcnew RectangleGeometry(ptr) : nullptr;
}

RoundedRectangleGeometry ^
D2DFactory::CreateRoundedRectangleGeometry(
    RoundedRect roundedRectangle
    )
{
    D2D1_ROUNDED_RECT tempRectangle;
    roundedRectangle.CopyTo(&tempRectangle);

    ID2D1RoundedRectangleGeometry * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1Factory>()->CreateRoundedRectangleGeometry(
            &tempRectangle,
            &ptr
            ));

    return ptr ? gcnew RoundedRectangleGeometry(ptr) : nullptr;
}

EllipseGeometry ^
D2DFactory::CreateEllipseGeometry(
    Ellipse ellipse
    )
{
    D2D1_ELLIPSE tempEllipse;
    ellipse.CopyTo(&tempEllipse);

    ID2D1EllipseGeometry * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1Factory>()->CreateEllipseGeometry(
            &tempEllipse,
            &ptr
            ));

    return ptr ? gcnew EllipseGeometry(ptr): nullptr;
}

GeometryGroup ^
D2DFactory::CreateGeometryGroup(
    FillMode fillMode,
    IEnumerable<Geometry ^> ^ geometries
    )
{
    if (Object::ReferenceEquals(geometries, nullptr))
    {
        throw gcnew ArgumentNullException("geometries");
    }

    vector<ID2D1Geometry*> geometryVector;
    UINT count = Utilities::Convert::FillIUnknownsVector<Geometry,ID2D1Geometry>(geometries, geometryVector);

    if (count == 0)
    {
        throw gcnew ArgumentException("Enumeration must be non-empty", "geometries");
    }

    ID2D1GeometryGroup * ptr = NULL;
    
    Validate::VerifyResult(
        CastInterface<ID2D1Factory>()->CreateGeometryGroup(
            static_cast<D2D1_FILL_MODE>(fillMode), &geometryVector[0], count, &ptr ));

    return ptr != NULL ? gcnew GeometryGroup(ptr): nullptr;
}

TransformedGeometry ^
D2DFactory::CreateTransformedGeometry(
    Geometry ^ sourceGeometry,
    Matrix3x2F transform
    )
{
    D2D1_MATRIX_3X2_F tempTransform;
    transform.CopyTo(&tempTransform);

    ID2D1TransformedGeometry * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1Factory>()->CreateTransformedGeometry(
            sourceGeometry->CastInterface<ID2D1Geometry>(),
            &tempTransform,
            &ptr
            ));

    return ptr ? gcnew TransformedGeometry(ptr): nullptr;
}

PathGeometry ^
D2DFactory::CreatePathGeometry(
    )
{
    ID2D1PathGeometry * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1Factory>()->CreatePathGeometry(
            &ptr
            ));

    return ptr ? gcnew PathGeometry(ptr): nullptr;
}

StrokeStyle ^
D2DFactory::CreateStrokeStyle(
    StrokeStyleProperties strokeStyleProperties,
    cli::array<FLOAT> ^ dashes
    )
{
    D2D1_STROKE_STYLE_PROPERTIES tempProperties;
    strokeStyleProperties.CopyTo(&tempProperties);
    FLOAT *tempDashes = NULL;
    pin_ptr<FLOAT> dashesInner = dashes ? &dashes[0] : nullptr;
    tempDashes = static_cast<::FLOAT *>(dashesInner);

    ID2D1StrokeStyle * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1Factory>()->CreateStrokeStyle(
            &tempProperties,
            dashes ? tempDashes : NULL,
            dashes ? dashes->Length : 0,
            &ptr
            ));

    return ptr ? gcnew StrokeStyle(ptr): nullptr;
}

StrokeStyle ^
D2DFactory::CreateStrokeStyle(
    StrokeStyleProperties strokeStyleProperties
    )
{
    return CreateStrokeStyle(strokeStyleProperties, nullptr);
}
RenderTarget ^
D2DFactory::CreateWicBitmapRenderTarget(
    ImagingBitmap^ target,
    RenderTargetProperties renderTargetProperties
    )
{
    D2D1_RENDER_TARGET_PROPERTIES tempProperties;
    renderTargetProperties.CopyTo(&tempProperties);

    ID2D1RenderTarget * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1Factory>()->CreateWicBitmapRenderTarget(
            target->CastInterface<IWICBitmap>(),
            &tempProperties,
            &ptr
            ));

    return ptr ? gcnew RenderTarget(ptr): nullptr;
}

HwndRenderTarget ^
D2DFactory::CreateHwndRenderTarget(
    RenderTargetProperties renderTargetProperties,
    HwndRenderTargetProperties hwndRenderTargetProperties
    )
{
    D2D1_RENDER_TARGET_PROPERTIES tempTargetProperties;
    D2D1_HWND_RENDER_TARGET_PROPERTIES tempHwndProperties;

    renderTargetProperties.CopyTo(&tempTargetProperties);
    hwndRenderTargetProperties.CopyTo(&tempHwndProperties);

    ID2D1HwndRenderTarget * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1Factory>()->CreateHwndRenderTarget(
            &tempTargetProperties,
            &tempHwndProperties,
            &ptr
            ));

    return ptr ? gcnew HwndRenderTarget(ptr): nullptr;
}

RenderTarget ^
D2DFactory::CreateGraphicsSurfaceRenderTarget(
    Surface^ surface,
    RenderTargetProperties renderTargetProperties
    )
{
    D2D1_RENDER_TARGET_PROPERTIES tempProperties;
    renderTargetProperties.CopyTo(&tempProperties);

    ID2D1RenderTarget * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1Factory>()->CreateDxgiSurfaceRenderTarget(
            surface->CastInterface<IDXGISurface>(),
            &tempProperties,
            &ptr
            ));

    return ptr ? gcnew RenderTarget(ptr): nullptr;
}

DCRenderTarget ^
D2DFactory::CreateDCRenderTarget(
    RenderTargetProperties renderTargetProperties
    )
{
    D2D1_RENDER_TARGET_PROPERTIES tempProperties;
    renderTargetProperties.CopyTo(&tempProperties);

    ID2D1DCRenderTarget * ptr = NULL;
    Validate::VerifyResult(
        CastInterface<ID2D1Factory>()->CreateDCRenderTarget(
            &tempProperties,
            &ptr
            ));

    return ptr ? gcnew DCRenderTarget(ptr): nullptr;
}

} } } }

