//Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "DirectUnknown.h"

using namespace Microsoft::WindowsAPICodePack::DirectX;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;

DirectUnknown::DirectUnknown() { }

void DirectUnknown::Attach(IUnknown* _right)
{
	// Default is deletable
    nativeUnknown.Set(_right);
}

DirectUnknown::DirectUnknown(IUnknown* _iUnknown) 
{
    nativeUnknown.Set(_iUnknown);
}

generic<typename T> where T : DirectUnknown
T DirectUnknown::QueryInterface(void)
{
    IUnknown *punknown;

    CastInterface<IUnknown>()->QueryInterface(CommonUtils::GetGuid(T::typeid), (void **)&punknown);

    return Utilities::Convert::CreateIUnknownWrapper<T>(punknown);
}
