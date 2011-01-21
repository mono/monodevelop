// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10Include.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;
using namespace msclr::interop;

Include::Include(IntPtr nativeInterface)
	: DirectObject(static_cast<ID3D10Include *>(nativeInterface.ToPointer()))
{
}

void Include::Close(IncludeData data)
{
    Validate::VerifyResult(CastInterface<ID3D10Include>()->Close(data.Pointer.ToPointer()));
}

IncludeData Include::Open(IncludeType includeType, String^ fileName, IntPtr parentData)
{
    void* tempoutData = NULL;    
    UINT tempSize;

    IntPtr ptr = Marshal::StringToHGlobalAnsi(fileName);

    try
    {
        Validate::VerifyResult(CastInterface<ID3D10Include>()->Open(
            static_cast<D3D10_INCLUDE_TYPE>(includeType), 
            static_cast<char*>(ptr.ToPointer()), 
            static_cast<LPCVOID>(parentData.ToPointer()), 
            (LPCVOID*)&tempoutData, 
            &tempSize));
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }

    return IncludeData(IntPtr(tempoutData), tempSize);
}

