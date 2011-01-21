//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DXGIFactory.h"

using namespace System;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics {

ref class Adapter1;

    // REVIEW: the DirectX API is somewhat unfriendly, in that the caller is required
    // to check for an out-of-date factory and create a new one if needed. The CodePack
    // API could easily handle this internally, so that only a single factory instance
    // ever need be created. The current problem with doing that is that, at least for now,
    // CodePack object instances have a one-to-one connection with a specific unmanaged
    // interface instance, and the client code can even get this instance and use it
    // directly if they like (see DirectUnknown::NativeInterface, for example). Having this
    // class automatically recreate the underlying unmanaged interface as needed could
    // potentially cause confusion for the client code, if they retrieve the unmanaged
    // interface and then expect that interface to always match the one used for the instance
    // of this class.

    /// <summary>
    /// The Factory1 interface implements methods for generating Graphics objects.
    /// <para>(Also see DirectX SDK: IDXGIFactory1)</para>
    /// </summary>
    public ref class Factory1 :
        public Factory
    {
    public: 
        /// <summary>
        /// Enumerates both local and remote adapters (video cards).
        /// <para>(Also see DirectX SDK: IDXGIFactory1::EnumAdapters1)</para>
        /// </summary>
        /// <remarks>
        /// Note that if the IsCurrent property returns false, the adapter collection
        /// returned by this factory instance is stale. To get the current collection
        /// of adapters, a new factory instance must be created.
        /// </remarks>
        property ReadOnlyCollection<Adapter1^>^ Adapters
        {
            ReadOnlyCollection<Adapter1^>^ get(void) new;
        }

        /// <summary>
        /// Informs application the possible need to re-enumerate adapters -- new adapter(s) have become available, current adapter(s) become unavailable.
        /// Called by Direct3D 10.1 Command Remoting applications to handle Remote Desktop Services session transitions.
        /// <para>(Also see DirectX SDK: IDXGIFactory1::IsCurrent)</para>
        /// </summary>
        property Boolean IsCurrent
        {
            Boolean get();
        }

        /// <summary>
        /// Creates a Graphics 1.1 factory that generates objects used to enumerate and specify video graphics settings.
        /// <para>(Also see DirectX SDK: CreateDXGIFactory1() Function.)</para>
        /// </summary>
        /// <returns>Factory1 Object.</returns>
        static Factory1^ Create();

    internal:

        Factory1(void)
        { }

        Factory1(IDXGIFactory1* pNativeIDXGIFactory1) : Factory(pNativeIDXGIFactory1)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(IDXGIFactory1)); }
        }

    private:

        ReadOnlyCollection<Adapter1^>^ adapters;
    };
} } } }
