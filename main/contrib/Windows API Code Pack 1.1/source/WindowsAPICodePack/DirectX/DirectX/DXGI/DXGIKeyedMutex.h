//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DXGIDeviceSubObject.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {

using namespace System;

    /// <summary>
    /// Represents a keyed mutex, which allows exclusive access to a shared resource 
    /// that is used by multiple devices.
    /// <para>(Also see DirectX SDK: IDXGIKeyedMutex)</para>
    /// </summary>
    public ref class KeyedMutex :
        public DeviceSubObject
    {
    public: 
        /// <summary>
        /// Using a key, acquires exclusive rendering access to a shared resource.
        /// <para>(Also see DirectX SDK: IDXGIKeyedMutex::AcquireSync)</para>
        /// </summary>
        /// <param name="key">A value that indicates which device to give access to. 
        /// This method will succeed when the device that currently owns the surface calls
        /// the KeyedMutex.ReleaseSync method using the same value. 
        /// This value can be any UINT64 value.</param>
        /// <param name="milliseconds">The time-out interval, in milliseconds. 
        /// This method will return if the interval elapses, and the keyed mutex has not 
        /// been released using the specified Key.           
        /// If this value is set to zero, the AcquireSync method will test to see if 
        /// the keyed mutex has been released and returns immediately.           
        /// If this value is set to INFINITE, the time-out interval will never elapse.</param>
        void AcquireSync(UInt64 key, DWORD milliseconds);

        /// <summary>
        /// Using a key, releases exclusive rendering access to a shared resource
        /// <para>(Also see DirectX SDK: IDXGIKeyedMutex::ReleaseSync)</para>
        /// </summary>
        /// <param name="key">A value that indicates which device to give access to. This method will succeed when the device that currently owns the surface calls the KeyedMutex.ReleaseSync method using the same value. This value can be any UINT64 value.</param>
        void ReleaseSync(UInt64 key);

		static KeyedMutex^ FromResource(DirectUnknown^ unknown);

    internal:

		KeyedMutex(void)
        { }

		KeyedMutex(IDXGIKeyedMutex* pNativeIDXGIKeyedMutex)
			: DeviceSubObject(pNativeIDXGIKeyedMutex)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(IDXGIKeyedMutex)); }
        }
    };
} } } }
