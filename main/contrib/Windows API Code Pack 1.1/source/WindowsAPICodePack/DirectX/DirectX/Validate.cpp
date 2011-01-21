//Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "Validate.h"

using namespace System;
using namespace Microsoft::WindowsAPICodePack::DirectX::Utilities;
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct2D1;

void Validate::VerifyResult(HRESULT hr)
{
    if (SUCCEEDED(hr))
        return;
    else
        throw GetExceptionForHR(hr);
}

void Validate::VerifyBoolean(bool success)
{
    if (!success)
    {
        throw GetExceptionForHR(GetLastError());
    }
}

Exception^ Validate::GetExceptionForHR(HRESULT hr)
{
    // Special-case mapping from non-facility error to Direct2D (why?)
    if (hr == static_cast<HRESULT>(ERROR_INSUFFICIENT_BUFFER))
    {
        return gcnew Direct2DException("The supplied buffer is too small to accommodate the data.", (int)hr);
    }

    Exception^ exception = nullptr;

    switch (HRESULT_FACILITY(hr))
    {
    case 0x0007:
        exception = GetWinErrorException(hr);

    case _FACD3D10:
        exception = GetDirect3D10Exception(hr);

    case _FACD3D11:
        exception = GetDirect3D11Exception(hr);

    case _FACDXGI:
        exception = GetDirectXGraphicsException(hr);

    case FACILITY_D2D:
        exception = GetDirect2DException(hr);

    case FACILITY_WINCODEC_ERR:
        exception = GetWindowsImagingException(hr);

    default:
        break;
    }

    return exception != nullptr ? exception : Marshal::GetExceptionForHR(hr);
}

Exception^ Validate::GetWinErrorException(HRESULT hr)
{
    switch (hr)
    {
    // General Errors

    // facility: 0x0007
    case E_INVALIDARG: 
        return gcnew ArgumentException ("An invalid parameter was passed to the returning function.");

    // facility: 0x0007
    case E_OUTOFMEMORY:
        // REVIEW: this is a little better than OutOfMemoryException. However, it's
        // still typically used to indicate _before_ an allocation when the allocation
        // is not expected to succeed. I remain unconvinced that we should throw a system
        // exception here, but for the moment I don't have a better candidate in mind.
        return gcnew InsufficientMemoryException("Could not allocate sufficient memory to complete the call.");

    default:
        return nullptr;
    }
}

Exception^ Validate::GetDirect3D10Exception(HRESULT hr)
{
    switch (hr)
    {
    // facility: 0x0879 (_FACD3D10)
    case D3D10_ERROR_FILE_NOT_FOUND: 
        return gcnew FileNotFoundException();

    // facility: 0x0879
    case D3D10_ERROR_TOO_MANY_UNIQUE_STATE_OBJECTS: 
        return gcnew Direct3DException("Too many unique instances", hr);

    default:
        return nullptr;
    }
}

Exception^ Validate::GetDirect3D11Exception(HRESULT hr)
{
    switch (hr)
    {
    // facility: 0x87c (_FACD3D11)
    case D3D11_ERROR_FILE_NOT_FOUND:
        return gcnew FileNotFoundException();

    // facility: 0x87c
    case D3D11_ERROR_TOO_MANY_UNIQUE_STATE_OBJECTS:
        return gcnew Direct3DException("Too many unique instances", hr);

    default:
        return nullptr;
    }
}

Exception^ Validate::GetDirectXGraphicsException(HRESULT hr)
{
	EnsureGraphicsExceptionDescriptions();

	return GetException(graphicsExceptionDescriptions, ExceptionType::Graphics,
		gcnew Func<HRESULT, String^, Exception^>(DirectXGraphicsExceptionFactory), hr);
}

Exception^ Validate::GetDirect2DException(HRESULT hr)
{
	EnsureDirect2DExceptionDescriptions();

	return GetException(direct2DExceptionDescriptions, ExceptionType::Direct2D,
		gcnew Func<HRESULT, String^, Exception^>(Direct2DExceptionFactory), hr);
}

Exception^ Validate::GetWindowsImagingException(HRESULT hr)
{
	EnsureWindowsImagingExceptionDescriptions();

	return GetException(windowsImagingExceptionDescriptions, ExceptionType::Direct2D,
		gcnew Func<HRESULT, String^, Exception^>(WindowsImagingExceptionFactory), hr);
}

void Validate::EnsureGraphicsExceptionDescriptions(void)
{
    if (graphicsExceptionDescriptions != nullptr)
    {
        return;
    }

    Dictionary<HRESULT, ExceptionDescription>^ descriptions =  gcnew Dictionary<HRESULT, ExceptionDescription>();

    descriptions->Add(DXGI_ERROR_DEVICE_HUNG, ExceptionDescription("The application's device failed due to badly formed commands sent by the application. This is an design-time issue that should be investigated and fixed.", ExceptionType::Graphics));
    descriptions->Add(DXGI_ERROR_DEVICE_REMOVED, ExceptionDescription("The video card has been physically removed from the system, or a driver upgrade for the video card has occurred. The application should destroy and recreate the device. For help debugging the problem, call Device.GetDeviceRemovedReason().", ExceptionType::Graphics));
    descriptions->Add(DXGI_ERROR_DEVICE_RESET, ExceptionDescription("The device failed due to a badly formed command. This is a run-time issue; The application should destroy and recreate the device.", ExceptionType::Graphics));
    descriptions->Add(DXGI_ERROR_DRIVER_INTERNAL_ERROR, ExceptionDescription("The driver encountered a problem and was put into the device removed state.", ExceptionType::Graphics));
    descriptions->Add(DXGI_ERROR_FRAME_STATISTICS_DISJOINT, ExceptionDescription("The requested functionality is not supported by the device or the driver.", ExceptionType::NotSupported));
    descriptions->Add(DXGI_ERROR_GRAPHICS_VIDPN_SOURCE_IN_USE, ExceptionDescription("The requested functionality is not supported by the device or the driver.", ExceptionType::Graphics));
    descriptions->Add(DXGI_ERROR_INVALID_CALL, ExceptionDescription("The application provided invalid parameter data; this must be debugged and fixed before the application is released.", ExceptionType::ArgumentOutOfRange));
    descriptions->Add(DXGI_ERROR_MORE_DATA, ExceptionDescription("The buffer supplied by the application is not big enough to hold the requested data.", ExceptionType::Graphics));
    descriptions->Add(DXGI_ERROR_NONEXCLUSIVE, ExceptionDescription("The application attempted to acquire exclusive ownership of an output, but failed because some other application (or device within the application) has already acquired ownership.", ExceptionType::Graphics));
    descriptions->Add(DXGI_ERROR_NOT_CURRENTLY_AVAILABLE, ExceptionDescription("The requested functionality is not supported by the device or the driver.", ExceptionType::Graphics));
    descriptions->Add(DXGI_ERROR_NOT_FOUND, ExceptionDescription("When calling GraphicsObject.GetPrivateData(), the GUID passed in is not recognized as one previously passed to GraphicsObject.SetPrivateData() or GraphicsObject.SetPrivateDataInterface().", ExceptionType::Graphics));
    descriptions->Add(DXGI_ERROR_REMOTE_CLIENT_DISCONNECTED, ExceptionDescription("The application's remote device has been removed due to session disconnect or network disconnect. The application should call Graphics.Factory1.IsCurrent() to find out when the remote device becomes available again.", ExceptionType::Graphics));
    descriptions->Add(DXGI_ERROR_REMOTE_OUTOFMEMORY, ExceptionDescription("The application's remote device has failed due to lack of memory or machine error. The application should destroy and recreate resources using less memory.", ExceptionType::InsufficientMemory));
    descriptions->Add(DXGI_ERROR_WAS_STILL_DRAWING, ExceptionDescription("The device was busy, and did not schedule the requested task. This error only applies to Asynchronous queries in Direct3D 10.", ExceptionType::Graphics));
    descriptions->Add(DXGI_ERROR_UNSUPPORTED, ExceptionDescription("The requested functionality is not supported by the device or the driver.", ExceptionType::NotSupported));

	graphicsExceptionDescriptions = descriptions;
}

void Validate::EnsureDirect2DExceptionDescriptions(void)
{
	if (direct2DExceptionDescriptions != nullptr)
	{
		return;
	}

	Dictionary<HRESULT, ExceptionDescription>^ descriptions = gcnew Dictionary<HRESULT, ExceptionDescription>();

	descriptions->Add(D2DERR_BAD_NUMBER, ExceptionDescription("The number is invalid.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_DISPLAY_FORMAT_NOT_SUPPORTED, ExceptionDescription("The display format to render is not supported by the hardware device.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_DISPLAY_STATE_INVALID, ExceptionDescription("A valid display state could not be determined.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_EXCEEDS_MAX_BITMAP_SIZE, ExceptionDescription("The requested size is larger than the guaranteed supported texture size.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_INCOMPATIBLE_BRUSH_TYPES, ExceptionDescription("The brush types are incompatible for the call.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_INTERNAL_ERROR, ExceptionDescription("The application should close this instance of Direct2D and restart it as a new process.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_INVALID_CALL, ExceptionDescription("A call to this method is invalid.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_LAYER_ALREADY_IN_USE, ExceptionDescription("The application attempted to reuse a layer resource that has not yet been popped off the stack.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_MAX_TEXTURE_SIZE_EXCEEDED, ExceptionDescription("The requested DX surface size exceeds the maximum texture size.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_NO_HARDWARE_DEVICE, ExceptionDescription("There is no hardware rendering device available for this operation.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_NOT_INITIALIZED, ExceptionDescription("The object has not yet been initialized.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_POP_CALL_DID_NOT_MATCH_PUSH, ExceptionDescription("The application attempted to pop a layer off the stack when a clip was at the top, or pop a clip off the stack when a layer was at the top.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_PUSH_POP_UNBALANCED, ExceptionDescription("The application did not pop all clips and layers off the stack, or it attempted to pop too many clips or layers off the stack.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_RECREATE_TARGET, ExceptionDescription("A presentation error has occurred that may be recoverable. The caller needs to re-create the render target, render the entire frame again, and reattempt presentation.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_RENDER_TARGET_HAS_LAYER_OR_CLIPRECT, ExceptionDescription("The application attempted to copy the contents of a render target before popping all layers and clips off the stack.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_SCANNER_FAILED, ExceptionDescription("The geomery scanner failed to process the data.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_SCREEN_ACCESS_DENIED, ExceptionDescription("Direct2D could not access the screen.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_SHADER_COMPILE_FAILED, ExceptionDescription("Shader compilation failed.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_TARGET_NOT_GDI_COMPATIBLE, ExceptionDescription("The render target is not compatible with GDI.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_TEXT_EFFECT_IS_WRONG_TYPE, ExceptionDescription("A text client drawing effect object is of the wrong type.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_TEXT_RENDERER_NOT_RELEASED, ExceptionDescription("An application is holding a reference to the IDWriteTextRenderer interface after the corresponding DrawText or DrawTextLayout call has returned.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_TOO_MANY_SHADER_ELEMENTS, ExceptionDescription("Shader construction failed because it was too complex.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_UNSUPPORTED_OPERATION, ExceptionDescription("The requested operation is not supported.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_UNSUPPORTED_VERSION, ExceptionDescription("The requested Direct2D version is not supported.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_WIN32_ERROR, ExceptionDescription("An unknown Win32 failure occurred.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_WRONG_FACTORY, ExceptionDescription("Objects used together were not all created from the same factory instance.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_WRONG_RESOURCE_DOMAIN, ExceptionDescription("The resource used was created by a render target in a different resource domain.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_WRONG_STATE, ExceptionDescription("The object was not in the correct state to process the method.", ExceptionType::Direct2D));
	descriptions->Add(D2DERR_ZERO_VECTOR, ExceptionDescription("The supplied vector is zero.", ExceptionType::Direct2D));

	direct2DExceptionDescriptions = descriptions;
}

void Validate::EnsureWindowsImagingExceptionDescriptions(void)
{
	if (windowsImagingExceptionDescriptions != nullptr)
	{
		return;
	}

	Dictionary<HRESULT, ExceptionDescription>^ descriptions = gcnew Dictionary<HRESULT, ExceptionDescription>();

    // DirectX knows this one
	descriptions->Add(WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT, ExceptionDescription("WIC Error was returned: Unsupported pixel format.", ExceptionType::Imaging));
    
    // Windows knows this one (it's actually mapped to an existing COM error code)
	descriptions->Add(WINCODEC_ERR_VALUEOVERFLOW, ExceptionDescription("WIC Error was returned: Value overflow.", ExceptionType::Imaging));

    // WIC Errors not supported by system formatting (identified empirically)
	descriptions->Add(WINCODEC_ERR_ALREADYLOCKED, ExceptionDescription("WIC Error was returned: Already locked .", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_BADHEADER, ExceptionDescription("WIC Error was returned: Bad header.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_BADIMAGE, ExceptionDescription("WIC Error was returned: Bad image.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_BADMETADATAHEADER, ExceptionDescription("WIC Error was returned: Bad meta data header.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_BADSTREAMDATA, ExceptionDescription("WIC Error was returned: Bad stream data.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_CODECNOTHUMBNAIL, ExceptionDescription("WIC Error was returned: Codec no thumbnail.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_CODECPRESENT, ExceptionDescription("WIC Error was returned: Codec present.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_CODECTOOMANYSCANLINES, ExceptionDescription("WIC Error was returned: Codec too many scan lines.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_COMPONENTINITIALIZEFAILURE, ExceptionDescription("WIC Error was returned: Component initialize failure.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_COMPONENTNOTFOUND, ExceptionDescription("WIC Error was returned: Component not found.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_DUPLICATEMETADATAPRESENT, ExceptionDescription("WIC Error was returned: Duplicate meta data present.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_FRAMEMISSING, ExceptionDescription("WIC Error was returned: Frame missing.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_IMAGESIZEOUTOFRANGE, ExceptionDescription("WIC Error was returned: Image size out of range.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_INSUFFICIENTBUFFER, ExceptionDescription("WIC Error was returned: Insufficient buffer.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_INTERNALERROR, ExceptionDescription("WIC Error was returned: Internal error.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_INVALIDQUERYCHARACTER, ExceptionDescription("WIC Error was returned: Invalid query character.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_INVALIDQUERYREQUEST, ExceptionDescription("WIC Error was returned: Invalid query request.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_INVALIDREGISTRATION, ExceptionDescription("WIC Error was returned: Invalid registration.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_NOTINITIALIZED, ExceptionDescription("WIC Error was returned: Not initialized.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_PALETTEUNAVAILABLE, ExceptionDescription("WIC Error was returned: Palette unavailable.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_PROPERTYNOTFOUND, ExceptionDescription("WIC Error was returned: Property not found.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_PROPERTYNOTSUPPORTED, ExceptionDescription("WIC Error was returned: Property not supported.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_PROPERTYSIZE, ExceptionDescription("WIC Error was returned: Property size.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_PROPERTYUNEXPECTEDTYPE, ExceptionDescription("WIC Error was returned: Property unexpected type.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_REQUESTONLYVALIDATMETADATAROOT, ExceptionDescription("WIC Error was returned: Request only valid at meta data root.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_SOURCERECTDOESNOTMATCHDIMENSIONS, ExceptionDescription("WIC Error was returned: Source rectangle does not match dimensions.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_STREAMWRITE, ExceptionDescription("WIC Error was returned: Stream write.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_STREAMREAD, ExceptionDescription("WIC Error was returned: Stream read.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_STREAMNOTAVAILABLE, ExceptionDescription("WIC Error was returned: Stream not available.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_TOOMUCHMETADATA, ExceptionDescription("WIC Error was returned: Too much meta data.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_UNKNOWNIMAGEFORMAT, ExceptionDescription("WIC Error was returned: Unknown image format.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_UNEXPECTEDMETADATATYPE, ExceptionDescription("WIC Error was returned: Unexpected meta data type.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_UNEXPECTEDSIZE, ExceptionDescription("WIC Error was returned: Unexpected size.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_UNSUPPORTEDOPERATION, ExceptionDescription("WIC Error was returned: Unsupported operation.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_UNSUPPORTEDVERSION, ExceptionDescription("WIC Error was returned: Unsupported version.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_VALUEOUTOFRANGE, ExceptionDescription("WIC Error was returned: Value out of range.", ExceptionType::Imaging));
	descriptions->Add(WINCODEC_ERR_WRONGSTATE, ExceptionDescription("WIC Error was returned: Wrong state.", ExceptionType::Imaging));

	windowsImagingExceptionDescriptions = descriptions;
}

Exception^ Validate::GetException(Dictionary<HRESULT, ExceptionDescription>^ descriptions,
	ExceptionType defaultType, Func<HRESULT, String^, Exception^>^ defaultFactory, HRESULT hr)
{
	ExceptionDescription description;

	if (descriptions->TryGetValue(hr, description))
	{
		if (description.ExceptionType == defaultType)
		{
			switch (description.ExceptionType)
			{
			case ExceptionType::ArgumentOutOfRange:
				return gcnew ArgumentOutOfRangeException(description.Text, defaultFactory(hr, nullptr));

			case ExceptionType::InsufficientMemory:
				return gcnew InsufficientMemoryException(description.Text, defaultFactory(hr, nullptr));

			case ExceptionType::NotSupported:
				return gcnew NotSupportedException(description.Text, defaultFactory(hr, nullptr));

			default:
    			return defaultFactory(hr, description.Text);
			}
		}
	}

	return nullptr;
}

Exception^ Validate::DirectXGraphicsExceptionFactory(HRESULT hr, System::String ^text)
{
	if (text != nullptr)
	{
		return gcnew GraphicsException(text, hr);
	}

	return gcnew GraphicsException(hr);
}

Exception^ Validate::Direct2DExceptionFactory(HRESULT hr, System::String ^text)
{
	if (text != nullptr)
	{
		return gcnew Direct2DException(text, hr);
	}

	return gcnew Direct2DException(hr);
}

Exception^ Validate::WindowsImagingExceptionFactory(HRESULT hr, System::String ^text)
{
	if (text != nullptr)
	{
        return gcnew ImagingException(text, hr);
	}

	return gcnew ImagingException(hr);
}

void Validate::CheckNull(Object^ obj, String^ argName)
{
    if (obj == nullptr)
    {
        throw gcnew ArgumentNullException(argName);
    }
}