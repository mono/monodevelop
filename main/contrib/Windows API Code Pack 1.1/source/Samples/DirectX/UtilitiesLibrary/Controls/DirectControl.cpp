// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "DirectControl.h"

using namespace  Microsoft::WindowsAPICodePack::DirectX::Controls;

#define WM_CUSTOM_DRAW WM_USER+100

DirectControl::DirectControl()
{
    SetStyle(ControlStyles::UserPaint, true);
    SetStyle(ControlStyles::AllPaintingInWmPaint, true);
    SetStyle(ControlStyles::ContainerControl, true);
    UpdateStyles();
}

void DirectControl::WndProc (Message %m)
{
    if (m.Msg == WM_DESTROY )
    {
		Render = nullptr;
    }
	else if (m.Msg == WM_PAINT || m.Msg == WM_DISPLAYCHANGE)
	{
		if (Render!= nullptr)
			Render();
        
        ::PostMessage((HWND)Handle.ToPointer(), WM_CUSTOM_DRAW, 0 , 0);
	}
    else if (m.Msg == WM_CUSTOM_DRAW)
    {   
        if (!DesignMode)
        {
            Application::DoEvents();
            Invalidate();
        }
    }

    UserControl::WndProc(m);
}

void DirectControl::OnPaintBackground(PaintEventArgs ^e)
{
    if (DesignMode)
    {
        e->Graphics->FillRectangle(
            SystemBrushes::Control, 
            System::Drawing::Rectangle(0,0,Width, Height));
    }
}

void DirectControl::OnPaint(PaintEventArgs ^e)
{
    if (DesignMode)
    {               
        e->Graphics->DrawString(
            "Direct Control", 
            SystemFonts::DefaultFont,
            SystemBrushes::ControlText, 
            (float)Width / 2, 
            (float)Height / 2);
    }
}