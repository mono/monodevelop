// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct2D1 {

using namespace System::Collections::Generic;

/// <summary>
/// Implements native tessellation sink wrapper interfaces that call into a 
/// wrapper implementation of a managed interface supplied by the caller.
/// </summary>
class TessellationSinkCallback : public ID2D1TessellationSink
{
public:

    TessellationSinkCallback(
        ITessellationSink ^sink
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
    // ITessellationSink methods
    // 
    STDMETHOD_(void, AddTriangles)(
        __in_ecount(aTrianglesLength) CONST D2D1_TRIANGLE *aTriangles,
        UINT aTrianglesLength
        );

    STDMETHOD(Close)();

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

    virtual ~TessellationSinkCallback();

private:

    //
    // Copying and assignment are not defined.
    // 
    TessellationSinkCallback(
        __in const TessellationSinkCallback &
        );

    TessellationSinkCallback &
    operator=(
        __in const TessellationSinkCallback &
        );

    HRESULT m_hr;
    LONG m_cRef;
    gcroot<ITessellationSink^> m_sink;
};

} } } }
