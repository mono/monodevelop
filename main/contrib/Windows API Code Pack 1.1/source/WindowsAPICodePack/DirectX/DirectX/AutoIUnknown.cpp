// Copyright (c) Microsoft Corporation.  All rights reserved.
#include "stdafx.h"
#include "AutoIUnknown.h"

using namespace Microsoft::WindowsAPICodePack::DirectX;

void AutoIUnknown<IUnknown>::DisposeTarget()
{
    if (isDeletable && target != 0)
    {
        target->Release();
        target = 0;
    }
}    