// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DWriteInlineObject.h"

void InlineObject::Draw(
    FLOAT /* originX */,
    FLOAT /* originY */,
    Boolean /* isSideways */,
    Boolean /* isRightToLeft */,
    Direct2D1::Brush^ /* clientDrawingEffect */
    )
{
    throw gcnew NotSupportedException("Do not use the managed ICustomInlineObject interface directly from this object; unwrap the native IDWriteInlineObject interface and use that.");
}

InlineObjectMetrics InlineObject::Metrics::get(void)
{
    throw gcnew NotSupportedException("Do not use the managed ICustomInlineObject interface directly from this object; unwrap the native IDWriteInlineObject interface and use that.");
}

DirectWrite::OverhangMetrics InlineObject::OverhangMetrics::get(void)
{
    throw gcnew NotSupportedException("Do not use the managed ICustomInlineObject interface directly from this object; unwrap the native IDWriteInlineObject interface and use that.");
}

BreakCondition InlineObject::BreakConditionBefore::get(void)
{
    throw gcnew NotSupportedException("Do not use the managed ICustomInlineObject interface directly from this object; unwrap the native IDWriteInlineObject interface and use that.");
}

BreakCondition InlineObject::BreakConditionAfter::get(void)
{
    throw gcnew NotSupportedException("Do not use the managed ICustomInlineObject interface directly from this object; unwrap the native IDWriteInlineObject interface and use that.");
}