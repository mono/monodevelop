//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectUnknown.h"
#include "DWriteEnums.h"
#include "DWriteStructs.h"
#include "ICustomInlineObject.h"

using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace DirectWrite {

/// <summary>
/// Wraps an inline graphic.
/// <para>(Also see DirectX SDK: IDWriteInlineObject)</para>
/// </summary>
public ref class InlineObject : public DirectUnknown, public ICustomInlineObject
{
private: 

    virtual void Draw(
        FLOAT originX,
        FLOAT originY,
        Boolean isSideways,
        Boolean isRightToLeft,
        Microsoft::WindowsAPICodePack::DirectX::Direct2D1::Brush^ clientDrawingEffect
        ) sealed = ICustomInlineObject::Draw;

    virtual property InlineObjectMetrics Metrics
    {
        InlineObjectMetrics get() sealed = ICustomInlineObject::Metrics::get;
    }

    virtual property DirectWrite::OverhangMetrics OverhangMetrics
    {
        DirectWrite::OverhangMetrics get() sealed = ICustomInlineObject::OverhangMetrics::get;
    }

    virtual property BreakCondition BreakConditionBefore
    {
        BreakCondition get() sealed = ICustomInlineObject::BreakConditionBefore::get;
    }

    virtual property BreakCondition BreakConditionAfter
    {
        BreakCondition get() sealed = ICustomInlineObject::BreakConditionAfter::get;
    }

internal:

    InlineObject(void)
    { }

    InlineObject(IDWriteInlineObject* pNativeIDWriteInlineObject) : DirectUnknown(pNativeIDWriteInlineObject)
    { }
};

private class AutoInlineObject
{
public:

    AutoInlineObject(ICustomInlineObject^ managedInline)
    {
        if (managedInline != nullptr)
        {
            InlineObject^ inlineWrapper = dynamic_cast<InlineObject^>(managedInline);

            if (inlineWrapper != nullptr)
            {
                // Get unmanaged implementation from wrapper object
                nativeInline = inlineWrapper->CastInterface<IDWriteInlineObject>();
            }
            else
            {
                // Wrap the managed implementation in an unmanaged one
                nativeInline = new CustomInlineObject(managedInline);
                release = true;
            }
        }
        else
        {
            nativeInline = NULL;
        }
    }

    IDWriteInlineObject* GetNativeInline(void) { return nativeInline; }

    virtual ~AutoInlineObject(void)
    {
        if (release)
        {
            nativeInline->Release();
            nativeInline = NULL;
            release = false;
        }
    }

private:

    IDWriteInlineObject *nativeInline;
    bool release;
};

} } } }