//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectUnknown.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

    /// <summary>
    /// A multithread interface accesses multithread settings and can only be used if the thread-safe layer is turned on.
    /// <para>(Also see DirectX SDK: ID3D10Multithread)</para>
    /// </summary>
    public ref class Multithread :
        public DirectUnknown
    {
    public: 
        /// <summary>
        /// Enter a device's critical section.
        /// <para>(Also see DirectX SDK: ID3D10Multithread::Enter)</para>
        /// </summary>
        void Enter();

         /// <summary>
        /// Leave a device's critical section.
        /// <para>(Also see DirectX SDK: ID3D10Multithread::Leave)</para>
        /// </summary>
        void Leave();

       /// <summary>
        /// Find out if multithreading is turned on or not.
        /// <para>(Also see DirectX SDK: ID3D10Multithread::GetMultithreadProtected, ID3D10Multithread::SetMultithreadProtected)</para>
        /// </summary>
        property Boolean IsMultithreadProtected
        {
            Boolean get();
            void set(Boolean value);
        }

    internal:

		Multithread(void)
        { }

        Multithread(ID3D10Multithread* pNativeID3D10Multithread) :
            DirectUnknown(pNativeID3D10Multithread)
        { }

        static property System::Guid Guid
        {
            System::Guid get() { return Utilities::Convert::SystemGuidFromGUID(__uuidof(ID3D10Multithread)); }
        }
    };
} } } }
