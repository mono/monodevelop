// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "ICustomInlineObject.h"
#include "Direct2D/D2DInterfaces.h"



HRESULT STDMETHODCALLTYPE CustomInlineObject::Draw(
    // REVIEW: unused parameter
    __maybenull void* /* clientDrawingContext */,
    // REVIEW: unused parameter
    IDWriteTextRenderer* /* renderer */,
    FLOAT originX,
    FLOAT originY,
    BOOL isSideways,
    BOOL isRightToLeft,
    IUnknown* clientDrawingEffect
    )
{
    try
    {
        managedInlineObject->Draw(originX, originY, isSideways != 0, isRightToLeft != 0, 
            clientDrawingEffect ? gcnew Microsoft::WindowsAPICodePack::DirectX::Direct2D1::Brush(clientDrawingEffect) : nullptr);
    }
    catch (System::Exception^)
    {
        return E_FAIL;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE CustomInlineObject::GetMetrics(
    __out DWRITE_INLINE_OBJECT_METRICS* metrics
    )
{
    try
    {
        InlineObjectMetrics metricsCopy = managedInlineObject->Metrics;
        metricsCopy.CopyTo(metrics);

    }
    catch (System::Exception^)
    {
        return E_FAIL;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE CustomInlineObject::GetOverhangMetrics(
    __out DWRITE_OVERHANG_METRICS* overhangs
    )
{
    try
    {
        OverhangMetrics metricsCopy = managedInlineObject->OverhangMetrics;
        metricsCopy.CopyTo(overhangs);

    }
    catch (System::Exception^)
    {
        return E_FAIL;
    }
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CustomInlineObject::GetBreakConditions(
    __out DWRITE_BREAK_CONDITION* breakConditionBefore,
    __out DWRITE_BREAK_CONDITION* breakConditionAfter
    )
{
    try
    {
        *breakConditionBefore = static_cast<DWRITE_BREAK_CONDITION>(managedInlineObject->BreakConditionBefore);
        *breakConditionAfter  = static_cast<DWRITE_BREAK_CONDITION>(managedInlineObject->BreakConditionAfter);
    }
    catch (System::Exception^)
    {
        return E_FAIL;
    }
    return S_OK;
}

STDMETHODIMP CustomInlineObject::QueryInterface(
    IID const& riid,
    void** ppvObject
    )
{
    if (__uuidof(IDWriteInlineObject) == riid)
    {
        *ppvObject = static_cast<IDWriteInlineObject*>(this);
    }
    else if (__uuidof(IUnknown) == riid)
    {
        *ppvObject = static_cast<IUnknown*>(this);
    }
    else
    {
        *ppvObject = NULL;
        return E_FAIL;
    }

    return S_OK;
}

STDMETHODIMP_(ULONG) CustomInlineObject::AddRef()
{
    return InterlockedIncrement(&m_refCount);
}

STDMETHODIMP_(ULONG) CustomInlineObject::Release()
{
    if (InterlockedDecrement(&m_refCount) == 0)
    {
        delete this;
        return 0;
    }

    return m_refCount;
}
