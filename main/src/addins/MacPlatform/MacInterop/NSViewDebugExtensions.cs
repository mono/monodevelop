//
// NSViewDebugExtensions.cs
//
// Author:
//       iain <iain@xamarin.com>
//
// Copyright (c) Xamarin, Inc 2016 
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

#if DEBUG

using System;
using System.Runtime.InteropServices;

using AppKit;
using Foundation;

namespace MacInterop
{
	public static class NSViewDebugExtensions
	{
		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
		static extern IntPtr IntPtr_objc_msgSend_void (IntPtr receiver, IntPtr selector);
		static readonly IntPtr _subtreeDescriptionSelector = ObjCRuntime.Selector.GetHandle ("_subtreeDescription");

		public static string _subtreeDescription (this NSView view)
		{
			var strHandle = IntPtr_objc_msgSend_void (view.Handle, _subtreeDescriptionSelector);
			return NSString.FromHandle (strHandle);
		}
	}
}

#endif

