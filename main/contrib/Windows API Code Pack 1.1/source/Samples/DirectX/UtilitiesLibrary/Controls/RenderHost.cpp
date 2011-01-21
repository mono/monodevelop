// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "RenderHost.h"

using namespace System;
using namespace System::Windows;
using namespace System::Windows::Interop;
using namespace System::Threading;
using namespace System::Runtime::InteropServices;
using namespace Microsoft::WindowsAPICodePack::DirectX::Controls;

bool RenderHost::RegisterWindowClass()
{
	WNDCLASS wndClass;

	if(GetClassInfo(m_hInstance, m_sClassName, &wndClass))
	{
		return true;
	}

	wndClass.style				= CS_HREDRAW | CS_VREDRAW;
	
	wndClass.lpfnWndProc		= DefWindowProc; 
	wndClass.cbClsExtra			= 0;
	wndClass.cbWndExtra			= 0;
	wndClass.hInstance			= m_hInstance;
	wndClass.hIcon				= LoadIcon(NULL, IDI_WINLOGO);
	wndClass.hCursor			= LoadCursor(0, IDC_ARROW);
	wndClass.hbrBackground		= 0;
	wndClass.lpszMenuName		= NULL; // No menu
	wndClass.lpszClassName		= m_sClassName;

	if (!RegisterClass(&wndClass))
	{
		return false;
	}

	return true;
}
HandleRef RenderHost::BuildWindowCore(HandleRef hwndParent) 
{
	m_hInstance		= (HINSTANCE) GetModuleHandle(NULL);
	m_sWindowName	= L"RenderHost";
	m_sClassName	= L"RenderHost";

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

void RenderHost::DestroyWindowCore(HandleRef hwnd)
{
	if(NULL != m_hWnd && m_hWnd == (HWND)hwnd.Handle.ToPointer())
	{
		::DestroyWindow(m_hWnd);
		m_hWnd = NULL;
	}

	UnregisterClass(m_sClassName, m_hInstance);
}

void RenderHost::OnRender(DrawingContext ^ ctx)
{
	if (Render!= nullptr)
		Render();
}

IntPtr RenderHost::WndProc( IntPtr hwnd,  int msg,  IntPtr wParam,  IntPtr lParam, bool% handled)
{
    if (msg == WM_SIZE )
    {
        InvalidateVisual();
    }
    else if (msg == WM_PAINT)
    {
        InvalidateVisual();
    }


    handled = false;
    return IntPtr::Zero;
}
