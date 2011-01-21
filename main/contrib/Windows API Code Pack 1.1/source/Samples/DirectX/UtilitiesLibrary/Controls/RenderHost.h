//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

using namespace System;
using namespace System::Windows;
using namespace System::Windows::Media;
using namespace System::Windows::Interop;
using namespace System::Runtime::InteropServices;

namespace  Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Controls {

public delegate void RenderHandler();

public ref class RenderHost : public HwndHost
{
private:
	HWND m_hWnd;
	HINSTANCE m_hInstance;
	LPCWSTR m_sWindowName;
	LPCWSTR m_sClassName;
	bool RegisterWindowClass();

public:
    RenderHost() : 
				m_hWnd(NULL),
				m_hInstance(NULL),
				m_sWindowName(NULL),
				m_sClassName(NULL)
		{
		}

protected:
	virtual HandleRef BuildWindowCore(HandleRef hwndParent) override;
   
    virtual void DestroyWindowCore(HandleRef hwnd) override;

    virtual void OnRender(DrawingContext ^ ctx) override;

    virtual IntPtr WndProc( IntPtr hwnd,  int msg,  IntPtr wParam,  IntPtr lParam, bool% handled) override;

public: 
    RenderHandler^ Render;
};
} } } }