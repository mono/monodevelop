// Copyright (c) Microsoft Corporation.  All rights reserved.

//+----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
// File name:
//      GeometrySink.cpp
//
// Description: 
//      Implements native geometry sink wrapper interfaces that call into a 
//      wrapper implementation of a managed interface supplied by the caller.
//
//-----------------------------------------------------------------------------
#include "stdafx.h"
#include "D2DGeometrySink.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct2D1 {


#define STANDARD_PROLOGUE       \
    if (SUCCEEDED(m_hr))        \
    {                           \
        try                     \
        {

#define STANDARD_CATCH_BLOCKS       \
    catch(System::Exception^)     \
    {                               \
        m_hr = E_FAIL;              \
    }                               \


#define STANDARD_EPILOGUE               \
        }                               \
        STANDARD_CATCH_BLOCKS           \
    }                                   \


GeometrySinkCallback::GeometrySinkCallback(
    ISimplifiedGeometrySink ^sink
    ) : m_hr(S_OK),
        m_sink(sink),
        m_cRef(1)
{
}


STDMETHODIMP
GeometrySinkCallback::QueryInterface(
    __in REFIID riid,
    __out void **ppv
    )
{
    if (ppv == NULL)
    {
        return E_INVALIDARG;
    }

    void *pv = NULL;

    if (riid == __uuidof(IUnknown))
    {
        pv = static_cast<IUnknown *>(this);
    }
    else if (riid == __uuidof(ID2D1SimplifiedGeometrySink))
    {
        pv = static_cast<ID2D1SimplifiedGeometrySink *>(this);
    }
    else if (riid == __uuidof(ID2D1GeometrySink))
    {
        pv = static_cast<ID2D1GeometrySink *>(this);
    }

    if (pv == NULL)
    {
        return E_NOINTERFACE;
    }

    AddRef();

    *ppv = pv;

    return S_OK;
}

STDMETHODIMP_(ULONG)
GeometrySinkCallback::AddRef()
{
    return InterlockedIncrement(&m_cRef);
}

STDMETHODIMP_(ULONG)
GeometrySinkCallback::Release()
{
    LONG cRef = InterlockedDecrement(&m_cRef);

    if (cRef == 0)
    {
        delete this;
    }

    return cRef;
}

STDMETHODIMP_(void)
GeometrySinkCallback::SetFillMode(
    D2D1_FILL_MODE fillMode 
    )
{
    STANDARD_PROLOGUE

    m_sink->SetFillMode(static_cast<FillMode>(fillMode));

    STANDARD_EPILOGUE
}

STDMETHODIMP_(void)
GeometrySinkCallback::SetSegmentFlags(
    D2D1_PATH_SEGMENT vertexOptions 
    )
{
    STANDARD_PROLOGUE

    m_sink->SetSegmentFlags(static_cast<PathSegmentOptions>(vertexOptions));

    STANDARD_EPILOGUE
}

STDMETHODIMP_(void)
GeometrySinkCallback::BeginFigure(
    __in CONST D2D1_POINT_2F startPoint,
    D2D1_FIGURE_BEGIN figureBegin 
    ) 
{
    STANDARD_PROLOGUE

    Point2F copystartPoint;

    copystartPoint.CopyFrom(startPoint);

    m_sink->BeginFigure(copystartPoint, static_cast<FigureBegin>(figureBegin));

    STANDARD_EPILOGUE
}

STDMETHODIMP_(void)
GeometrySinkCallback::AddLines(
    __in_ecount(pointsLength) CONST D2D1_POINT_2F *points,
    UINT pointsLength 
    ) 
{
    STANDARD_PROLOGUE

    cli::array<Point2F> ^arrayCopy = GetArray<Point2F>(points, pointsLength);

    m_sink->AddLines(arrayCopy);

    STANDARD_EPILOGUE
}

STDMETHODIMP_(void)
GeometrySinkCallback::AddBeziers(
    __in_ecount(aBeziersLength) CONST D2D1_BEZIER_SEGMENT *aBeziers,
    UINT aBeziersLength 
    )
{
    STANDARD_PROLOGUE

    cli::array<BezierSegment> ^arrayCopy = GetArray<BezierSegment>(aBeziers, aBeziersLength);

    m_sink->AddBeziers(arrayCopy);

    STANDARD_EPILOGUE
}

STDMETHODIMP_(void)
GeometrySinkCallback::EndFigure(
    D2D1_FIGURE_END figureEnd 
    )
{
    STANDARD_PROLOGUE

    m_sink->EndFigure(static_cast<FigureEnd>(figureEnd));

    STANDARD_EPILOGUE
}

STDMETHODIMP
GeometrySinkCallback::Close()
{
    STANDARD_PROLOGUE

    //
    // We always call Close, even if we are in an error state. The managed interior
    // implementation might need to close stuff and might have an independent error to
    // report.
    // 

    m_sink->Close();

    STANDARD_EPILOGUE

    return m_hr;
}

STDMETHODIMP_(void)
GeometrySinkCallback::AddLine(
    __in D2D1_POINT_2F vertex 
    )
{
    STANDARD_PROLOGUE

    Point2F point2F;

    point2F.CopyFrom(vertex);

    ISimplifiedGeometrySink^ sink = m_sink;
    safe_cast<IGeometrySink^>(sink)->AddLine(point2F);

    STANDARD_EPILOGUE
}

STDMETHODIMP_(void)
GeometrySinkCallback::AddBezier(
    __in CONST D2D1_BEZIER_SEGMENT *pBezier 
    )
{
    STANDARD_PROLOGUE

    BezierSegment bezierSegment;

    bezierSegment.CopyFrom(*pBezier);

    ISimplifiedGeometrySink^ sink = m_sink;
    safe_cast<IGeometrySink^>(sink)->AddBezier(bezierSegment);

    STANDARD_EPILOGUE
}

STDMETHODIMP_(void)
GeometrySinkCallback::AddQuadraticBezier(
    __in CONST D2D1_QUADRATIC_BEZIER_SEGMENT *pBezier 
    )
{
    STANDARD_PROLOGUE

    QuadraticBezierSegment quadraticBezierSegment;

    quadraticBezierSegment.CopyFrom(*pBezier);

    ISimplifiedGeometrySink^ sink = m_sink;
    safe_cast<IGeometrySink^>(sink)->AddQuadraticBezier(quadraticBezierSegment);

    STANDARD_EPILOGUE
}

STDMETHODIMP_(void)
GeometrySinkCallback::AddQuadraticBeziers(
    __in_ecount(aBeziersLength) CONST D2D1_QUADRATIC_BEZIER_SEGMENT *aBeziers,
    UINT aBeziersLength 
    )
{
    STANDARD_PROLOGUE

    cli::array<QuadraticBezierSegment> ^arrayCopy = GetArray<QuadraticBezierSegment>(aBeziers, aBeziersLength);

    ISimplifiedGeometrySink^ sink = m_sink;
    safe_cast<IGeometrySink^>(sink)->AddQuadraticBeziers(arrayCopy);

    STANDARD_EPILOGUE
}

STDMETHODIMP_(void)
GeometrySinkCallback::AddArc(
    __in CONST D2D1_ARC_SEGMENT *pArc 
    )
{
    STANDARD_PROLOGUE

    ArcSegment arcSegment;
    arcSegment.CopyFrom(*pArc);

    ISimplifiedGeometrySink^ sink = m_sink;
    safe_cast<IGeometrySink^>(sink)->AddArc(arcSegment);

    STANDARD_EPILOGUE
}

GeometrySinkCallback::~GeometrySinkCallback()
{
}


} } } }
