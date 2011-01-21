// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include <vector>
#include <map>
#include <string>

using namespace std;
using namespace msclr::interop;

using namespace System;
using namespace System::IO;
using namespace System::Collections::Generic;
using namespace System::Collections::ObjectModel;
using namespace System::Runtime::InteropServices;

// Work-around for C++ compiler bug: crashes when System.Core.dll is included as a reference

#ifdef SYSTEM_CORE_DLL_WORKAROUND
namespace System { namespace Linq {

private ref class Enumerable
{
private:
    Enumerable() { }

public:
    generic<typename TSource>
    static array<TSource>^ ToArray(IEnumerable<TSource>^ source)
    {
        System::Collections::ICollection^ sourceAsCollection = dynamic_cast<System::Collections::ICollection^>(source);
        List<TSource>^ list;

        if (sourceAsCollection != nullptr)
        {
            list = gcnew List<TSource>(sourceAsCollection->Count);
        }
        else
        {
            list = gcnew List<TSource>();
        }

        IEnumerator<TSource>^ enumerator = source->GetEnumerator();

        while (enumerator->MoveNext())
        {
            list->Add(enumerator->Current);
        }

        return list->ToArray();
    }

    generic<typename TSource>
    static int Count(IEnumerable<TSource>^ source)
    {
        System::Collections::ICollection^ sourceAsCollection = dynamic_cast<System::Collections::ICollection^>(source);

        if (sourceAsCollection != nullptr)
        {
            return sourceAsCollection->Count;
        }

        int count = 0;
        IEnumerator<TSource>^ enumerator = source->GetEnumerator();

        while (enumerator->MoveNext())
        {
            count++;
        }

        return count;
    }

    generic<typename TSource>
    static TSource FirstOrDefault(IEnumerable<TSource>^ source)
	{
		for each (TSource item in source)
		{
			// Return the first one we find
			return item;
		}

		// If the enumeration is empty, return null
		return TSource();
	}
};

} }

#endif // SYSTEM_CORE_DLL_WORKAROUND

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX {
ref class DirectUnknown;
ref class DirectObject;

namespace Utilities {

/// <summary>
/// Utility class for DirectX wrappers
/// </summary>
private ref class CommonUtils
{
private:
    static Dictionary<System::Type^, System::Guid>^ GuidsDictionary = gcnew Dictionary<System::Type^, System::Guid>();
    static Object^ guidDictionaryLock = gcnew Object();

    // Prohibit instantiation of static class
    CommonUtils() { }

internal:

    // Interface name retrieval
    static int MaxNameSize = 1024;
    

    // GUID mapping
    static GUID GetGuid(System::Type^ type);
    static Guid GetManagedGuid(System::Type^ type);



    // Stream helper method
    static array<unsigned char>^ ReadStream(Stream^ stream);


    // Module loading
    static HINSTANCE LoadDll(String^ libraryName);

};

} } } }
