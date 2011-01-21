// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "LibraryLoader.h"
#include "D3D10Blob.h"

using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;

typedef HRESULT (WINAPI *PfnD3D10CreateBlob)(SIZE_T NumBytes, LPD3D10BLOB *ppBuffer);

Blob^ Blob::Create(Int64 size)
{
	if (IntPtr::Size == 4 && size > Int32::MaxValue)
	{
		throw gcnew ArgumentOutOfRangeException("size", size,
			"Allocation size must be no greater than " + Int32::MaxValue.ToString(System::Globalization::CultureInfo::InvariantCulture));
	}

	PfnD3D10CreateBlob createFunction = (PfnD3D10CreateBlob)LibraryLoader::Instance()->GetFunctionFromDll(
		D3D10Library, "D3D10CreateBlob");

	ID3D10Blob *blobInterface;

	Validate::VerifyResult(createFunction(static_cast<SIZE_T>(size), &blobInterface));

	return gcnew Blob(blobInterface);
}

IntPtr Blob::BufferPointer::get(void)
{
    return IntPtr(CastInterface<ID3D10Blob>()->GetBufferPointer());
}

UInt32 Blob::BufferSize::get(void)
{
    return static_cast<UInt32>(CastInterface<ID3D10Blob>()->GetBufferSize());
}