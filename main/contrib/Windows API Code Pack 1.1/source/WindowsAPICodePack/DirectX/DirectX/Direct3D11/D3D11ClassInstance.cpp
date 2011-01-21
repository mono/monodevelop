// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D11ClassInstance.h"

#include "D3D11ClassLinkage.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D11;

ClassLinkage^ ClassInstance::ClassLinkage::get(void)
{
    ID3D11ClassLinkage* tempoutLinkage = NULL;
    CastInterface<ID3D11ClassInstance>()->GetClassLinkage(&tempoutLinkage);

    return tempoutLinkage == NULL ? nullptr : gcnew Direct3D11::ClassLinkage(tempoutLinkage);
}

ClassInstanceDescription ClassInstance::Description::get()
{
    ClassInstanceDescription desc;
    pin_ptr<ClassInstanceDescription> ptr = &desc;

    CastInterface<ID3D11ClassInstance>()->GetDesc((D3D11_CLASS_INSTANCE_DESC*)ptr);

    return desc;
}

String^ ClassInstance::InstanceName::get()
{
    SIZE_T len = CommonUtils::MaxNameSize;
    LPSTR tempInstanceName = new char[len];

    try
    {
        CastInterface<ID3D11ClassInstance>()->GetInstanceName(tempInstanceName, &len);
        return tempInstanceName ? gcnew String(tempInstanceName) : nullptr;
    }
    finally
    {
        delete [] tempInstanceName;
    }
}

String^ ClassInstance::TypeName::get()
{
    SIZE_T len = CommonUtils::MaxNameSize;
    LPSTR tempTypeName = new char[len];

    try
    {
        CastInterface<ID3D11ClassInstance>()->GetTypeName(tempTypeName, &len);
        return tempTypeName ? gcnew String(tempTypeName) : nullptr;
    }
    finally
    {
        delete [] tempTypeName;
    }
}

