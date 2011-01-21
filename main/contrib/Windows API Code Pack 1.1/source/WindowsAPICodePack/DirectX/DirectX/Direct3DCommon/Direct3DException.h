// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectXException.h"

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D {


/// <summary>
/// An exception thrown by Direct3D.
/// </summary>
public ref class Direct3DException : public DirectXException
{
public:
    Direct3DException(void) : DirectXException("Non-specific Direct3D error was returned.")
    { }
    
    Direct3DException(int hr) : DirectXException("Direct3D error was returned. Check ErrorCode property.", hr)
    { }
    
    Direct3DException(String^ message, int hr) : DirectXException(message, hr)
    { }
    
    Direct3DException(String^ message, Exception^ innerException, int hr) : 
        DirectXException(message, innerException, hr)
    { }

    Direct3DException(String^ message) : DirectXException(message)
    { }
    
    Direct3DException(String^ message, Exception^ innerException) : 
        DirectXException(message, innerException)
    { }

protected:

    Direct3DException(System::Runtime::Serialization::SerializationInfo^ info,
        System::Runtime::Serialization::StreamingContext context, int hr) :
    DirectXException(info, context, hr)
    { }

    Direct3DException(System::Runtime::Serialization::SerializationInfo^ info,
        System::Runtime::Serialization::StreamingContext context) :
    DirectXException(info, context)
    { }

};
} } } }
