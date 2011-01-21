// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "DXGIFactory1.h"
#include "DXGIAdapter1.h"
#include "LibraryLoader.h"

using namespace System::Collections::Generic;
using namespace System::Collections::ObjectModel;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;

typedef HRESULT (WINAPI *CreateDXGIFactory1FuncPtr)(REFIID riid, void **ppFactory);

Factory1^ Factory1::Create()
{
    CreateDXGIFactory1FuncPtr createFuncPtr = 
        (CreateDXGIFactory1FuncPtr) LibraryLoader::Instance()->GetFunctionFromDll(
            DXGILibrary, "CreateDXGIFactory1");

    IDXGIFactory1 * factory = NULL;

    Validate::VerifyResult(
        (*createFuncPtr)(__uuidof(IDXGIFactory1), (void**)(&factory)));

    return gcnew Factory1(factory);
}

ReadOnlyCollection<Adapter1^>^ Factory1::Adapters::get(void)
{
    if (adapters != nullptr)
    {
        return adapters;
    }

    IList<Adapter1^>^ adapters1Cache = gcnew List<Adapter1^>();

    int i = 0;
    IDXGIAdapter1* tempAdapter;

    while (DXGI_ERROR_NOT_FOUND != CastInterface<IDXGIFactory1>()->EnumAdapters1(i++, &tempAdapter))
    {
        adapters1Cache->Add(gcnew Adapter1(tempAdapter));
    }

    adapters = gcnew ReadOnlyCollection<Adapter1^>(adapters1Cache);

    return adapters;
}

Boolean Factory1::IsCurrent::get()
{
    return CastInterface<IDXGIFactory1>()->IsCurrent() != 0;
}

