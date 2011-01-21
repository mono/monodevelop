// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DXGIKeyedMutex.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;

KeyedMutex^ KeyedMutex::FromResource(DirectUnknown^ unknown)
{
	IDXGIKeyedMutex *pmutex;

	Validate::VerifyResult(unknown->CastInterface<IUnknown>()->QueryInterface(__uuidof(IDXGIKeyedMutex), (void **)&pmutex));

	return pmutex == NULL ? nullptr : gcnew KeyedMutex(pmutex);
}

void KeyedMutex::AcquireSync(UInt64 key, DWORD milliseconds)
{
    Validate::VerifyResult(CastInterface<IDXGIKeyedMutex>()->AcquireSync(safe_cast<UINT64>(key), safe_cast<DWORD>(milliseconds)));
}

void KeyedMutex::ReleaseSync(UInt64 key)
{
    Validate::VerifyResult(CastInterface<IDXGIKeyedMutex>()->ReleaseSync(safe_cast<UINT64>(key)));
}

