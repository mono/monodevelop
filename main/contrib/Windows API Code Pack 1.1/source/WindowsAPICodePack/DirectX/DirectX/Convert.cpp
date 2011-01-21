//Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "Convert.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;

generic <typename T> where T : DirectUnknown
T Utilities::Convert::CreateIUnknownWrapper(IUnknown* ptr)
{
#ifdef _DEBUG
    System::Diagnostics::Debug::Assert(ptr != NULL);

    // We can't verify that the pointer type being passed by the caller
    // is correct, but we can at least verify that the COM object it
    // points to implements the interface that goes with the managed
    // type being used to wrap it.
    IUnknown *punknownDebug;

    Validate::VerifyResult(ptr->QueryInterface(CommonUtils::GetGuid(T::typeid), (void **)&punknownDebug));
    punknownDebug->Release();
#endif

    T wrapper = safe_cast<T>(Activator::CreateInstance(T::typeid, true));
    (safe_cast<DirectUnknown^>(wrapper))->Attach(ptr);
    return wrapper;
}

CA_SUPPRESS_MESSAGE("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")
System::Guid Utilities::Convert::SystemGuidFromGUID(REFGUID guid) 
{
   return Guid( 
       guid.Data1, guid.Data2, guid.Data3, 
       guid.Data4[ 0 ], guid.Data4[ 1 ], 
       guid.Data4[ 2 ], guid.Data4[ 3 ], 
       guid.Data4[ 4 ], guid.Data4[ 5 ], 
       guid.Data4[ 6 ], guid.Data4[ 7 ] );
}

GUID Utilities::Convert::GUIDFromSystemGuid( System::Guid guid ) 
{
   pin_ptr<Byte> data = &(guid.ToByteArray()[ 0 ]);
   return *(GUID *)data;
}