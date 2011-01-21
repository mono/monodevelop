//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "DWriteEnums.h"
#include "DWriteStructs.h"

using namespace Microsoft::WindowsAPICodePack::DirectX;
using namespace System::Runtime::InteropServices;
using namespace System::Runtime::CompilerServices;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct2D1 {
    ref class Brush;
}}}}

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {

/// <summary>
/// Wraps an application defined inline graphic, allowing DWrite to query 
/// metrics as if it was a glyph inline with the text.
/// <para>(Also see DirectX SDK: IDWriteInlineObject)</para>
/// </summary>
public interface class ICustomInlineObject
{
    /// <summary>
    /// The application implemented rendering callback (IDWriteTextRenderer::DrawInlineObject)
    /// can use this to draw the inline object without needing to cast or query the object
    /// type. The text layout does not call this method directly.
    /// </summary>
    /// <param name="originX">X-coordinate at the top-left corner of the inline object.</param>
    /// <param name="originY">Y-coordinate at the top-left corner of the inline object.</param>
    /// <param name="isSideways">The object should be drawn on its side.</param>
    /// <param name="isRightToLeft">The object is in an right-to-left context and should be drawn flipped.</param>
    /// <param name="clientDrawingEffect">The drawing effect. This is usually a foreground brush.</param>
    void Draw(
        FLOAT originX,
        FLOAT originY,
        Boolean isSideways,
        Boolean isRightToLeft,
        Microsoft::WindowsAPICodePack::DirectX::Direct2D1::Brush^ clientDrawingEffect
        ) = 0;

    /// <summary>
    /// TextLayout calls this callback function to get the measurement of the inline object.
    /// </summary>
    property InlineObjectMetrics Metrics
    {
        InlineObjectMetrics get() = 0;
    }

    /// <summary>
    /// TextLayout calls this callback function to get the visible extents (in DIPs) of the inline object.
    /// In the case of a simple bitmap, with no padding and no overhang, all the overhangs will
    /// simply be zeroes.
    /// </summary>
    /// <remarks>
    /// The overhangs should be returned relative to the reported size of the object
    /// (InlineObjectMetrics width/height), and should not be baseline
    /// adjusted. If you have an image that is actually 100x100 DIPs, but you want it
    /// slightly inset (perhaps it has a glow) by 20 DIPs on each side, you would
    /// return a width/height of 60x60 and four overhangs of 20 DIPs.
    /// </remarks>
    property DirectWrite::OverhangMetrics OverhangMetrics
    {
        DirectWrite::OverhangMetrics get() = 0;
    }

    /// <summary>
    /// Layout uses this to determine the line breaking behavior of the inline object
    /// amidst the text.
    /// Should return the line-breaking condition between the object and the content immediately preceding it.
    /// </summary>
    property BreakCondition BreakConditionBefore
    {
        BreakCondition get() = 0;
    }

    /// <summary>
    /// Layout uses this to determine the line breaking behavior of the inline object
    /// amidst the text.
    /// Should return the line-breaking condition between the object and the content immediately following it.
    /// </summary>
    property BreakCondition BreakConditionAfter
    {
        BreakCondition get() = 0;
    }
};

private class CustomInlineObject
    :   public ::IDWriteInlineObject
{
public:
    CustomInlineObject(ICustomInlineObject^ inlineObject) : 
        managedInlineObject(inlineObject),
        m_refCount(1)
    { }

    STDMETHOD(Draw)(
        __maybenull void* clientDrawingContext,
        IDWriteTextRenderer* renderer,
        FLOAT originX,
        FLOAT originY,
        BOOL isSideways,
        BOOL isRightToLeft,
        IUnknown* clientDrawingEffect
        );

    STDMETHOD(GetMetrics)(
        __out DWRITE_INLINE_OBJECT_METRICS* metrics
        );

    STDMETHOD(GetOverhangMetrics)(
        __out DWRITE_OVERHANG_METRICS* overhangs
        );

    STDMETHOD(GetBreakConditions)(
        __out DWRITE_BREAK_CONDITION* breakConditionBefore,
        __out DWRITE_BREAK_CONDITION* breakConditionAfter
        );

public:
    unsigned long STDMETHODCALLTYPE AddRef();
    unsigned long STDMETHODCALLTYPE Release();
    HRESULT STDMETHODCALLTYPE QueryInterface(
        IID const& riid,
        void** ppvObject
    );

protected:
    virtual ~CustomInlineObject() { }

private:
    gcroot<ICustomInlineObject^> managedInlineObject;
    LONG m_refCount;
};

} } } }