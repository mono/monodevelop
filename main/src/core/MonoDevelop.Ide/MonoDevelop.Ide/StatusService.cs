//
// NotificationService.cs
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

namespace MonoDevelop.Ide
{
	public static class StatusService
	{
		public static event EventHandler<NotificationServiceContextEventArgs> ContextAdded;
		public static event EventHandler<NotificationServiceContextEventArgs> ContextRemoved;

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

		/*
		static void ContextMessageChanged (object sender, NotificationContextMessageChangedArgs e)
		{
			NotificationContext ctx = (NotificationContext)sender;
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
		*/
		/*
		static void OnMessageChanged (NotificationContext context)
		{
			string message = context != null ? context.Message : null;
			bool isMarkup = context != null && context.IsMarkup;
			IconId image = context != null ? context.Image : IconId.Null;

			if (MessageChanged != null) {
				var args = new NotificationContextMessageChangedArgs (message, isMarkup, image);
				MessageChanged (this, args);
			}
		}

		static void UpdateMessage ()
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

		static void ResetUpdateTimer ()
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
				OnMessageChanged (ctx);
				nextContext++;
				if (nextContext >= activeContexts.Count) {
					nextContext = 0;
				}
			};

			changeMessageTimer.Start ();
		}
		*/

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
				var args = new NotificationServiceContextEventArgs (ctx);
				ContextAdded (null, args);
			}
		}

		static void OnContextRemoved (StatusMessageContext ctx)
		{
			if (ContextRemoved != null) {
				var args = new NotificationServiceContextEventArgs (ctx);
				ContextRemoved (null, args);
			}
		}
	}

	public class NotificationServiceContextEventArgs : EventArgs
	{
		public StatusMessageContext Context { get; private set; }

		public NotificationServiceContextEventArgs (StatusMessageContext context)
		{
			Context = context;
		}
	}
}

