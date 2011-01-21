// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11SwitchToRef.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

Boolean SwitchToRef::UseRef::get(void)
{
    BOOL returnValue = CastInterface<ID3D11SwitchToRef>()->GetUseRef();
    return returnValue != 0;
}

void SwitchToRef::UseRef::set(Boolean value)
{
    BOOL returnValue = CastInterface<ID3D11SwitchToRef>()->SetUseRef(safe_cast<BOOL>(value));

    if (!returnValue)
    {
        throw gcnew InvalidOperationException("Cannot switch the device, because it was not created with the CreateDeviceOptions.SwitchToRef option");
    }
}

