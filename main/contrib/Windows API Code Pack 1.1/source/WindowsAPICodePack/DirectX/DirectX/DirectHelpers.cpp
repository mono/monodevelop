//Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "DirectObject.h"
#include "DirectUnknown.h"
#include "DirectHelpers.h"
#include "CommonUtils.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct2D1;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;



generic <typename T> where T : DirectUnknown
T DirectHelpers::CreateIUnknownWrapper(System::IntPtr nativeIUnknown)
{
    return safe_cast<T>(Utilities::Convert::CreateIUnknownWrapper<T>(static_cast<IUnknown*>(nativeIUnknown.ToPointer())));
}

generic <typename T> where T : DirectObject
T DirectHelpers::CreateInterfaceWrapper(System::IntPtr nativeInterface)
{
    T wrapper = safe_cast<T>(Activator::CreateInstance(T::typeid, true));
    (safe_cast<DirectObject^>(wrapper))->Attach(nativeInterface.ToPointer());
    return wrapper;
}

Exception^ DirectHelpers::GetExceptionForHresult(int hr)
{
    return Validate::GetExceptionForHR(static_cast<HRESULT>(hr));
}

Exception^ DirectHelpers::GetExceptionForErrorCode(ErrorCode errorCode)
{
    return Validate::GetExceptionForHR(static_cast<HRESULT>(errorCode));
}

// REVIEW: why is "val" an int and not an HRESULT? Especially
// since we're casting to HRESULT?!
void DirectHelpers::ThrowExceptionForHresult(int hresult)
{
	int val = static_cast<HRESULT>(hresult);
    
	if (SUCCEEDED(val))
    {
        return;
    }

    throw Validate::GetExceptionForHR(val);
}


void DirectHelpers::ThrowExceptionForErrorCode(ErrorCode errorCode)
{
	int val = static_cast<HRESULT>(errorCode);
    
	if (SUCCEEDED(val))
    {
        return;
    }

    throw Validate::GetExceptionForHR(val);
}
