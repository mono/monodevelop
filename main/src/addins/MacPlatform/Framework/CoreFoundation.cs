// 
// CoreFoundation.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//       Miguel de Icaza
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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

namespace OSXIntegration.Framework
{
	
	
	internal static class CoreFoundation
	{
		const string CFLib = "/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation";
		
		[DllImport (CFLib)]
		static extern IntPtr CFStringCreateWithCString (IntPtr alloc, string str, int encoding);
		
		public static IntPtr CreateString (string s)
		{
			// The magic value is "kCFStringENcodingUTF8"
			return CFStringCreateWithCString (IntPtr.Zero, s, 0x08000100);
		}
		
		[DllImport (CFLib, EntryPoint="CFRelease")]
		public static extern void Release (IntPtr cfRef);
	}
}
