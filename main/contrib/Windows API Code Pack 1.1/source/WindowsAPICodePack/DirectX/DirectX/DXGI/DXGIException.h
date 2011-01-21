// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectXException.h"

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Graphics { 

/// <summary>
/// An exception thrown by Graphics.
/// </summary>
public ref class GraphicsException : public DirectXException
{
public:
    GraphicsException(void) : DirectXException("Non-specific DirectX Graphics Infrastructure error was returned.")
    { }
    
    GraphicsException(int hr) : DirectXException("DirectX Graphics Infrastructure error was returned. Check ErrorCode property.", hr)
    { }
    
    GraphicsException(String^ message, int hr) : DirectXException(message, hr)
    { }
    
    GraphicsException(String^ message, Exception^ innerException, int hr) : 
        DirectXException(message, innerException, hr)
    { }

    GraphicsException(String^ message) : DirectXException(message)
    { }
    
    GraphicsException(String^ message, Exception^ innerException) : 
        DirectXException(message, innerException)
    { }

protected:

    GraphicsException(System::Runtime::Serialization::SerializationInfo^ info,
        System::Runtime::Serialization::StreamingContext context, int hr) :
    DirectXException(info, context, hr)
    { }

    GraphicsException(System::Runtime::Serialization::SerializationInfo^ info,
        System::Runtime::Serialization::StreamingContext context) :
    DirectXException(info, context)
    { }

};
} } } }
