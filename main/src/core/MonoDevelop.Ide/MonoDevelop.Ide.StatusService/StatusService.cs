//
// StatusService.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
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
using System.Timers;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Status
{
	public static class StatusService
	{
		public static event EventHandler<StatusServiceContextEventArgs> ContextAdded;
		public static event EventHandler<StatusServiceContextEventArgs> ContextRemoved;

		readonly static StatusMessageContext mainContext;
		readonly static List<StatusMessageContext> contexts = new List<StatusMessageContext> ();
		/*
		static Timer changeMessageTimer;
		static int nextContext;
		*/

		static StatusService ()
		{
			mainContext = new StatusMessageContext ();
			contexts.Add (mainContext);
		}

		public static StatusMessageContext MainContext {
			get { return mainContext; }
		}

		public static StatusMessageContext CreateContext ()
		{
			var ctx = new StatusMessageContext ();
			contexts.Add (ctx);

			OnContextAdded (ctx);
			return ctx;
		}

		public static StatusBarIcon ShowStatusIcon (Xwt.Drawing.Image pixbuf)
		{
			return IdeApp.Workbench.StatusBar.ShowStatusIcon (pixbuf);
		}

		internal static void Remove (StatusMessageContext ctx)
		{
			if (ctx == mainContext) {
				return;
			}

			contexts.Remove (ctx);
			OnContextRemoved (ctx);
		}

		static void OnContextAdded (StatusMessageContext ctx)
		{
			if (ContextAdded != null) {
				var args = new StatusServiceContextEventArgs (ctx);
				ContextAdded (null, args);
			}
		}

		static void OnContextRemoved (StatusMessageContext ctx)
		{
			if (ContextRemoved != null) {
				var args = new StatusServiceContextEventArgs (ctx);
				ContextRemoved (null, args);
			}
		}
	}

	public class StatusServiceContextEventArgs : EventArgs
	{
		public StatusMessageContext Context { get; private set; }

		public StatusServiceContextEventArgs (StatusMessageContext context)
		{
			Context = context;
		}
	}
}
