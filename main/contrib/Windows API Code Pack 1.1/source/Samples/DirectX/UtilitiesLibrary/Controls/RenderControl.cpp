// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "StdAfx.h"
#include "RenderControl.h"

using namespace  Microsoft::WindowsAPICodePack::DirectX::Controls;

RenderControl::RenderControl()
{
    SetStyle(ControlStyles::UserPaint, true);
    SetStyle(ControlStyles::AllPaintingInWmPaint, true);            
    UpdateStyles();
}

void RenderControl::OnPaintBackground(PaintEventArgs ^e)
{
    if (DesignMode)
    {
        e->Graphics->FillRectangle(
            SystemBrushes::Control, 
            System::Drawing::Rectangle(0,0,Width, Height));
    }
}

void RenderControl::OnPaint(PaintEventArgs ^e)
{
    if (DesignMode)
    {               
        e->Graphics->DrawString(
            "Render Control", 
            SystemFonts::DefaultFont,
            SystemBrushes::ControlText, 
            (float)Width / 2, 
            (float)Height / 2);
    }
    else
    {
        if (Render != nullptr)
            Render();
    }
}
