// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX {

ref class DirectUnknown;
ref class DirectObject;

/// <summary>
/// This class provides a set of helper methods that can be used to extend
/// the API, and wrap external natives interfaces.
/// </summary>
public ref class DirectHelpers sealed
{
private:

	// Static class
	DirectHelpers() { }

public:
    /// <summary>
    /// Return an exception for a given HResult
    /// </summary>
    /// <param name="hresult">The HResult value</param>
    /// <returns>The equivalent exception</returns>
    static Exception^ GetExceptionForHresult(int hresult);

    /// <summary>
    /// Return an exception for a given ErrorCode
    /// </summary>
    /// <param name="errorCode">The ErrorCode value</param>
    /// <returns>The equivalent exception</returns>
    static Exception^ GetExceptionForErrorCode(ErrorCode errorCode);

    /// <summary>
    /// Throw an exception for a given ErrorCode. 
    /// No exception will be thrown if the errorCode is Success.
    /// </summary>
    /// <param name="errorCode">The ErrorCode value</param>
    static void ThrowExceptionForErrorCode(ErrorCode errorCode);    

    /// <summary>
    /// Throw an exception for a given HResult
    /// No exception will be thrown if the errorCode is Success.
    /// </summary>
    /// <param name="hresult">The HResult value</param>
    static void ThrowExceptionForHresult(int hresult);

    /// <summary>
    /// Create a wrapper for a given native IUnknown interface pointer.
    /// This method will not increase the ref count for the wrapped native
    /// interface. However, when this class is disposed, the native interface
    /// will have Release() called.
    /// This method is mainly intended to wrap interfaces that inherit from IUnknown.
    /// </summary>
    /// <param name="nativeIUnknown">The native pointer to the IUnknown interface.</param>
    /// <typeparam name="T">The type of the IUnknown wrapper to create. Must inherit from <see cref="DirectUnknown"/>.</typeparam>
    /// <returns>A DirectUnknown wrapper.</returns>
    generic <typename T> where T : DirectUnknown
    static T CreateIUnknownWrapper(System::IntPtr nativeIUnknown);

    /// <summary>
    /// Create a wrapper for a given native interface pointer that does not have an IUnknown.
    /// This method will not increase the ref count for the wrapped native
    /// interface. Also, when this class is disposed, the native interface
    /// will not be deleted or released.
    /// This method is mainly intended to wrap interfaces that do not inherit from IUnknown.
    /// </summary>
    /// <typeparam name="T">The type of the object wrapper to create. Must inherit from <see cref="DirectObject"/>.</typeparam>
    /// <param name="nativeInterface">The native pointer to the interface.</param>
    /// <returns>A DirectObject wrapper.</returns>
    generic <typename T> where T : DirectObject
    static T CreateInterfaceWrapper(System::IntPtr nativeInterface);
};
} } }
