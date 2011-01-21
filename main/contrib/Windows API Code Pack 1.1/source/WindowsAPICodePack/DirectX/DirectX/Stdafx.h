// Copyright (c) Microsoft Corporation.  All rights reserved.

// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once

// Exclude rarely used parts of the windows headers
#define WIN32_LEAN_AND_MEAN

#if _MSC_VER < 1600
#define SYSTEM_CORE_DLL_WORKAROUND
#endif

//Define Unicode
#ifndef UNICODE
#define UNICODE
#endif

// smart pointer definitions for use in Texture utilities
#include <comdef.h>
#undef INTSAFE_E_ARITHMETIC_OVERFLOW

// Common Public Headers
#include <vcclr.h>
#include <Windows.h>
#include <msclr\marshal.h>

// Common DirectX Headers
#include <D3D10_1.h>
#include <D3D10.h>
#include <D3D11.h>
#include <DXGI.h>
#include <DXGIType.h>
#include <wincodec.h>
#include <d2d1.h>
#include <dwrite.h>
#include <D3DCommon.h>

// Common Includes
#include "AutoPointer.h"
#include "AutoIUnknown.h"
#include "Direct3DCommon/Direct3DException.h"
#include "DXGI/DXGIException.h"
#include "Direct2D/D2DException.h"
#include "WIC/WICException.h"
#include "CommonUtils.h"
#include "Convert.h"
#include "Validate.h"

// Common Types
#include "CommonEnums.h"
#include "Direct3DCommon/D3DCommonEnums.h"
#include "Direct3DCommon/D3DCommonStructs.h"
#include "DXGI/DXGIEnums.h"
#include "DXGI/DXGIStructs.h"
#include "Direct3D11/D3D11Enums.h"
#include "Direct3D11/D3D11Structs.h"
#include "Direct3D10/D3D10Enums.h"
#include "Direct3D10/D3D10Structs.h"

// Direct 2D
#include "Direct2D/D2Dcommon.h"
