//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "DirectObject.h"

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

    public value class IncludeData
    {
    public:

        property IntPtr Pointer
        {
            IntPtr get(void) { return pointer; }
        }

        property UInt32 Length
        {
            UInt32 get(void) { return length; }
        }

        static bool operator ==(IncludeData data1, IncludeData data2)
        {
            return data1.pointer == data2.pointer &&
                data1.length == data2.length;
        }

        static bool operator !=(IncludeData data1, IncludeData data2)
        {
            return !(data1 == data2);
        }

        virtual bool Equals(Object^ obj) override
        {
            if (obj->GetType() != IncludeData::typeid)
            {
                return false;
            }

            return *this == safe_cast<IncludeData>(obj);
        }

        virtual int GetHashCode(void) override
        {
            int hashCode = 0;

            hashCode = hashCode * 31 + pointer.GetHashCode();
            hashCode = hashCode * 31 + length.GetHashCode();

            return hashCode;
        }

    internal:

        IncludeData(IntPtr pointer, UInt32 length) : pointer(pointer), length(length) { }

    private:

        IntPtr pointer;
        UInt32 length;
    };

    /// <summary>
    /// An include interface allows an application to create user-overridable methods for opening and closing files when loading an effect from memory. This class does not inherit from anything, but does declare the following methods:
    /// <para>(Also see DirectX SDK: ID3D10Include)</para>
    /// </summary>
    public ref class Include :
        public DirectObject
    {
    public: 
        /// <summary>
        /// A user-implemented method for closing a shader #include file.
        /// <para>(Also see DirectX SDK: ID3D10Include::Close)</para>
        /// </summary>
        /// <param name="data">Pointer to the returned buffer that contains the include directives. This is the pointer that was returned by the corresponding Include.Open call.</param>
        void Close(IncludeData data);

        /// <summary>
        /// A user-implemented method for opening and reading the contents of a shader #include file.
        /// <para>(Also see DirectX SDK: ID3D10Include::Open)</para>
        /// </summary>
        /// <param name="includeType">The location of the #include file. See IncludeType.</param>
        /// <param name="fileName">Name of the #include file.</param>
        /// <param name="parentData">Pointer to the container that includes the #include file.</param>
        /// <returns>The data that contains the include directives. This data remains valid until Include.Close() is called.</returns>
        IncludeData Open(IncludeType includeType, String^ fileName, IntPtr parentData);
  
		/// <summary>
		/// Public constructor to allow client code to create managed sub-classes that implement the
		/// interface.
		/// </summary>
		/// <param name="nativeInterface">The unmanaged interface pointer for the client's implementation</param>
		Include(IntPtr nativeInterface);

    internal:

        Include(void)
        { }

		// REVIEW: unused for now. It will be needed if and when the DirectHelpers::CreateIUnknownWrapper
		// is fixed to not use the default constructor.

        CA_SUPPRESS_MESSAGE("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")
        Include(ID3D10Include* pNativeID3D10Include) : 
            DirectObject(pNativeID3D10Include)
        { }
    };
} } } }
