// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

using namespace System;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX {

/// <summary>
/// Base class for all DirectX exceptions.
/// </summary>
CA_SUPPRESS_MESSAGE("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")
public ref class DirectXException : public Exception
{
public:

    DirectXException(int hr) : System::Exception("Non-specific DirectX error. Check ErrorCode property.")
    { HResult = hr; }
    
    DirectXException(String^ message, int hr) : Exception(message)
    { HResult = hr; }
        
    DirectXException(String^ message, Exception^ innerException, int hr) :
        System::Exception(message, innerException)
    { HResult = hr; }

    /// <summary>
    /// Default constructor
    /// </summary>
    DirectXException(void) : System::Exception("Non-specific DirectX error. Check ErrorCode property.")
    { }
    
    DirectXException(String^ message) : Exception(message)
    { }
        
    DirectXException(String^ message, Exception^ innerException) :
        System::Exception(message, innerException)
    { }

    /// <summary>
    /// The error code returned by a DirectX API
    /// </summary>
    property int ErrorCode
    {
        int get() { return Exception::HResult; }
    }

protected:

    DirectXException(System::Runtime::Serialization::SerializationInfo^ info,
            System::Runtime::Serialization::StreamingContext context, int hr) :
        Exception(info, context)
    { HResult = hr; }

    DirectXException(System::Runtime::Serialization::SerializationInfo^ info,
            System::Runtime::Serialization::StreamingContext context) :
        Exception(info, context)
    { }

};

} } }
