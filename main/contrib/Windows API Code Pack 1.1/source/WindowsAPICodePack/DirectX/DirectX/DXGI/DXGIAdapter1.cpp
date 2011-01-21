// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DXGIAdapter1.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

AdapterDescription1 Adapter1::Description1::get()
{
	if (!description.HasValue)
	{
        DXGI_ADAPTER_DESC1 nativeDesc;

        Validate::VerifyResult(CastInterface<IDXGIAdapter1>()->GetDesc1(&nativeDesc));

        description = Nullable<AdapterDescription1>(AdapterDescription1(nativeDesc));
	}

    return description.Value;
}

