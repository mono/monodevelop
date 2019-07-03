//
// Author:
//       Rolf Kvinge <rolf@xamarin.com>
//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using Foundation;
using ObjCRuntime;

namespace AppKit
{
	public static class NSViewExtensions
	{
		delegate nint view_compare_func (IntPtr view1, IntPtr view2, IntPtr context);
		static view_compare_func view_comparer = view_compare;

		sealed class SortData
		{
			public Exception Exception;
			public Func<NSView, NSView, NSComparisonResult> Comparer;
		}

		[MonoPInvokeCallback (typeof (view_compare_func))]
		static nint view_compare (IntPtr view1, IntPtr view2, IntPtr context)
		{
			var data = (SortData)GCHandle.FromIntPtr (context).Target;
			try {
				var a = (NSView)Runtime.GetNSObject (view1);
				var b = (NSView)Runtime.GetNSObject (view2);
				return (nint)(long)data.Comparer (a, b);
			} catch (Exception e) {
				data.Exception = e;
				return (nint)(long)NSComparisonResult.Same;
			}
		}

		public static void SortSubviews (this NSView view, Func<NSView, NSView, NSComparisonResult> comparer)
		{
			if (comparer == null)
				throw new ArgumentNullException (nameof (comparer));

			var func = Marshal.GetFunctionPointerForDelegate (view_comparer);
			var context = new SortData () { Comparer = comparer };
			var handle = GCHandle.Alloc (context);
			try {
				SortSubviews (view, func, GCHandle.ToIntPtr (handle));
				if (context.Exception != null)
					throw new Exception ($"An exception occurred during sorting.", context.Exception);
			} finally {
				handle.Free ();
			}
		}

		static readonly IntPtr sel_sortSubviewsUsingFunction_context_ = Selector.GetHandle ("sortSubviewsUsingFunction:context:");
		static void SortSubviews (NSView view, IntPtr function_pointer, IntPtr context)
		{
			MonoDevelop.Components.Mac.Messaging.void_objc_msgSend_IntPtr_IntPtr (view.Handle, sel_sortSubviewsUsingFunction_context_, function_pointer, context);
		}
	}
}
