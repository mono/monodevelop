//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

// This is defined in DX SDK
#ifndef D2DERR_WRONG_RESOURCE_DOMAIN
    #define D2DERR_WRONG_RESOURCE_DOMAIN MAKE_D2DHR_ERR(0x015)
#endif

namespace Microsoft { namespace WindowsAPICodePack { namespace DirectX {


// REVIEW: most of these enum value names have the word "Error" in them; but given
// the name of the enum type, that seems redundant.

/// <summary>
/// Error codes that can be returned by DXGI, Direct3D, Direct2D, DirectWrite or WIC
/// </summary>
public enum class ErrorCode
{
    /// <summary>
    /// The method succeeded without an error. 
    /// </summary>
    Success = S_OK,

    /// <summary>
    /// Alternate success value, indicating a successful but nonstandard completion (the precise meaning depends on context).
    /// </summary>
    FalseSuccess =  S_FALSE,

    /// <summary>
    /// An invalid parameter was passed to the returning function.
    /// </summary>
    InvalidArgument = E_INVALIDARG,

    /// <summary>
    /// Could not allocate sufficient memory to complete the call.
    /// </summary>
    OutOfMemory = E_OUTOFMEMORY,

    /// <summary>
    /// Unspecified or generic error.
    /// </summary>
    Fail = E_FAIL,
    
    /// <summary>
    /// Not implemented.
    /// </summary>
    NotImplemented = E_NOTIMPL,
    

    /// <summary>
    /// Aborted.
    /// </summary>
    Aborted = E_ABORT,    

    /// <summary>
    /// Access Denied.
    /// </summary>
    AccessDenied = E_ACCESSDENIED,
    
    /// <summary>
    /// The application's device failed due to badly formed commands sent by the application. This is an design-time issue that should be investigated and fixed. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_DEVICE_HUNG )</para>
    /// </summary>
    GraphicsErrorDeviceHung = DXGI_ERROR_DEVICE_HUNG,

    /// <summary>
    /// The video card has been physically removed from the system, or a driver upgrade for the video card has occurred. The application should destroy and recreate the device. For help debugging the problem, call Device.GetDeviceRemovedReason(). 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_DEVICE_REMOVED )</para>
    /// </summary>
    GraphicsErrorDeviceRemoved = DXGI_ERROR_DEVICE_REMOVED,

    /// <summary>
    /// The device failed due to a badly formed command. This is a run-time issue; The application should destroy and recreate the device. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_DEVICE_RESET )</para>
    /// </summary>
    GraphicsErrorDeviceReset = DXGI_ERROR_DEVICE_RESET,
   
    /// <summary>
    /// The driver encountered a problem and was put into the device removed state. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_DRIVER_INTERNAL_ERROR )</para>
    /// </summary>
    GraphicsErrorDriverInternalError = DXGI_ERROR_DRIVER_INTERNAL_ERROR,
    
    /// <summary>
    /// The requested functionality is not supported by the device or the driver. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_FRAME_STATISTICS_DISJOINT )</para>
    /// </summary>    
    GraphicsErrorFrameStatisticsDisjoint = DXGI_ERROR_FRAME_STATISTICS_DISJOINT,

    /// <summary>
    /// The requested functionality is not supported by the device or the driver. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_GRAPHICS_VIDPN_SOURCE_IN_USE )</para>
    /// </summary>

    // "VidPN" == "Video Present Network"
    GraphicsErrorGraphicsVideoPresentNetworkSourceInUse = DXGI_ERROR_GRAPHICS_VIDPN_SOURCE_IN_USE,
    
    /// <summary>
    /// The application provided invalid parameter data; this must be debugged and fixed before the application is released. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_INVALID_CALL )</para>
    /// </summary>
    GraphicsErrorInvalidCall = DXGI_ERROR_INVALID_CALL,
    
    /// <summary>
    /// The buffer supplied by the application is not big enough to hold the requested data. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_MORE_DATA )</para>
    /// </summary>
    GraphicsErrorMoreData = DXGI_ERROR_MORE_DATA,
   
    /// <summary>
    /// The application attempted to acquire exclusive ownership of an output, but failed because some other application (or device within the application) has already acquired ownership. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_NONEXCLUSIVE )</para>
    /// </summary>
    GraphicsErrorNonexclusive = DXGI_ERROR_NONEXCLUSIVE,
    
    /// <summary>
    /// The requested functionality is not supported by the device or the driver. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_NOT_CURRENTLY_AVAILABLE )</para>
    /// </summary>
    GraphicsErrorNotCurrentlyAvailable = DXGI_ERROR_NOT_CURRENTLY_AVAILABLE,

    /// <summary>
    /// When calling GraphicsObject.GetPrivateData, the GUID passed in is not recognized as one previously passed to GraphicsObject.SetPrivateData or GraphicsObject.SetPrivateDataInterface. 
    /// When calling GraphicsFactory.EnumAdapters or Adapter.EnumOutputs, the enumerated ordinal is out of range. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_NOT_FOUND )</para>
    /// </summary>
    GraphicsErrorNotFound = DXGI_ERROR_NOT_FOUND,
   
    /// <summary>
    /// The application's remote device has been removed due to session disconnect or network disconnect. The application should call Graphics.Factory1::IsCurrent to find out when the remote device becomes available again. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_REMOTE_CLIENT_DISCONNECTED )</para>
    /// </summary>
    GraphicsErrorRemoteClientDisconnected = DXGI_ERROR_REMOTE_CLIENT_DISCONNECTED,

    /// <summary>
    /// The application's remote device has failed due to lack of memory or machine error. The application should destroy and recreate resources using less memory. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_REMOTE_OUTOFMEMORY )</para>
    /// </summary>
    GraphicsErrorRemoteOutOfMemory = DXGI_ERROR_REMOTE_OUTOFMEMORY,
    
    /// <summary>
    /// The device was busy, and did not schedule the requested task. This error only applies to asynchronous queries in Direct3D 10. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_WAS_STILL_DRAWING )</para>
    /// </summary>
    GraphicsErrorWasStillDrawing = DXGI_ERROR_WAS_STILL_DRAWING,
   
    /// <summary>
    /// The requested functionality is not supported by the device or the driver. 
    /// <para>(Also see DirectX SDK: DXGI_ERROR_UNSUPPORTED )</para>
    /// </summary>
    GraphicsErrorUnsupported = DXGI_ERROR_UNSUPPORTED,   

    /// <summary>
    /// The file was not found. 
    /// <para>(Also see DirectX SDK: D3D11_ERROR_FILE_NOT_FOUND )</para>
    /// </summary>
    Direct3D11ErrorFileNotFound =  D3D11_ERROR_FILE_NOT_FOUND,

    /// <summary>
    /// There are too many unique instances of a particular type of state object.
    /// <para>(Also see DirectX SDK: D3D11_ERROR_TOO_MANY_UNIQUE_STATE_OBJECTS )</para>
    /// </summary>
    Direct3D11ErrorTooManyUniqueInstances =  D3D11_ERROR_TOO_MANY_UNIQUE_STATE_OBJECTS,

    /// <summary>
    /// There are too many unique instances of a particular type of state object.
    /// <para>(Also see DirectX SDK: D3D10_ERROR_TOO_MANY_UNIQUE_STATE_OBJECTS )</para>
    /// </summary>
    Direct3D10ErrorTooManyUniqueInstances =  D3D10_ERROR_TOO_MANY_UNIQUE_STATE_OBJECTS,

    /// <summary>
    /// The file was not found. 
    /// <para>(Also see DirectX SDK: D3D10_ERROR_FILE_NOT_FOUND )</para>
    /// </summary>
    Direct3D10ErrorFileNotFound =  D3D10_ERROR_FILE_NOT_FOUND,


    /// <summary>
    /// The number is invalid
    /// </summary>
    Direct2DErrorBadNumber = D2DERR_BAD_NUMBER,
        

    /// <summary>
    /// The display format to render is not supported by the hardware device
    /// </summary>
    Direct2DErrorDisplayFormatNotSupported =  D2DERR_DISPLAY_FORMAT_NOT_SUPPORTED, 
        

    /// <summary>
    /// A valid display state could not be determined
    /// </summary>
    Direct2DErrorDisplayStateInvalid = D2DERR_DISPLAY_STATE_INVALID, 
        

    /// <summary>
    /// The requested size is larger than the guaranteed supported texture size
    /// </summary>
    Direct2DErrorExceedsMaxBitmapSize = D2DERR_EXCEEDS_MAX_BITMAP_SIZE, 
        

    /// <summary>
    /// The brush types are incompatible for the call
    /// </summary>
    Direct2DErrorIncompatibleBrushTypes = D2DERR_INCOMPATIBLE_BRUSH_TYPES, 
        

    /// <summary>
    /// The supplied buffer is too small to accommodate the data
    /// </summary>
    Direct2DErrorInsufficientBuffer = ERROR_INSUFFICIENT_BUFFER, 
        

    /// <summary>
    /// The application should close this instance of Direct2D and restart it as a new process
    /// </summary>
    Direct2DErrorInternalError = D2DERR_INTERNAL_ERROR, 
        

    /// <summary>
    /// A call to this method is invalid
    /// </summary>
    Direct2DErrorInvalidCall = D2DERR_INVALID_CALL, 
        

    /// <summary>
    /// The application attempted to reuse a layer resource that has not yet been popped off the stack
    /// </summary>
    Direct2DErrorLayerAlreadyInUse = D2DERR_LAYER_ALREADY_IN_USE, 
        

    /// <summary>
    /// The requested DX surface size exceeds the maximum texture size
    /// </summary>
    Direct2DErrorMaxTextureSizeExceeded = D2DERR_MAX_TEXTURE_SIZE_EXCEEDED, 
        

    /// <summary>
    /// There is no hardware rendering device available for this operation
    /// </summary>
    Direct2DErrorNoHardwareDevice = D2DERR_NO_HARDWARE_DEVICE, 
        
    
    /// <summary>
    /// The object has not yet been initialized
    /// </summary>
    Direct2DErrorNotInitialized = D2DERR_NOT_INITIALIZED, 
        
    
    /// <summary>
    /// The application attempted to pop a layer off the stack when a clip was at the top, or pop a clip off the stack when a layer was at the top
    /// </summary>
    Direct2DErrorPopCallDidNotMatchPush = D2DERR_POP_CALL_DID_NOT_MATCH_PUSH, 
        
    
    /// <summary>
    /// The application did not pop all clips and layers off the stack, or it attempted to pop too many clips or layers off the stack.
    /// </summary>
    Direct2DErrorPushPopUnbalanced = D2DERR_PUSH_POP_UNBALANCED, 
        
    
    /// <summary>
    /// A presentation error has occurred that may be recoverable. The caller needs to re-create the render target, render the entire frame again, and reattempt presentation
    /// </summary>
    Direct2DErrorRecreateTarget = D2DERR_RECREATE_TARGET, 
        
    
    /// <summary>
    /// The application attempted to copy the contents of a render target before popping all layers and clips off the stack
    /// </summary>
    Direct2DErrorRenderTargetHasLayerOrClipRect = D2DERR_RENDER_TARGET_HAS_LAYER_OR_CLIPRECT, 
        
    
    /// <summary>
    /// The geomery scanner failed to process the data
    /// </summary>
    Direct2DErrorScannerFailed = D2DERR_SCANNER_FAILED, 
        
    
    /// <summary>
    /// Direct2D could not access the screen
    /// </summary>
    Direct2DErrorScreenAccessDenied = D2DERR_SCREEN_ACCESS_DENIED, 
        
    
    /// <summary>
    /// Shader compilation failed
    /// </summary>
    Direct2DErrorShaderCompileFailed = D2DERR_SHADER_COMPILE_FAILED, 
        
    
    /// <summary>
    /// The render target is not compatible with GDI
    /// </summary>
    Direct2DErrorTargetNotGdiCompatible = D2DERR_TARGET_NOT_GDI_COMPATIBLE, 
        
    
    /// <summary>
    /// A text client drawing effect object is of the wrong type
    /// </summary>
    Direct2DErrorTextEffectIsWrongType = D2DERR_TEXT_EFFECT_IS_WRONG_TYPE, 
        
    
    /// <summary>
    /// An application is holding a reference to the IDWriteTextRenderer interface after the corresponding DrawText or DrawTextLayout call has returned
    /// </summary>
    Direct2DErrorTextRendererNotReleased = D2DERR_TEXT_RENDERER_NOT_RELEASED, 
        
    
    /// <summary>
    /// Shader construction failed because it was too complex
    /// </summary>
    Direct2DErrorTooManyShaderElements = D2DERR_TOO_MANY_SHADER_ELEMENTS, 
        
    
    /// <summary>
    /// The requested operation is not supported
    /// </summary>
    Direct2DErrorUnsupportedOperation = D2DERR_UNSUPPORTED_OPERATION, 
        
    
    /// <summary>
    /// The requested Direct2D version is not supported
    /// </summary>
    Direct2DErrorUnsupportedVersion = D2DERR_UNSUPPORTED_VERSION, 
        
    
    /// <summary>
    /// An unknown Win32 failure occurred
    /// </summary>
    Direct2DErrorWin32Error = D2DERR_WIN32_ERROR, 
        
    
    /// <summary>
    /// Objects used together were not all created from the same factory instance
    /// </summary>
    Direct2DErrorWrongFactory = D2DERR_WRONG_FACTORY, 
        
    
    /// <summary>
    /// The resource used was created by a render target in a different resource domain
    /// </summary>
    Direct2DErrorWrongResourceDomain = D2DERR_WRONG_RESOURCE_DOMAIN, 
        
    
    /// <summary>
    /// The object was not in the correct state to process the method
    /// </summary>
    Direct2DErrorWrongState = D2DERR_WRONG_STATE, 
        
    
    /// <summary>
    /// The supplied vector is zero
    /// </summary>
    Direct2DErrorZeroVector = D2DERR_ZERO_VECTOR, 
        
    /// <summary>
    /// Already locked.
    /// </summary>
    ImagingErrorAlreadyLocked =  WINCODEC_ERR_ALREADYLOCKED, 
    
    
    /// <summary>
    /// Bad header.
    /// </summary>
    ImagingErrorBadHeader =  WINCODEC_ERR_BADHEADER,
    
    
    /// <summary>
    /// Bad image.
    /// </summary>
    ImagingErrorBadImage =  WINCODEC_ERR_BADIMAGE,
    
    
    /// <summary>
    /// Bad meta data header.
    /// </summary>
    ImagingErrorBadMetadataHeader =  WINCODEC_ERR_BADMETADATAHEADER, 
    
    
    /// <summary>
    /// Bad stream data.
    /// </summary>
    ImagingErrorBadStreamData =  WINCODEC_ERR_BADSTREAMDATA,
    
    
    /// <summary>
    /// Codec no Thumbnail.
    /// </summary>
    ImagingErrorCodecNoThumbnail =  WINCODEC_ERR_CODECNOTHUMBNAIL,
    
    
    /// <summary>
    /// Codec present.
    /// </summary>
    ImagingErrorCodecPresent =  WINCODEC_ERR_CODECPRESENT, 
    
    
    /// <summary>
    /// codectoomanyscanlines.
    /// </summary>
    ImagingErrorCodecTooManyScanLines =  WINCODEC_ERR_CODECTOOMANYSCANLINES, 
    
    
    /// <summary>
    /// Component Initialize Failure.
    /// </summary>
    ImagingErrorComponentInitializeFailure =  WINCODEC_ERR_COMPONENTINITIALIZEFAILURE, 
    
    
    /// <summary>
    /// ComponentNotFound.
    /// </summary>
    ImagingErrorComponentNotFound =  WINCODEC_ERR_COMPONENTNOTFOUND, 
    
    
    /// <summary>
    /// Duplicate meta data present.
    /// </summary>
    ImagingErrorDuplicateMetadataPresent =  WINCODEC_ERR_DUPLICATEMETADATAPRESENT,
    
    
    /// <summary>
    /// Frame missing.
    /// </summary>
    ImagingErrorFrameMissing =  WINCODEC_ERR_FRAMEMISSING,
    
    
    /// <summary>
    /// Generic Error.
    /// </summary>
    ImagingErrorGeneric =  WINCODEC_ERR_GENERIC_ERROR,
    
    
    /// <summary>
    /// Image size out of range.
    /// </summary>
    ImagingErrorImageSizeOutOfRange =  WINCODEC_ERR_IMAGESIZEOUTOFRANGE,
    
    
    /// <summary>
    /// Insufficient buffer.
    /// </summary>
    ImagingErrorInsufficientBuffer =  WINCODEC_ERR_INSUFFICIENTBUFFER,
    
    
    /// <summary>
    /// Internal error.
    /// </summary>
    ImagingErrorInternal =  WINCODEC_ERR_INTERNALERROR,
    
    /// <summary>
    /// Invalid Query Character.
    /// </summary>
    ImagingErrorInvalidQueryCharacter =  WINCODEC_ERR_INVALIDQUERYCHARACTER,
    
    
    /// <summary>
    /// Invalid query request.
    /// </summary>
    ImagingErrorInvalidQueryRequest =  WINCODEC_ERR_INVALIDQUERYREQUEST,
    
    
    /// <summary>
    /// Invalid registration.
    /// </summary>
    ImagingErrorInvalidRegistration =  WINCODEC_ERR_INVALIDREGISTRATION,
    

    /// <summary>
    /// Not initialized.
    /// </summary>
    ImagingErrorNotInitialized =  WINCODEC_ERR_NOTINITIALIZED,
    
        
    /// <summary>
    /// Palette unavailable.
    /// </summary>
    ImagingErrorPaletteUnavailable =  WINCODEC_ERR_PALETTEUNAVAILABLE,
    
    
    /// <summary>
    /// Property not found.
    /// </summary>
    ImagingErrorPropertyNotFound =  WINCODEC_ERR_PROPERTYNOTFOUND,
    
    
    /// <summary>
    /// Property not supported.
    /// </summary>
    ImagingErrorPropertyNotSupported =  WINCODEC_ERR_PROPERTYNOTSUPPORTED,
    
    
    /// <summary>
    /// Property size.
    /// </summary>
    ImagingErrorPropertySize =  WINCODEC_ERR_PROPERTYSIZE, 
    
    
    /// <summary>
    /// Property unexpected type.
    /// </summary>
    ImagingErrorPropertyUnexpectedType =  WINCODEC_ERR_PROPERTYUNEXPECTEDTYPE,
    
    
    /// <summary>
    /// Request only valid at meta data root.
    /// </summary>
    ImagingErrorRequestOnlyValidAtMetadataRoot =  WINCODEC_ERR_REQUESTONLYVALIDATMETADATAROOT, 
    
    
    /// <summary>
    /// Source rectangle does not match dimensions.
    /// </summary>
    ImagingErrorSourceRectDoesNotMatchDimensions =  WINCODEC_ERR_SOURCERECTDOESNOTMATCHDIMENSIONS,
    
    
    /// <summary>
    /// Stream write.
    /// </summary>
    ImagingErrorStreamWrite =  WINCODEC_ERR_STREAMWRITE,
    
    
    /// <summary>
    /// Stream read.
    /// </summary>
    ImagingErrorStreamRead =  WINCODEC_ERR_STREAMREAD, 
    
    
    /// <summary>
    /// Stream not available.
    /// </summary>
    ImagingErrorStreamNotAvailable =  WINCODEC_ERR_STREAMNOTAVAILABLE,
    
    
    /// <summary>
    /// Too much meta data.
    /// </summary>
    ImagingErrorTooMuchMetadata =  WINCODEC_ERR_TOOMUCHMETADATA,
    
    
    /// <summary>
    /// Unknown image format.
    /// </summary>
    ImagingErrorUnknownImageFormat =  WINCODEC_ERR_UNKNOWNIMAGEFORMAT,
    
    
    /// <summary>
    /// Unexpected meta data type.
    /// </summary>
    ImagingErrorUnexpectedMetadataType =  WINCODEC_ERR_UNEXPECTEDMETADATATYPE,
    
    
    /// <summary>
    /// Unexpected size.
    /// </summary>
    ImagingErrorUnexpectedSize =  WINCODEC_ERR_UNEXPECTEDSIZE,
    
    
    /// <summary>
    /// Unsupported operation.
    /// </summary>
    ImagingErrorUnsupportedOperation =  WINCODEC_ERR_UNSUPPORTEDOPERATION,
    
    
    /// <summary>
    /// Unsupported Pixel Format.
    /// </summary>
    ImagingErrorUnsupportedPixelFormat =  WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT,
    
    
    /// <summary>
    /// Unsupported Version.
    /// </summary>
    ImagingErrorUnsupportedVersion =  WINCODEC_ERR_UNSUPPORTEDVERSION, 
    
    
    /// <summary>
    /// Value Out Of Range.
    /// </summary>
    ImagingErrorValueOutOfRange =  WINCODEC_ERR_VALUEOUTOFRANGE,
    
    /// <summary>
    /// Value Overflow.
    /// </summary>
    ImagingErrorValueOverflow =  WINCODEC_ERR_VALUEOVERFLOW,
    
    
    /// <summary>
    /// Wrong state.
    /// </summary>
    ImagingErrorWrongState =  WINCODEC_ERR_WRONGSTATE,

};

} } }
