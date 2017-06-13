//
// MacLeakHelpers.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2017 2017
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
using System.Collections.Generic;
using System.Linq;

#if DEBUG
#if MAC
namespace MonoDevelop.Components.Mac
{
	class MacLeakHelpers
	{
		// NOTE: When invoking these, the debugger will create a strong reference to the object for the entire session, so only invoke this
		// to check for leaks after a running an operation a few times (i.e. debug an app), then checking whether any NSObjects were leaked.
		// Also, place GC.Collect() a few times followed by GC.WaitForPendingFinalizers() before your breakpoint.
		static Dictionary<IntPtr, WeakReference> Dict {
			get {
				var fieldInfo = typeof (ObjCRuntime.Runtime).GetField ("object_map", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
				return (Dictionary<IntPtr, WeakReference>)fieldInfo.GetValue (null);
			}
		}

		static object [] Values {
			get {
				return Dict.Values
						.Select (x => x.Target)
						.ToArray ();
			}
		}

		static T [] GetValues<T> ()
		{
			return Values
				.OfType<T> ()
				.ToArray ();
		}
	}
}
#endif
#endif
