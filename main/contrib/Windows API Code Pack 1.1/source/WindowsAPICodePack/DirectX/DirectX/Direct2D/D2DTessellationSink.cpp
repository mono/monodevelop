// Copyright (c) Microsoft Corporation.  All rights reserved.

//+----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
// File name:
//      TessellationSink.cpp
//
// Description: 
//      Implements native tessellation sink wrapper interfaces that call into a 
//      wrapper implementation of a managed interface supplied by the caller.
//
//-----------------------------------------------------------------------------
#include "stdafx.h"
#include "D2DTessellationSink.h"
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


TessellationSinkCallback::TessellationSinkCallback(
    ITessellationSink ^sink
    ) : m_hr(S_OK),
        m_sink(sink),
        m_cRef(1)
{
}


STDMETHODIMP
TessellationSinkCallback::QueryInterface(
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
    else if (riid == __uuidof(ID2D1TessellationSink))
    {
        pv = static_cast<ID2D1TessellationSink *>(this);
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
TessellationSinkCallback::AddRef()
{
    return InterlockedIncrement(&m_cRef);
}

STDMETHODIMP_(ULONG)
TessellationSinkCallback::Release()
{
    LONG cRef = InterlockedDecrement(&m_cRef);

    if (cRef == 0)
    {
        delete this;
    }

    return cRef;
}

STDMETHODIMP_(void)
TessellationSinkCallback::AddTriangles(
    __in_ecount(aTrianglesLength) CONST D2D1_TRIANGLE *aTriangles,
    UINT aTrianglesLength 
    )
{
    STANDARD_PROLOGUE

    cli::array<Triangle> ^arrayCopy = GetArray<Triangle>(aTriangles, aTrianglesLength);

    m_sink->AddTriangles(arrayCopy);

    STANDARD_EPILOGUE
}

STDMETHODIMP
TessellationSinkCallback::Close()
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

TessellationSinkCallback::~TessellationSinkCallback()
{

}

} } } }
