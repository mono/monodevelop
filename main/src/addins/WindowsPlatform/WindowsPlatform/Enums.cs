//  Copyright (c) 2006, Gustavo Franco
//  Email:  gustavo_franco@hotmail.com
//  All rights reserved.

//  Redistribution and use in source and binary forms, with or without modification, 
//  are permitted provided that the following conditions are met:

//  Redistributions of source code must retain the above copyright notice, 
//  this list of conditions and the following disclaimer. 
//  Redistributions in binary form must reproduce the above copyright notice, 
//  this list of conditions and the following disclaimer in the documentation 
//  and/or other materials provided with the distribution. 

//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CustomControls.OS
{
    #region SWP_Flags
    [Flags]
    public enum SWP_Flags
    {
        SWP_NOSIZE			= 0x0001,
        SWP_NOMOVE			= 0x0002,
        SWP_NOZORDER		= 0x0004,
        SWP_NOACTIVATE		= 0x0010,
        SWP_FRAMECHANGED	= 0x0020, /* The frame changed: send WM_NCCALCSIZE */
        SWP_SHOWWINDOW		= 0x0040,
        SWP_HIDEWINDOW		= 0x0080,
        SWP_NOOWNERZORDER   = 0x0200, /* Don't do owner Z ordering */

        SWP_DRAWFRAME       = SWP_FRAMECHANGED,
        SWP_NOREPOSITION    = SWP_NOOWNERZORDER
    }
    #endregion

    #region DialogChangeStatus
    public enum DialogChangeStatus : long
    {
        CDN_FIRST           = 0xFFFFFDA7,
        CDN_INITDONE        = (CDN_FIRST - 0x0000),
        CDN_SELCHANGE       = (CDN_FIRST - 0x0001),
        CDN_FOLDERCHANGE    = (CDN_FIRST - 0x0002),
        CDN_SHAREVIOLATION  = (CDN_FIRST - 0x0003),
        CDN_HELP            = (CDN_FIRST - 0x0004),
        CDN_FILEOK          = (CDN_FIRST - 0x0005),
        CDN_TYPECHANGE      = (CDN_FIRST - 0x0006),
    }
    #endregion

    #region DialogChangeProperties
    public enum DialogChangeProperties
    {
        CDM_FIRST              = (0x400 + 100),
        CDM_GETSPEC            = (CDM_FIRST + 0x0000),
        CDM_GETFILEPATH        = (CDM_FIRST + 0x0001),
        CDM_GETFOLDERPATH      = (CDM_FIRST + 0x0002),
        CDM_GETFOLDERIDLIST    = (CDM_FIRST + 0x0003),
        CDM_SETCONTROLTEXT     = (CDM_FIRST + 0x0004),
        CDM_HIDECONTROL        = (CDM_FIRST + 0x0005),
        CDM_SETDEFEXT          = (CDM_FIRST + 0x0006)
    }
    #endregion

    #region ImeNotify
    [Author("Franco, Gustavo")]
    public enum ImeNotify
    {
        IMN_CLOSESTATUSWINDOW         = 0x0001,
        IMN_OPENSTATUSWINDOW          = 0x0002,
        IMN_CHANGECANDIDATE           = 0x0003,
        IMN_CLOSECANDIDATE            = 0x0004,
        IMN_OPENCANDIDATE             = 0x0005,
        IMN_SETCONVERSIONMODE         = 0x0006,
        IMN_SETSENTENCEMODE           = 0x0007,
        IMN_SETOPENSTATUS             = 0x0008,
        IMN_SETCANDIDATEPOS           = 0x0009,
        IMN_SETCOMPOSITIONFONT        = 0x000A,
        IMN_SETCOMPOSITIONWINDOW      = 0x000B,
        IMN_SETSTATUSWINDOWPOS        = 0x000C,
        IMN_GUIDELINE                 = 0x000D,
        IMN_PRIVATE                   = 0x000E
    }
    #endregion

    #region FolderViewMode
    [Author("Franco, Gustavo")]
    public enum FolderViewMode
    {
        Default     = 0x7028,
	    Icon	    = Default + 1,
	    SmallIcon	= Default + 2,
	    List	    = Default + 3,
	    Details	    = Default + 4,
	    Thumbnails	= Default + 5,
	    Title	    = Default + 6,
	    Thumbstrip  = Default + 7,
    }
    #endregion

    #region Enum DialogViewProperty
    [Author("Franco, Gustavo")]
    public enum DefaultViewType
	{
		Icons       = 0x7029,
		List        = 0x702b,
		Details     = 0x702c,
		Thumbnails  = 0x702d,
		Tiles       = 0x702e,
	}
	#endregion

    #region ButtonStyle
    [Author("Franco, Gustavo")]
    public enum ButtonStyle : long
    {
        BS_PUSHBUTTON     = 0x00000000,
        BS_DEFPUSHBUTTON  = 0x00000001,
        BS_CHECKBOX       = 0x00000002,
        BS_AUTOCHECKBOX   = 0x00000003,
        BS_RADIOBUTTON    = 0x00000004,
        BS_3STATE         = 0x00000005,
        BS_AUTO3STATE     = 0x00000006,
        BS_GROUPBOX       = 0x00000007,
        BS_USERBUTTON     = 0x00000008,
        BS_AUTORADIOBUTTON= 0x00000009,
        BS_PUSHBOX        = 0x0000000A,
        BS_OWNERDRAW      = 0x0000000B,
        BS_TYPEMASK       = 0x0000000F,
        BS_LEFTTEXT       = 0x00000020,
        BS_TEXT           = 0x00000000,
        BS_ICON           = 0x00000040,
        BS_BITMAP         = 0x00000080,
        BS_LEFT           = 0x00000100,
        BS_RIGHT          = 0x00000200,
        BS_CENTER         = 0x00000300,
        BS_TOP            = 0x00000400,
        BS_BOTTOM         = 0x00000800,
        BS_VCENTER        = 0x00000C00,
        BS_PUSHLIKE       = 0x00001000,
        BS_MULTILINE      = 0x00002000,
        BS_NOTIFY         = 0x00004000,
        BS_FLAT           = 0x00008000,
        BS_RIGHTBUTTON    = BS_LEFTTEXT
    }
    #endregion

    #region ZOrderPos
    [Author("Franco, Gustavo")]
    public enum ZOrderPos
    {
        HWND_TOP        = 0,
        HWND_BOTTOM     = 1,
        HWND_TOPMOST    = -1,
        HWND_NOTOPMOST  = -2
    }
    #endregion

    #region Static Control Styles
    [Author("Franco, Gustavo")]
    public enum StaticControlStyles : long
    {
        SS_LEFT             = 0x00000000,
        SS_CENTER           = 0x00000001,
        SS_RIGHT            = 0x00000002,
        SS_ICON             = 0x00000003,
        SS_BLACKRECT        = 0x00000004,
        SS_GRAYRECT         = 0x00000005,
        SS_WHITERECT        = 0x00000006,
        SS_BLACKFRAME       = 0x00000007,
        SS_GRAYFRAME        = 0x00000008,
        SS_WHITEFRAME       = 0x00000009,
        SS_USERITEM         = 0x0000000A,
        SS_SIMPLE           = 0x0000000B,
        SS_LEFTNOWORDWRAP   = 0x0000000C,
        SS_OWNERDRAW        = 0x0000000D,
        SS_BITMAP           = 0x0000000E,
        SS_ENHMETAFILE      = 0x0000000F,
        SS_ETCHEDHORZ       = 0x00000010,
        SS_ETCHEDVERT       = 0x00000011,
        SS_ETCHEDFRAME      = 0x00000012,
        SS_TYPEMASK         = 0x0000001F,
        SS_REALSIZECONTROL  = 0x00000040,
        SS_NOPREFIX         = 0x00000080, /* Don't do "&" character translation */
        SS_NOTIFY           = 0x00000100,
        SS_CENTERIMAGE      = 0x00000200,
        SS_RIGHTJUST        = 0x00000400,
        SS_REALSIZEIMAGE    = 0x00000800,
        SS_SUNKEN           = 0x00001000,
        SS_EDITCONTROL      = 0x00002000,
        SS_ENDELLIPSIS      = 0x00004000,
        SS_PATHELLIPSIS     = 0x00008000,
        SS_WORDELLIPSIS     = 0x0000C000,
        SS_ELLIPSISMASK     = 0x0000C000
    }
    #endregion

	#region Combo Box styles
    [Author("Franco, Gustavo")]
    public enum ComboBoxStyles : long
    {
        CBS_SIMPLE            = 0x0001,
        CBS_DROPDOWN          = 0x0002,
        CBS_DROPDOWNLIST      = 0x0003,
        CBS_OWNERDRAWFIXED    = 0x0010,
        CBS_OWNERDRAWVARIABLE = 0x0020,
        CBS_AUTOHSCROLL       = 0x0040,
        CBS_OEMCONVERT        = 0x0080,
        CBS_SORT              = 0x0100,
        CBS_HASSTRINGS        = 0x0200,
        CBS_NOINTEGRALHEIGHT  = 0x0400,
        CBS_DISABLENOSCROLL   = 0x0800,
        CBS_UPPERCASE         = 0x2000,
        CBS_LOWERCASE         = 0x4000
    }
    #endregion

    #region Window Styles
    [Author("Franco, Gustavo")]
    public enum WindowStyles : long
	{
		WS_OVERLAPPED       = 0x00000000,
		WS_POPUP            = 0x80000000,
		WS_CHILD            = 0x40000000,
		WS_MINIMIZE         = 0x20000000,
		WS_VISIBLE          = 0x10000000,
		WS_DISABLED         = 0x08000000,
		WS_CLIPSIBLINGS     = 0x04000000,
		WS_CLIPCHILDREN     = 0x02000000,
		WS_MAXIMIZE         = 0x01000000,
		WS_CAPTION          = 0x00C00000,
		WS_BORDER           = 0x00800000,
		WS_DLGFRAME         = 0x00400000,
		WS_VSCROLL          = 0x00200000,
		WS_HSCROLL          = 0x00100000,
		WS_SYSMENU          = 0x00080000,
		WS_THICKFRAME       = 0x00040000,
		WS_GROUP            = 0x00020000,
		WS_TABSTOP          = 0x00010000,
		WS_MINIMIZEBOX      = 0x00020000,
		WS_MAXIMIZEBOX      = 0x00010000,
		WS_TILED            = 0x00000000,
		WS_ICONIC           = 0x20000000,
		WS_SIZEBOX          = 0x00040000,
		WS_POPUPWINDOW      = 0x80880000,
		WS_OVERLAPPEDWINDOW = 0x00CF0000,
		WS_TILEDWINDOW      = 0x00CF0000,
		WS_CHILDWINDOW      = 0x40000000
	}
	#endregion

	#region Window Extended Styles
    [Author("Franco, Gustavo")]
	[Flags]
	public enum WindowExStyles
	{
		WS_EX_DLGMODALFRAME     = 0x00000001,
		WS_EX_NOPARENTNOTIFY    = 0x00000004,
		WS_EX_TOPMOST           = 0x00000008,
		WS_EX_ACCEPTFILES       = 0x00000010,
		WS_EX_TRANSPARENT       = 0x00000020,
		WS_EX_MDICHILD          = 0x00000040,
		WS_EX_TOOLWINDOW        = 0x00000080,
		WS_EX_WINDOWEDGE        = 0x00000100,
		WS_EX_CLIENTEDGE        = 0x00000200,
		WS_EX_CONTEXTHELP       = 0x00000400,
		WS_EX_RIGHT             = 0x00001000,
		WS_EX_LEFT              = 0x00000000,
		WS_EX_RTLREADING        = 0x00002000,
		WS_EX_LTRREADING        = 0x00000000,
		WS_EX_LEFTSCROLLBAR     = 0x00004000,
		WS_EX_RIGHTSCROLLBAR    = 0x00000000,
		WS_EX_CONTROLPARENT     = 0x00010000,
		WS_EX_STATICEDGE        = 0x00020000,
		WS_EX_APPWINDOW         = 0x00040000,
		WS_EX_OVERLAPPEDWINDOW  = 0x00000300,
		WS_EX_PALETTEWINDOW     = 0x00000188,
		WS_EX_LAYERED			= 0x00080000
	}
	#endregion

	#region ChildFromPointFlags
    [Author("Franco, Gustavo")]
	public enum ChildFromPointFlags
	{
		CWP_ALL             = 0x0000,
		CWP_SKIPINVISIBLE   = 0x0001,
		CWP_SKIPDISABLED    = 0x0002,
		CWP_SKIPTRANSPARENT = 0x0004
	}
	#endregion

	#region HitTest 
    [Author("Franco, Gustavo")]
	public enum HitTest
	{
		HTERROR             = (-2),
		HTTRANSPARENT       = (-1),
		HTNOWHERE           =   0,
		HTCLIENT            =   1,
		HTCAPTION           =   2,
		HTSYSMENU           =   3,
		HTGROWBOX           =   4,
		HTSIZE              =   HTGROWBOX,
		HTMENU              =   5,
		HTHSCROLL           =   6,
		HTVSCROLL           =   7,
		HTMINBUTTON         =   8,
		HTMAXBUTTON         =   9,
		HTLEFT              =   10,
		HTRIGHT             =   11,
		HTTOP               =   12,
		HTTOPLEFT           =   13,
		HTTOPRIGHT          =   14,
		HTBOTTOM            =   15,
		HTBOTTOMLEFT        =   16,
		HTBOTTOMRIGHT       =   17,
		HTBORDER            =   18,
		HTREDUCE            =   HTMINBUTTON,
		HTZOOM              =   HTMAXBUTTON,
		HTSIZEFIRST         =   HTLEFT,
		HTSIZELAST          =   HTBOTTOMRIGHT,
		HTOBJECT            =   19,
		HTCLOSE             =   20,
		HTHELP              =   21
	}
	#endregion

	#region Windows Messages
    [Author("Franco, Gustavo")]
	public enum Msg
	{
		WM_NULL                   = 0x0000,
		WM_CREATE                 = 0x0001,
		WM_DESTROY                = 0x0002,
		WM_MOVE                   = 0x0003,
		WM_SIZE                   = 0x0005,
		WM_ACTIVATE               = 0x0006,
		WM_SETFOCUS               = 0x0007,
		WM_KILLFOCUS              = 0x0008,
		WM_ENABLE                 = 0x000A,
		WM_SETREDRAW              = 0x000B,
		WM_SETTEXT                = 0x000C,
		WM_GETTEXT                = 0x000D,
		WM_GETTEXTLENGTH          = 0x000E,
		WM_PAINT                  = 0x000F,
		WM_CLOSE                  = 0x0010,
		WM_QUERYENDSESSION        = 0x0011,
		WM_QUIT                   = 0x0012,
		WM_QUERYOPEN              = 0x0013,
		WM_ERASEBKGND             = 0x0014,
		WM_SYSCOLORCHANGE         = 0x0015,
		WM_ENDSESSION             = 0x0016,
		WM_SHOWWINDOW             = 0x0018,
		WM_CTLCOLOR               = 0x0019,
		WM_WININICHANGE           = 0x001A,
		WM_SETTINGCHANGE          = 0x001A,
		WM_DEVMODECHANGE          = 0x001B,
		WM_ACTIVATEAPP            = 0x001C,
		WM_FONTCHANGE             = 0x001D,
		WM_TIMECHANGE             = 0x001E,
		WM_CANCELMODE             = 0x001F,
		WM_SETCURSOR              = 0x0020,
		WM_MOUSEACTIVATE          = 0x0021,
		WM_CHILDACTIVATE          = 0x0022,
		WM_QUEUESYNC              = 0x0023,
		WM_GETMINMAXINFO          = 0x0024,
		WM_PAINTICON              = 0x0026,
		WM_ICONERASEBKGND         = 0x0027,
		WM_NEXTDLGCTL             = 0x0028,
		WM_SPOOLERSTATUS          = 0x002A,
		WM_DRAWITEM               = 0x002B,
		WM_MEASUREITEM            = 0x002C,
		WM_DELETEITEM             = 0x002D,
		WM_VKEYTOITEM             = 0x002E,
		WM_CHARTOITEM             = 0x002F,
		WM_SETFONT                = 0x0030,
		WM_GETFONT                = 0x0031,
		WM_SETHOTKEY              = 0x0032,
		WM_GETHOTKEY              = 0x0033,
		WM_QUERYDRAGICON          = 0x0037,
		WM_COMPAREITEM            = 0x0039,
		WM_GETOBJECT              = 0x003D,
		WM_COMPACTING             = 0x0041,
		WM_COMMNOTIFY             = 0x0044 ,
		WM_WINDOWPOSCHANGING      = 0x0046,
		WM_WINDOWPOSCHANGED       = 0x0047,
		WM_POWER                  = 0x0048,
		WM_COPYDATA               = 0x004A,
		WM_CANCELJOURNAL          = 0x004B,
		WM_NOTIFY                 = 0x004E,
		WM_INPUTLANGCHANGEREQUEST = 0x0050,
		WM_INPUTLANGCHANGE        = 0x0051,
		WM_TCARD                  = 0x0052,
		WM_HELP                   = 0x0053,
		WM_USERCHANGED            = 0x0054,
		WM_NOTIFYFORMAT           = 0x0055,
		WM_CONTEXTMENU            = 0x007B,
		WM_STYLECHANGING          = 0x007C,
		WM_STYLECHANGED           = 0x007D,
		WM_DISPLAYCHANGE          = 0x007E,
		WM_GETICON                = 0x007F,
		WM_SETICON                = 0x0080,
		WM_NCCREATE               = 0x0081,
		WM_NCDESTROY              = 0x0082,
		WM_NCCALCSIZE             = 0x0083,
		WM_NCHITTEST              = 0x0084,
		WM_NCPAINT                = 0x0085,
		WM_NCACTIVATE             = 0x0086,
		WM_GETDLGCODE             = 0x0087,
		WM_SYNCPAINT              = 0x0088,
		WM_NCMOUSEMOVE            = 0x00A0,
		WM_NCLBUTTONDOWN          = 0x00A1,
		WM_NCLBUTTONUP            = 0x00A2,
		WM_NCLBUTTONDBLCLK        = 0x00A3,
		WM_NCRBUTTONDOWN          = 0x00A4,
		WM_NCRBUTTONUP            = 0x00A5,
		WM_NCRBUTTONDBLCLK        = 0x00A6,
		WM_NCMBUTTONDOWN          = 0x00A7,
		WM_NCMBUTTONUP            = 0x00A8,
		WM_NCMBUTTONDBLCLK        = 0x00A9,
		WM_NCXBUTTONDOWN		  = 0x00AB,
		WM_NCXBUTTONUP			  = 0x00AC,
		WM_NCXBUTTONDBLCLK		  = 0x00AD,
		WM_KEYDOWN                = 0x0100,
		WM_KEYUP                  = 0x0101,
		WM_CHAR                   = 0x0102,
		WM_DEADCHAR               = 0x0103,
		WM_SYSKEYDOWN             = 0x0104,
		WM_SYSKEYUP               = 0x0105,
		WM_SYSCHAR                = 0x0106,
		WM_SYSDEADCHAR            = 0x0107,
		WM_KEYLAST                = 0x0108,
		WM_IME_STARTCOMPOSITION   = 0x010D,
		WM_IME_ENDCOMPOSITION     = 0x010E,
		WM_IME_COMPOSITION        = 0x010F,
		WM_IME_KEYLAST            = 0x010F,
		WM_INITDIALOG             = 0x0110,
		WM_COMMAND                = 0x0111,
		WM_SYSCOMMAND             = 0x0112,
		WM_TIMER                  = 0x0113,
		WM_HSCROLL                = 0x0114,
		WM_VSCROLL                = 0x0115,
		WM_INITMENU               = 0x0116,
		WM_INITMENUPOPUP          = 0x0117,
		WM_MENUSELECT             = 0x011F,
		WM_MENUCHAR               = 0x0120,
		WM_ENTERIDLE              = 0x0121,
		WM_MENURBUTTONUP          = 0x0122,
		WM_MENUDRAG               = 0x0123,
		WM_MENUGETOBJECT          = 0x0124,
		WM_UNINITMENUPOPUP        = 0x0125,
		WM_MENUCOMMAND            = 0x0126,
		WM_CTLCOLORMSGBOX         = 0x0132,
		WM_CTLCOLOREDIT           = 0x0133,
		WM_CTLCOLORLISTBOX        = 0x0134,
		WM_CTLCOLORBTN            = 0x0135,
		WM_CTLCOLORDLG            = 0x0136,
		WM_CTLCOLORSCROLLBAR      = 0x0137,
		WM_CTLCOLORSTATIC         = 0x0138,
		WM_MOUSEMOVE              = 0x0200,
		WM_LBUTTONDOWN            = 0x0201,
		WM_LBUTTONUP              = 0x0202,
		WM_LBUTTONDBLCLK          = 0x0203,
		WM_RBUTTONDOWN            = 0x0204,
		WM_RBUTTONUP              = 0x0205,
		WM_RBUTTONDBLCLK          = 0x0206,
		WM_MBUTTONDOWN            = 0x0207,
		WM_MBUTTONUP              = 0x0208,
		WM_MBUTTONDBLCLK          = 0x0209,
		WM_MOUSEWHEEL             = 0x020A,
		WM_XBUTTONDOWN			  = 0x020B,
		WM_XBUTTONUP			  = 0x020C,
		WM_XBUTTONDBLCLK		  = 0x020D,
		WM_PARENTNOTIFY           = 0x0210,
		WM_ENTERMENULOOP          = 0x0211,
		WM_EXITMENULOOP           = 0x0212,
		WM_NEXTMENU               = 0x0213,
		WM_SIZING                 = 0x0214,
		WM_CAPTURECHANGED         = 0x0215,
		WM_MOVING                 = 0x0216,
		WM_DEVICECHANGE           = 0x0219,
		WM_MDICREATE              = 0x0220,
		WM_MDIDESTROY             = 0x0221,
		WM_MDIACTIVATE            = 0x0222,
		WM_MDIRESTORE             = 0x0223,
		WM_MDINEXT                = 0x0224,
		WM_MDIMAXIMIZE            = 0x0225,
		WM_MDITILE                = 0x0226,
		WM_MDICASCADE             = 0x0227,
		WM_MDIICONARRANGE         = 0x0228,
		WM_MDIGETACTIVE           = 0x0229,
		WM_MDISETMENU             = 0x0230,
		WM_ENTERSIZEMOVE          = 0x0231,
		WM_EXITSIZEMOVE           = 0x0232,
		WM_DROPFILES              = 0x0233,
		WM_MDIREFRESHMENU         = 0x0234,
		WM_IME_SETCONTEXT         = 0x0281,
		WM_IME_NOTIFY             = 0x0282,
		WM_IME_CONTROL            = 0x0283,
		WM_IME_COMPOSITIONFULL    = 0x0284,
		WM_IME_SELECT             = 0x0285,
		WM_IME_CHAR               = 0x0286,
		WM_IME_REQUEST            = 0x0288,
		WM_IME_KEYDOWN            = 0x0290,
		WM_IME_KEYUP              = 0x0291,
		WM_MOUSEHOVER             = 0x02A1,
		WM_MOUSELEAVE             = 0x02A3,
		WM_CUT                    = 0x0300,
		WM_COPY                   = 0x0301,
		WM_PASTE                  = 0x0302,
		WM_CLEAR                  = 0x0303,
		WM_UNDO                   = 0x0304,
		WM_RENDERFORMAT           = 0x0305,
		WM_RENDERALLFORMATS       = 0x0306,
		WM_DESTROYCLIPBOARD       = 0x0307,
		WM_DRAWCLIPBOARD          = 0x0308,
		WM_PAINTCLIPBOARD         = 0x0309,
		WM_VSCROLLCLIPBOARD       = 0x030A,
		WM_SIZECLIPBOARD          = 0x030B,
		WM_ASKCBFORMATNAME        = 0x030C,
		WM_CHANGECBCHAIN          = 0x030D,
		WM_HSCROLLCLIPBOARD       = 0x030E,
		WM_QUERYNEWPALETTE        = 0x030F,
		WM_PALETTEISCHANGING      = 0x0310,
		WM_PALETTECHANGED         = 0x0311,
		WM_HOTKEY                 = 0x0312,
		WM_PRINT                  = 0x0317,
		WM_PRINTCLIENT            = 0x0318,
		WM_THEME_CHANGED          = 0x031A,
		WM_HANDHELDFIRST          = 0x0358,
		WM_HANDHELDLAST           = 0x035F,
		WM_AFXFIRST               = 0x0360,
		WM_AFXLAST                = 0x037F,
		WM_PENWINFIRST            = 0x0380,
		WM_PENWINLAST             = 0x038F,
		WM_APP                    = 0x8000,
		WM_USER                   = 0x0400,
		WM_REFLECT                = WM_USER + 0x1c00
	}
	#endregion

	#region SetWindowPosFlags
    [Author("Franco, Gustavo")]
	[Flags]
	public enum SetWindowPosFlags
	{
		SWP_NOSIZE          = 0x0001,
		SWP_NOMOVE          = 0x0002,
		SWP_NOZORDER        = 0x0004,
		SWP_NOREDRAW        = 0x0008,
		SWP_NOACTIVATE      = 0x0010,
		SWP_FRAMECHANGED    = 0x0020,
		SWP_SHOWWINDOW      = 0x0040,
		SWP_HIDEWINDOW      = 0x0080,
		SWP_NOCOPYBITS      = 0x0100,
		SWP_NOOWNERZORDER   = 0x0200, 
		SWP_NOSENDCHANGING  = 0x0400,
		SWP_DRAWFRAME       = 0x0020,
		SWP_NOREPOSITION    = 0x0200,
		SWP_DEFERERASE      = 0x2000,
		SWP_ASYNCWINDOWPOS  = 0x4000
	}
	#endregion
}
