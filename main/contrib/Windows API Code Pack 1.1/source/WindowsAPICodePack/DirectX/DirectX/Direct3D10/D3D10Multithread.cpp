// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Multithread.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

void Multithread::Enter()
{

    CastInterface<ID3D10Multithread>()->Enter();
}

Boolean Multithread::IsMultithreadProtected::get()
{

    return CastInterface<ID3D10Multithread>()->GetMultithreadProtected() != 0;
}

void Multithread::Leave()
{

    CastInterface<ID3D10Multithread>()->Leave();
}

void Multithread::IsMultithreadProtected::set(Boolean value)
{

    CastInterface<ID3D10Multithread>()->SetMultithreadProtected(safe_cast<BOOL>(value));
}

