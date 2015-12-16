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

namespace MonoDevelop.Components.MainToolbar
{
	class StatusBarContextHandler
	{
		public event EventHandler<NotificationContextMessageChangedArgs> MessageChanged;
		public event EventHandler<NotificationContextProgressChangedArgs> ProgressChanged;

		readonly List<StatusMessageContext> activeContexts = new List<StatusMessageContext> ();
		Timer changeMessageTimer;
		int nextContext;

		public StatusBarContextHandler ()
		{
			StatusService.ContextAdded += NotificationServiceContextAdded;
			StatusService.ContextRemoved += NotificationServiceContextRemoved;
			StatusService.MainContext.MessageChanged += ContextMessageChanged;
			StatusService.MainContext.ProgressChanged += ContextProgressChanged;
		}

		void NotificationServiceContextAdded (object sender, NotificationServiceContextEventArgs e)
		{
			e.Context.MessageChanged += ContextMessageChanged;
			e.Context.ProgressChanged += ContextProgressChanged;
		}

		void NotificationServiceContextRemoved (object sender, NotificationServiceContextEventArgs e)
		{
			e.Context.MessageChanged -= ContextMessageChanged;
			e.Context.ProgressChanged -= ContextProgressChanged;
			activeContexts.Remove (e.Context);

			UpdateMessage ();
		}

		void ContextMessageChanged (object sender, NotificationContextMessageChangedArgs e)
		{
			StatusMessageContext ctx = (StatusMessageContext)sender;
			if (!activeContexts.Contains (ctx)) {
				activeContexts.Add (ctx);
			} else {
				// Remove it from the list and insert it at the end if it's not an empty context
				activeContexts.Remove (ctx);

				if (ctx.Message != null && ctx.Image != IconId.Null) {
					activeContexts.Add (ctx);
				}
			}

			UpdateMessage ();
		}

		void ContextProgressChanged (object sender, NotificationContextProgressChangedArgs e)
		{
			if (ProgressChanged != null) {
				ProgressChanged (this, e);
			}
		}

		void OnMessageChanged (StatusMessageContext context)
		{
			string message = context != null ? context.Message : "";
			bool isMarkup = context != null && context.IsMarkup;
			IconId image = context != null ? context.Image : IconId.Null;

			if (MessageChanged != null) {
				var args = new NotificationContextMessageChangedArgs (context, message, isMarkup, image);

				// Enforce dispatch on GUI thread so clients don't need to care.
				DispatchService.GuiDispatch (() => MessageChanged (this, args));
			}
		}

		void UpdateMessage ()
		{
			if (activeContexts.Count != 0) {
				// Display the newest active context
				var ctx = activeContexts.Last ();
				OnMessageChanged (ctx);
			} else {
				OnMessageChanged (null);
			}

			nextContext = 0;
			ResetUpdateTimer ();
		}

		void ResetUpdateTimer ()
		{
			// Shut down the old timer;
			if (changeMessageTimer != null) {
				changeMessageTimer.Dispose ();
				changeMessageTimer = null;
			}

			if (activeContexts.Count <= 1) {
				// If we don't need a new timer, just return
				return;
			}

			changeMessageTimer = new Timer { Interval = 5000, AutoReset = true };
			changeMessageTimer.Elapsed += (object sender, ElapsedEventArgs e) => {
				var ctx = activeContexts[nextContext];

				nextContext++;
				if (nextContext >= activeContexts.Count) {
					nextContext = 0;
				}

				OnMessageChanged (ctx);
			};

			changeMessageTimer.Start ();
		}
	}
}
