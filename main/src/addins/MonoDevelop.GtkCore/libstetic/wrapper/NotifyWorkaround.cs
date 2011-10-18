// 
// NotifyWorkaround.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using GLib;
using System;
using System.Runtime.InteropServices;


namespace Stetic.Wrapper
{
	/// <summary>
	/// This class contains a notification work around for a gtk 3 bug where notifications could yield a
	/// System.NullReferenceException: Object reference not set to an instance of an object at GLib.Object.NotifyCallback (IntPtr handle, IntPtr pspec, IntPtr gch)
	/// </summary>
	static class NotifyWorkaround
	{
		[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
		delegate void NotifyDelegate (IntPtr handle, IntPtr pspec, IntPtr gch);
		
		static NotifyDelegate delegateInstance = new NotifyDelegate (NotifyCallback);

		static void NotifyCallback (IntPtr handle, IntPtr pspec, IntPtr gch)
		{
			try {
				var sig = ((GCHandle) gch).Target as GLib.Signal;
				if (sig == null)
					throw new Exception("Unknown signal GC handle received " + gch);
				
				var handler = sig.Handler as NotifyHandler;
				if (handler != null) {
					handler (GLib.Object.GetObject (handle), new NotifyArgs () {
						Args = new object[] { pspec }
					});
				}
			} catch (Exception e) {
				ExceptionManager.RaiseUnhandledException (e, false);
			}
		}
		
		static void ConnectNotification (GLib.Object w, string signal, NotifyHandler handler)
		{
			var sig = GLib.Signal.Lookup (w, signal, delegateInstance);
			sig.AddDelegate (handler);
		}
		
		public static void AddNotification (GLib.Object w, NotifyHandler handler)
		{
			ConnectNotification (w, "notify", handler);
		}
		
		public static void AddNotification (GLib.Object w, string property, NotifyHandler handler)
		{
			ConnectNotification (w, "notify::" + property, handler);
		}
		
		static void DisconnectNotification (GLib.Object w, string signal, NotifyHandler handler)
		{
			var sig = GLib.Signal.Lookup (w, signal, delegateInstance);
			sig.RemoveDelegate (handler);
		}
		
		public static void RemoveNotification (GLib.Object w, NotifyHandler handler)
		{
			DisconnectNotification (w, "notify", handler);
		}
		
	}
}

