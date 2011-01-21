// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectXException.h"

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct2D1 {

/// <summary>
/// Base class for all Direct2D exceptions
/// </summary>
public ref class Direct2DException : public DirectXException
{

public:
    Direct2DException(void) : DirectXException("Non-specific Direct2D error was returned.")
    { }
    
    Direct2DException(int hr) : DirectXException("Direct2D error was returned. Check ErrorCode property.", hr)
    { }
    
    Direct2DException(String^ message, int hr) : DirectXException(message, hr)
    { }
    
    Direct2DException(String^ message, Exception^ innerException, int hr) : 
        DirectXException(message, innerException, hr)
    { }

    Direct2DException(String^ message) : DirectXException(message)
    { }
    
    Direct2DException(String^ message, Exception^ innerException) : 
        DirectXException(message, innerException)
    { }

protected:

    Direct2DException(System::Runtime::Serialization::SerializationInfo^ info,
        System::Runtime::Serialization::StreamingContext context, int hr) :
    DirectXException(info, context, hr)
    { }

    Direct2DException(System::Runtime::Serialization::SerializationInfo^ info,
        System::Runtime::Serialization::StreamingContext context) :
    DirectXException(info, context)
    { }

};
} } } }
