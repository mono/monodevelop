//
// SearchBar.cs
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
using AppKit;
using Foundation;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components.Mac;
using MonoDevelop.Ide;
using Xwt.Mac;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	[Register]
	class SearchBar : NSSearchField
	{
		bool debugSearchbar;
		internal Widget gtkWidget;
		internal event EventHandler<Xwt.KeyEventArgs> KeyPressed;
		internal event EventHandler LostFocus;
		internal event EventHandler SelectionActivated;
		public event EventHandler GainedFocus;

		/// <summary>
		/// This tells whether events have been attached when created from the menu.
		/// </summary>
		internal bool EventsAttached;

		public SearchBar ()
		{
			Cell.Scrollable = true;
			Initialize ();
			var debugFilePath = System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".xs-searchbar-debug");
			debugSearchbar = System.IO.File.Exists (debugFilePath);
		}

		public SearchBar (IntPtr ptr) : base (ptr)
		{
			Initialize ();
		}

		internal void LogMessage (string message)
		{
			if (!debugSearchbar)
				return;

			LoggingService.LogInfo (message);
		}

		void Initialize ()
		{
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidResignKeyNotification, notification => DispatchService.GuiDispatch (() => {
				var other = (NSWindow)notification.Object;

				LogMessage ($"Lost focus from resign key: {other.DebugDescription}.");
				if (notification.Object == Window) {
					if (LostFocus != null)
						LostFocus (this, null);
				}
			}));
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidResizeNotification, notification => DispatchService.GuiDispatch (() => {
				var other = (NSWindow)notification.Object;
				LogMessage ("Lost focus from resize: {other.DebugDescription}.");
				if (notification.Object == Window) {
					if (LostFocus != null)
						LostFocus (this, null);
				}
			}));
		}

		bool SendKeyPressed (Xwt.KeyEventArgs kargs)
		{
			if (KeyPressed != null)
				KeyPressed (this, kargs);

			LogMessage ($"KeyPressed with Handled {kargs.Handled}");
			return kargs.Handled;
		}

		public override bool PerformKeyEquivalent (NSEvent theEvent)
		{
			var popupHandled = SendKeyPressed (theEvent.ToXwtKeyEventArgs ());
			LogMessage ($"Popup handled {popupHandled}");
			if (popupHandled)
				return true;
			var baseHandled = base.PerformKeyEquivalent (theEvent);;
			LogMessage ($"Base handled {baseHandled}");
			return baseHandled;
		}

		public override void DidEndEditing (NSNotification notification)
		{
			base.DidEndEditing (notification);

			LogMessage ("Did end editing");

			nint value = ((NSNumber)notification.UserInfo.ValueForKey ((NSString)"NSTextMovement")).LongValue;
			if (value == (nint)(long)NSTextMovement.Tab) {
				LogMessage ("Tab movement");
				SelectText (this);
				return;
			}

			if (value == (nint)(long)NSTextMovement.Return) {
				LogMessage ("Activated by enter");
				if (SelectionActivated != null)
					SelectionActivated (this, null);
				return;
			}

			LogMessage ($"Got NSTextMovement: {value}");

			// This means we've reached a focus loss event.
			var replacedWith = notification.UserInfo.ValueForKey ((NSString)"_NSFirstResponderReplacingFieldEditor");
			if (replacedWith != this && LostFocus != null) {
				LogMessage ($"Mouse focus loss to {replacedWith.DebugDescription}");
				LostFocus (this, null);
			}
		}

		public override void ViewDidMoveToWindow ()
		{
			base.ViewDidMoveToWindow ();

			LogMessage ("View moved to parent window");
			// Needs to be grabbed after it's parented.
			gtkWidget = GtkMacInterop.NSViewToGtkWidget (this);
		}

		public override bool BecomeFirstResponder ()
		{
			LogMessage ("Becoming first responder");
			bool firstResponder = base.BecomeFirstResponder ();
			if (firstResponder)
				Focus ();

			return firstResponder;
		}

		public void Focus ()
		{
			LogMessage ("Focused");
			if (GainedFocus != null)
				GainedFocus (this, EventArgs.Empty);
		}
	}
}

