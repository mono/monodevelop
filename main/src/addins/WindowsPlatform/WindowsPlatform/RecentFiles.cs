// 
// RecentFiles.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;
using MonoDevelop.Core;
using MonoDevelop.Ide.Desktop;
using System.Collections.Generic;
using CustomControls.OS;

namespace MonoDevelop.Platform
{
	// We don't bother with the registry MRU storage because FdoRecentFiles works fine, and it wouldn't
	// gain us anything AFAICT.
	//
	// However, we have this custom subclass of FdoRecentFiles for two key differences -
	// This has two key differences from the base versionL
	// 1. Call SHAddToRecentDocs on adding recent ptojects, so they show up in start menu and jump lists
	// 2. Store the .recently-used in a private folder instead of annoying the user by putting it in Documents
	//
	public class WindowsRecentFiles : FdoRecentFiles
	{
		public WindowsRecentFiles () : base (UserProfile.Current.LocalConfigDir.Combine ("RecentlyUsed.xml"))
		{
		}
		
		public override void AddProject (string fileName, string displayName)
		{
			SHAddToRecentDocs (SHARD.PATHW, fileName);
			base.AddProject (fileName, displayName);
		}
		
		[DllImport (Win32.SHELL32, CharSet = CharSet.Unicode)]
		static extern void SHAddToRecentDocs (SHARD uFlags, string pv);
		
		[DllImport (Win32.SHELL32, CharSet = CharSet.Unicode)]
		static extern void SHAddToRecentDocs (SHARD uFlags, IntPtr pv);
		
		enum SHARD : uint
		{
			PIDL              = 0x00000001,
			PATHA             = 0x00000002,
			PATHW             = 0x00000003,
			//Below are Windows 7+ only
			APPIDINFO         = 0x00000004,
			APPIDINFOIDLIST   = 0x00000005,
			LINK              = 0x00000006,
			APPIDINFOLINK     = 0x00000007,
			SHELLITEM         = 0x00000008,
		}
	}
}

