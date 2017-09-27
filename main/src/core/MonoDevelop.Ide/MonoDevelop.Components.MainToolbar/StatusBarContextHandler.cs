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
using System.Linq;
using System.Timers;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Status;

namespace MonoDevelop.Components.MainToolbar
{
	class StatusBarContextHandler
	{
		public event EventHandler<StatusMessageContextMessageChangedArgs> MessageChanged;
		public event EventHandler<StatusMessageContextProgressChangedArgs> ProgressChanged;

		readonly List<StatusMessageContext> activeContexts = new List<StatusMessageContext> ();

		public StatusBarContextHandler ()
		{
			StatusService.ContextAdded += StatusServiceContextAdded;
			StatusService.ContextRemoved += StatusServiceContextRemoved;
			StatusService.MainContext.MessageChanged += ContextMessageChanged;
			StatusService.MainContext.ProgressChanged += ContextProgressChanged;
		}

		void StatusServiceContextAdded (object sender, StatusServiceContextEventArgs e)
		{
			e.Context.MessageChanged += ContextMessageChanged;
			e.Context.ProgressChanged += ContextProgressChanged;

			// This will be added to the active contexts once a message or progress has been set.
		}

		void StatusServiceContextRemoved (object sender, StatusServiceContextEventArgs e)
		{
			e.Context.MessageChanged -= ContextMessageChanged;
			e.Context.ProgressChanged -= ContextProgressChanged;

			bool wasActive = (activeContexts[0] == e.Context);
			activeContexts.Remove (e.Context);

			if (wasActive) {
				UpdateMessage ();
			}
		}

		void ContextMessageChanged (object sender, StatusMessageContextMessageChangedArgs e)
		{
			StatusMessageContext ctx = (StatusMessageContext)sender;
			if (!activeContexts.Contains (ctx)) {
				activeContexts.Insert (0, ctx);
			}

			if (activeContexts [0] == ctx) {
				UpdateMessage ();
			}
		}

		void ContextProgressChanged (object sender, StatusMessageContextProgressChangedArgs e)
		{
			StatusMessageContext ctxt = (StatusMessageContext) sender;
			if (!activeContexts.Contains(ctxt)) {
				activeContexts.Insert(0, ctxt);
			}

			if (activeContexts[0] == ctxt) {
				if (ProgressChanged != null) {
					Runtime.RunInMainThread(() => ProgressChanged (this, e));
				}
			}
		}

		void OnMessageChanged (StatusMessageContext context)
		{
			string message = context != null ? context.Message : "";
			bool isMarkup = context != null && context.IsMarkup;
			IconId image = context != null ? context.Image : IconId.Null;

			if (MessageChanged != null) {
				var args = new StatusMessageContextMessageChangedArgs (context, message, isMarkup, image);

				// Enforce dispatch on GUI thread so clients don't need to care.
				Runtime.RunInMainThread (() => MessageChanged (this, args));
			}
		}

		void UpdateMessage ()
		{
			if (activeContexts.Count != 0) {
				// Display the newest active context
				var ctx = activeContexts[0];
				OnMessageChanged (ctx);
			} else {
				OnMessageChanged (null);
			}
		}
	}
}