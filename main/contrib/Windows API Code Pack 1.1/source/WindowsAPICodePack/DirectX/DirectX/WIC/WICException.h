// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

#include "DirectXException.h"

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX;

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace WindowsImagingComponent { 

/// <summary>
/// An exception thrown by a WIC (Windows Imaging Component) method.
/// </summary>
public ref class ImagingException : public DirectXException
{
public:

    ImagingException(void)
        : DirectXException("Non-specific Windows Imaging Component error was returned.")
	{ }
    
    ImagingException(int hr)
        : DirectXException("Windows Imaging Component error was returned. Check ErrorCode property.", hr)
    { }
    
    ImagingException(String^ message, int hr) : DirectXException(message, hr)
    { }

    ImagingException(String^ message, Exception^ innerException, int hr) : 
        DirectXException(message, innerException, hr)
    { }

    ImagingException(String^ message) : DirectXException(message)
    { }

    ImagingException(String^ message, Exception^ innerException) : 
        DirectXException(message, innerException)
    { }

protected:

	ImagingException(System::Runtime::Serialization::SerializationInfo^ info,
            System::Runtime::Serialization::StreamingContext context, int hr) :
        DirectXException(info, context, hr)
	{ }

	ImagingException(System::Runtime::Serialization::SerializationInfo^ info,
            System::Runtime::Serialization::StreamingContext context) :
        DirectXException(info, context)
	{ }

};
} } } }