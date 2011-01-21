//Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "CommonUtils.h"
#include "AllTypes.h"
#include <msclr/lock.h>

using namespace msclr;
using namespace System::IO;
using namespace System::Reflection;
using namespace System::Globalization;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Graphics;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D;

GUID CommonUtils::GetGuid(System::Type^ type)
{
    return Utilities::Convert::GUIDFromSystemGuid(GetManagedGuid(type));
}

Guid CommonUtils::GetManagedGuid(Type^ type)
{
    lock l(guidDictionaryLock);

    System::Guid guid;

    if (!GuidsDictionary->TryGetValue(type, guid))
    {
        PropertyInfo^ prop = type->GetProperty(
            "Guid",
            BindingFlags::Static | BindingFlags::NonPublic | BindingFlags::DeclaredOnly,
            nullptr,
            System::Guid::typeid,
            gcnew array<Type^>(0),
            nullptr);

        if (prop == nullptr)
        {
            throw gcnew ArgumentException(
                String::Format(CultureInfo::CurrentCulture,
                "Type argument \"{0}\" is not a valid DirectX wrapper type", type->Name),
                "type");
        }

        guid = safe_cast<System::Guid>(prop->GetValue(nullptr, nullptr));

        GuidsDictionary->Add(type, guid);
    }

    return guid;
}

HINSTANCE CommonUtils::LoadDll(String^ libraryName)
{
    HINSTANCE softwareModule = NULL;

    String^ fullPath = Path::GetFullPath(libraryName);
    
    if (!File::Exists(fullPath) && !Path::IsPathRooted(libraryName))
    {
        fullPath = Path::Combine(System::Environment::GetFolderPath(
            Environment::SpecialFolder::System),
            libraryName);
    }

    if (!File::Exists(fullPath))
    {
        throw gcnew FileNotFoundException(
            String::Format(CultureInfo::CurrentCulture,
            "Unable to find library \"{0}\".", libraryName));
    }
    
    pin_ptr<const wchar_t> libName = PtrToStringChars(fullPath);
    softwareModule = LoadLibrary(libName);

    if (softwareModule == NULL)
    {
        throw gcnew System::ComponentModel::Win32Exception(Marshal::GetLastWin32Error()); 
    }
    
    return softwareModule;
}



array<unsigned char>^ CommonUtils::ReadStream(Stream^ stream)
{
    // REVIEW: technically, no dispose of BinaryReader is necessary,
    // as this code doesn't own the Stream passed in.  In fact, disposing
    // it could hose the caller, who may expect the Stream to last
    // longer.  On the other hand, it means that the BinaryReader will
    // wind up in the finalizer queue, causing GC overhead that's unnecessary.
    // If the contract for this method can be "disposes the passed-in
    // stream", it can be more efficient.
    //
    // Basically, this code is an accident waiting to happen.  At
    // the very least, should leave a comment in to explain that the code
    // intentionally fails to dispose the IDisposable object (contrary to
    // .NET guidelines).
    BinaryReader^ reader = gcnew BinaryReader(stream);
    array<unsigned char>^ data = reader->ReadBytes( (int)stream->Length);

    return data;
}
