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
    public static class Win32
    {
        internal const string USER32 = "user32.dll";
        internal const string SHELL32 = "shell32.dll";
        internal const string SHLWAPI = "Shlwapi.dll";

        [DllImport (Win32.SHLWAPI, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int AssocQueryStringW (AssociationFlags flags, AssociationString str, string assoc, string extra, StringBuilder outBuffer, ref int outBufferSize);
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
