// Copyright (c) Microsoft Corporation.  All rights reserved.
#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct2D1 {


using namespace System::Collections::Generic;

/// <summary>
/// Implements native geometry sink wrapper interfaces that call into a 
/// wrapper implementation of a managed interface supplied by the caller.
/// </summary>
class GeometrySinkCallback : public ID2D1GeometrySink
{
public:

    GeometrySinkCallback(
        ISimplifiedGeometrySink ^sink
        );

    //
    // IUnknown methods
    // 
    STDMETHOD(QueryInterface)(
        __in REFIID riid,
        __out void **ppv
        );

    STDMETHOD_(ULONG, AddRef)();

    STDMETHOD_(ULONG, Release)();

    //
    // ISimplifiedGeometrySink methods
    // 
    STDMETHOD_(void, SetFillMode)(
        D2D1_FILL_MODE fillMode 
        );

    STDMETHOD_(void, SetSegmentFlags)(
        D2D1_PATH_SEGMENT vertexFlags 
        );

    STDMETHOD_(void, BeginFigure)(
        __in D2D1_POINT_2F startPoint,
        D2D1_FIGURE_BEGIN figureBegin 
        );

    STDMETHOD_(void, AddLines)(
        __in_ecount(pointsLength) CONST D2D1_POINT_2F *points,
        UINT pointsLength 
        );

    STDMETHOD_(void, AddBeziers)(
        __in_ecount(aBeziersLength) CONST D2D1_BEZIER_SEGMENT *aBeziers,
        UINT aBeziersLength 
        );

    STDMETHOD_(void, EndFigure)(
        D2D1_FIGURE_END figureEnd 
        );

    STDMETHOD(Close)();

    //
    // IGeometrySink methods
    // 
    STDMETHOD_(void, AddLine)(
        __in D2D1_POINT_2F vertex 
        );

    STDMETHOD_(void, AddBezier)(
        __in CONST D2D1_BEZIER_SEGMENT *pBezier 
        );

    STDMETHOD_(void, AddQuadraticBezier)(
        __in CONST D2D1_QUADRATIC_BEZIER_SEGMENT *pBezier 
        );

    STDMETHOD_(void, AddQuadraticBeziers)(
        __in_ecount(aBeziersLength) CONST D2D1_QUADRATIC_BEZIER_SEGMENT *aBeziers,
        UINT aBeziersLength 
        );

    STDMETHOD_(void, AddArc)(
        __in CONST D2D1_ARC_SEGMENT *pArc 
        );    

protected:

    template<class Managed, class Native>
    cli::array<Managed> ^
    GetArray(
        __in_ecount(count) const Native *source,
        UINT count
        )
    {
        cli::array<Managed> ^newArray = gcnew cli::array<Managed>(count);

        for(UINT i = 0; i < count; ++i)
        {
            newArray[i].CopyFrom(source[i]);
        }

        return newArray;
    }


    virtual ~GeometrySinkCallback();

private:

    //
    // Copying and assignment are not defined.
    // 
    GeometrySinkCallback(
        __in const GeometrySinkCallback &
        );

    GeometrySinkCallback &
    operator=(
        __in const GeometrySinkCallback &
        );

    int m_token;
    HRESULT m_hr;
    LONG m_cRef;

    gcroot<ISimplifiedGeometrySink^> m_sink;
};

} } } }

