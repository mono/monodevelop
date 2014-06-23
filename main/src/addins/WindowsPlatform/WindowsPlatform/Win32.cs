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
    [Author("Franco, Gustavo")]
    public static class Win32
    {
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0; // 'Large icon
        public const uint SHGFI_SMALLICON = 0x1; // 'Small icon
        public const uint SHGFI_ICONLOCATION = 0x1000;
        public const uint SHGFI_TYPENAME = 0x400;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        public const uint FILE_ATTRIBUTES_NORMAL = 0x80;
        internal const string USER32 = "user32.dll";
        internal const string SHELL32 = "shell32.dll";
	 internal const string SHLWAPI = "Shlwapi.dll";

        #region Delegates
        public delegate bool EnumWindowsCallBack(IntPtr hWnd, int lParam);
        #endregion

        #region USER32
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport(Win32.USER32)]
        public static extern int GetDlgCtrlID(IntPtr hWndCtl);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern int MapWindowPoints(IntPtr hWnd, IntPtr hWndTo, ref POINT pt, int cPoints);
        [DllImport(Win32.USER32, SetLastError = true)]
        public static extern bool GetWindowInfo(IntPtr hwnd, out WINDOWINFO pwi);
        [DllImport(Win32.USER32)]
        public static extern void GetWindowText(IntPtr hWnd, StringBuilder param, int length);
        [DllImport(Win32.USER32)]
        public static extern void GetClassName(IntPtr hWnd, StringBuilder param, int length);
        [DllImport(Win32.USER32)]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsCallBack lpEnumFunc, int lParam);
        [DllImport(Win32.USER32)]
        public static extern bool EnumWindows(EnumWindowsCallBack lpEnumFunc, int lParam);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern bool ReleaseCapture();
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern IntPtr SetCapture(IntPtr hWnd);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern IntPtr ChildWindowFromPointEx(IntPtr hParent, POINT pt, ChildFromPointFlags flags);
        [DllImport(Win32.USER32, EntryPoint = "FindWindowExA", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport(Win32.USER32)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern int PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern int PostMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, StringBuilder param);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, char[] chars);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern IntPtr BeginDeferWindowPos(int nNumWindows);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern IntPtr DeferWindowPos(IntPtr hWinPosInfo, IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int Width, int Height, SetWindowPosFlags flags);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern bool EndDeferWindowPos(IntPtr hWinPosInfo);
        [DllImport(Win32.USER32, CharSet = CharSet.Auto)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int Width, int Height, SetWindowPosFlags flags);
        [DllImport(Win32.USER32)]
        public static extern bool GetWindowRect(IntPtr hwnd, ref RECT rect);
        [DllImport(Win32.USER32)]
        public static extern bool GetClientRect(IntPtr hwnd, ref RECT rect);
        [DllImport(Win32.USER32)]
        public static extern bool DestroyIcon([In] IntPtr hIcon);
        [DllImport(Win32.SHELL32, CharSet = CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfoW([In] string pszPath, uint dwFileAttributes, [In, Out] ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
        #endregion

	 [DllImport (Win32.SHLWAPI, SetLastError = true, CharSet = CharSet.Unicode)]
	 public static extern int AssocQueryStringW (AssociationFlags flags, AssociationString str, string assoc, string extra, StringBuilder outBuffer, ref int outBufferSize);
    }

	[AttributeUsage(AttributeTargets.Class |
		AttributeTargets.Enum |
		AttributeTargets.Interface |
		AttributeTargets.Struct,
		AllowMultiple = true)]
	[Author("Franco, Gustavo")]
	internal class AuthorAttribute : Attribute
	{
		#region Constructors
		public AuthorAttribute(string authorName)
		{
		}
		#endregion
	}

	[Flags]
	public enum AssociationFlags {
		None = 0x00000000,
		InitNoRemapClsid = 0x00000001,
		InitByExeName = 0x00000002,
		OpenByExeName = 0x00000002,
		InitDefaultToStar = 0x00000004,
		InitDefaultToFolder = 0x00000008,
		NoUserSettings = 0x00000010,
		NoTruncate = 0x00000020,
		Verify = 0x00000040,
		RemapRunDll = 0x00000080,
		NoFixups = 0x00000100,
		IgnoreBaseClass = 0x00000200,
		InitIgnoreUnknown = 0x00000400,
		InitFixedProgid = 0x00000800,
		IsProtocol = 0x00001000
	}

	public enum AssociationString {
		Command = 1,
		Executable,
		FriendlyDocName,
		FriendlyAppName,
		NoOpen,
		ShellNewValue,
		DdeCommand,
		DdeIfExec,
		DdeApplication,
		DdeTopic,
		InfoTip,
		QuickTip,
		Tileinfo,
		ContentType,
		DefaultIcon,
		ShellExtension,
		DropTrget,
		DelegateExecute,
		SupportedUriProtocols,
		MaxString
	}
}
