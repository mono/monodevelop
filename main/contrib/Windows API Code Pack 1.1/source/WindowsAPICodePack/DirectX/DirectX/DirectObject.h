// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX {

/// <summary>
/// Base for classes supporting an internal interface that is not an IUnknown
/// </summary>
public ref class DirectObject abstract
{
internal:

    template <typename T>
    T* CastInterface()
    {
        return static_cast<T*>(nativeObject.Get());
    }

    DirectObject();
    
    DirectObject(void* _ptr);

    DirectObject(void* _ptr, bool _deletable);

	// Should be used ONLY by DirectHelpers::CreateInterfaceWrapper!
    void Attach(void* _right);

public:
    /// <summary>
    /// Get the internal native pointer for the wrapped native object
    /// </summary>
    /// <returns>
    /// A pointer to the wrapped native interfac.
    /// </returns>
    property IntPtr NativeObject
    {
        IntPtr get()
        {
            return IntPtr(nativeObject.Get());
        }
    }

private:
    AutoPointer<void> nativeObject;

};
} } }
