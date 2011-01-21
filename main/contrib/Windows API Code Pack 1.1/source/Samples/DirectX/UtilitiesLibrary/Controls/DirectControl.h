//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once

using namespace System;
using namespace System::Windows::Forms;
using namespace System::Drawing;
using namespace System::Threading;

namespace  Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Controls {

public delegate void RenderHandler();

public ref class DirectControl : public System::Windows::Forms::UserControl
{
public:
    /// <summary>
    /// Default Constructor
    /// </summary>
    DirectControl();

protected:
	virtual void WndProc (Message %m) override;

	virtual void OnPaintBackground(PaintEventArgs ^e) override;

    virtual void OnPaint(PaintEventArgs ^e) override;

public: 
    RenderHandler^ Render;
};
} } } }
