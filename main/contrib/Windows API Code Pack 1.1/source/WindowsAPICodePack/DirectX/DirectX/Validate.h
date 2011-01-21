// Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

namespace  Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Utilities {

#ifdef SYSTEM_CORE_DLL_WORKAROUND
generic<typename T1, typename T2, typename TResult>
private delegate TResult Func(T1 arg1, T2 arg2);
#endif // SYSTEM_CORE_DLL_WORKAROUND

private ref class Validate
{
private:

    // Prohibit instantiation of static class
    Validate() { }

	static Exception^ GetWinErrorException(HRESULT hr);
	static Exception^ GetDirect3D10Exception(HRESULT hr);
	static Exception^ GetDirect3D11Exception(HRESULT hr);

	static Exception^ GetDirectXGraphicsException(HRESULT hr);
	static Exception^ DirectXGraphicsExceptionFactory(HRESULT hr, String^ text);

	static Exception^ GetDirect2DException(HRESULT hr);
	static Exception^ Direct2DExceptionFactory(HRESULT hr, String^ text);

	static Exception^ GetWindowsImagingException(HRESULT hr);
	static Exception^ WindowsImagingExceptionFactory(HRESULT hr, String^ text);

	static void EnsureGraphicsExceptionDescriptions(void);
	static void EnsureDirect2DExceptionDescriptions(void);
	static void EnsureWindowsImagingExceptionDescriptions(void);

	enum class ExceptionType
	{
		Graphics,
		Direct2D,
		Imaging,
		NotSupported,
		ArgumentOutOfRange,
		InsufficientMemory
	};

	value class ExceptionDescription
	{
	public:

		property String^ Text
		{
			String^ get(void) { return text; }
		}

		property ExceptionType ExceptionType
		{
			Validate::ExceptionType get(void) { return exceptionType; }
		}

		ExceptionDescription(String^ text, Validate::ExceptionType exceptionType)
			: text(text), exceptionType(exceptionType)
		{ }

	private:

		String^ text;
		Validate::ExceptionType exceptionType;
	};

	static Exception^ GetException(Dictionary<HRESULT, ExceptionDescription>^ descriptions,
		ExceptionType defaultType, Func<HRESULT, String^, Exception^>^ defaultFactory, HRESULT hr);

	static Dictionary<HRESULT, ExceptionDescription>^ graphicsExceptionDescriptions;
	static Dictionary<HRESULT, ExceptionDescription>^ direct2DExceptionDescriptions;
	static Dictionary<HRESULT, ExceptionDescription>^ windowsImagingExceptionDescriptions;

internal:

    // Argument validation
    static void CheckNull(Object^ o, String^ argName);

    // Error reporting
    static Exception^ GetExceptionForHR(HRESULT hr);
    static void VerifyResult(HRESULT hr);
	static void VerifyBoolean(bool success);
	static void VerifyBoolean(BOOL success) { VerifyBoolean(success != FALSE); }

};

} } } }