// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

using namespace System::Globalization;

namespace  Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Utilities {

private ref class Convert
{
private:
    // Prohibit instantiation of static class
    Convert() { }

internal:
    // Convert existing IUnknown reference to a managed wrapper
    generic <typename T> where T : DirectUnknown
    static T CreateIUnknownWrapper(IUnknown* ptr);

    // GUID conversion
	CA_SUPPRESS_MESSAGE("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")
    static System::Guid SystemGuidFromGUID(REFGUID guid );
    static GUID GUIDFromSystemGuid( System::Guid guid );

    // Convert a vector<S> to a ReadOnlyCollection<T>
    template <class T, class S>
    static ReadOnlyCollection<T^>^ GetCollection(UINT numElements, vector<S*> & buffer)
    {
        List<T^>^ items = gcnew List<T^>(numElements);
        for (UINT i = 0; i < numElements; i++)
        {
            items->Add(buffer[i] == NULL ? nullptr : gcnew T(buffer[i]));
        }

        return gcnew ReadOnlyCollection<T^>(items);
    }

    // Convert a vector<S> to a ReadOnlyCollection<T>
    template <class T, class S>
    static ReadOnlyCollection<T^>^ GetTrimmedCollection(S *ignoreMarker, vector<S *> &buffer)
    {
        vector<S *>::size_type count = buffer.size();

        while (count > 0 && buffer[count - 1] != ignoreMarker) { count--; }

        if (count > static_cast<vector<S *>::size_type>(Int32::MaxValue))
        {
            throw gcnew ArgumentOutOfRangeException("buffer",
                "Collection is too large to convert; must not have more than 2,147,483,647 elements");
        }

        List<T^>^ items = gcnew List<T^>(static_cast<Int32>(count));

        for (UINT i = 0; i < count; i++)
        {
            items->Add(buffer[i] == NULL ? nullptr : gcnew T(buffer[i]));
        }

        return gcnew ReadOnlyCollection<T^>(items);
    }

    // Convert IEnumerable<T> to a vector<S> of pointers
    template <class T, class S>
    static UINT FillIUnknownsVector(IEnumerable<T^>^ items, vector<S*> & buffer)
    {
        if (items != nullptr)
        {
            for each (T^ item in items)
            {
                buffer.push_back(item == nullptr ? NULL : item->CastInterface<S>());
            }
        }

        // REVIEW: potential overflow for 64-bit builds
        return static_cast<UINT>(buffer.size());
    }

    // Convert IEnumerable<T> to a vector<S> of values
    // REVIEW: unused?
    template <class T, class S>
    static UINT FillStructsVector(IEnumerable<T^>^ items, vector<S> & buffer)
    {
        if (items != nullptr)
        {
            for each (T^ item in items)
            {
                buffer.push_back(*(item->nativeObject.Get()));
            }
        }

        // REVIEW: potential overflow for 64-bit builds
        return static_cast<UINT>(buffer.size());
    }

    // Convert IEnumerable<T> of values to vector<S> of values
    template <class T, class S>
    static UINT FillValStructsVector(IEnumerable<T>^ items, vector<S> & buffer)
    {
        if (items != nullptr)
        {
            for each (T item in items)
            {
                buffer.push_back(item);
            }
        }

        // REVIEW: potential overflow for 64-bit builds
        return static_cast<UINT>(buffer.size());
    }

    // Convert IEnumerable<T> of values to vector<S> of values by calling
    // CopyTo() method for T
    template <class T, class S>
    static UINT CopyToStructsVector(IEnumerable<T>^ items, vector<S> & buffer)
    {
        if (items != nullptr)
        {
            for each (T item in items)
            {
                S bufferItem;

                item.CopyTo(bufferItem);
                buffer.push_back(bufferItem);
            }
        }

        // REVIEW: potential overflow for 64-bit builds
        return static_cast<UINT>(buffer.size());
    }

    // Convert IEnumerable<T> of values to vector<S> of values
    template <class T, class S>
    static UINT FillValStructsVector(IEnumerable<T>^ items, vector<S> & buffer, marshal_context^ ctx)
    {
        if (items != nullptr)
        {
            for each (T item in items)
            {
                S s = {0};
                item.CopyTo(&s, ctx);
                buffer.push_back(s);
            }
        }

        // REVIEW: potential overflow for 64-bit builds
        return static_cast<UINT>(buffer.size());
    }
};

} } } }