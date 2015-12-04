//
// StatusBarContextHandler.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Ide;

namespace MonoDevelop.Components.MainToolbar
{
	class StatusBarContextHandler
	{
		readonly MainStatusBarContextImpl mainContext;
		readonly List<StatusBarContextImpl> contexts = new List<StatusBarContextImpl> ();
		StatusBarContextImpl activeContext;

		internal StatusBar StatusBar {
			get;
			private set;
		}

		public StatusBarContextHandler (StatusBar statusBar)
		{
			StatusBar = statusBar;
			mainContext = new MainStatusBarContextImpl (this);
			activeContext = mainContext;
			contexts.Add (mainContext);
		}

		internal StatusBar MainContext {
			get { return mainContext; }
		}

		public StatusBarContext CreateContext ()
		{
			var ctx = new StatusBarContextImpl (this);
			contexts.Add (ctx);
			return ctx;
		}

		public bool IsCurrentContext (StatusBarContextImpl ctx)
		{
			return ctx == activeContext;
		}

		public void Remove (StatusBarContextImpl ctx)
		{
			if (ctx == mainContext)
				return;

			StatusBarContextImpl oldActive = activeContext;
			contexts.Remove (ctx);
			UpdateActiveContext ();
			if (oldActive != activeContext) {
				// Removed the active context. Update the status bar.
				activeContext.Update ();
			}
		}

		public void UpdateActiveContext ()
		{
			for (int n = contexts.Count - 1; n >= 0; n--) {
				StatusBarContextImpl ctx = contexts [n];
				if (ctx.StatusChanged) {
					if (ctx != activeContext) {
						activeContext = ctx;
						activeContext.Update ();
					}
					return;
				}
			}
			throw new InvalidOperationException (); // There must be at least the main context
		}
	}
}
