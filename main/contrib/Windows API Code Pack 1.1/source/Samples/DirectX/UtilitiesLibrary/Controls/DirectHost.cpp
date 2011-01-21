// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "DirectHost.h"

using namespace System;
using namespace System::Windows;
using namespace System::Windows::Interop;
using namespace System::Threading;
using namespace System::Runtime::InteropServices;
using namespace Microsoft::WindowsAPICodePack::DirectX::Controls;

bool DirectHost::RegisterWindowClass()
{
	WNDCLASS wndClass;

	if(GetClassInfo(m_hInstance, m_sClassName, &wndClass))
	{
		return true;
	}

	wndClass.style				= CS_HREDRAW | CS_VREDRAW | CS_OWNDC;
	wndClass.lpfnWndProc		= DefWindowProc; 
	wndClass.cbClsExtra			= 0;
	wndClass.cbWndExtra			= 0;
	wndClass.hInstance			= m_hInstance;
	wndClass.hIcon				= LoadIcon(NULL, IDI_WINLOGO);
	wndClass.hCursor			= LoadCursor(0, IDC_ARROW);
	wndClass.hbrBackground		= 0;
	wndClass.lpszMenuName		= NULL;
	wndClass.lpszClassName		= m_sClassName;

	return (RegisterClass(&wndClass) != 0);
}

HandleRef DirectHost::BuildWindowCore(HandleRef hwndParent) 
{
	m_hInstance		= (HINSTANCE) GetModuleHandle(NULL);
	m_sWindowName	= L"DirectHost";
	m_sClassName	= L"DirectHost";

	if(RegisterWindowClass())
	{
		HWND parentHwnd = (HWND)hwndParent.Handle.ToPointer();
		m_hWnd = CreateWindowEx(0,
					m_sClassName,
					m_sWindowName,
					WS_CHILD | WS_VISIBLE,
					0,
					0,
					10, // These are arbitary values,
					10, // real sizes will be defined by the parent
					parentHwnd,
					NULL,
					m_hInstance,
					NULL );

		if(!m_hWnd)
		{
			return HandleRef(nullptr, System::IntPtr::Zero);
		}
		
        return HandleRef(this, IntPtr(m_hWnd));
    }

	return HandleRef(nullptr, System::IntPtr::Zero);
}

void DirectHost::DestroyWindowCore(HandleRef hwnd)
{
	if(NULL != m_hWnd && m_hWnd == (HWND)hwnd.Handle.ToPointer())
	{
		::DestroyWindow(m_hWnd);
		m_hWnd = NULL;
	}

	UnregisterClass(m_sClassName, m_hInstance);
}

IntPtr DirectHost::WndProc( IntPtr hwnd,  int message,  IntPtr wParam,  IntPtr lParam, bool% handled)
{
    switch (message)
    {
    case WM_PAINT:
    case WM_DISPLAYCHANGE:
        {
			if (Render != nullptr)
				Render();			
        }
		handled = true;
        return IntPtr::Zero;

	case WM_DESTROY:
        {
            Render = nullptr;
			break;
        }
	}

	handled = false;
	return IntPtr::Zero;
}
