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
using MonoDevelop.Components.Mac;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	[Register]
	class SearchBar : NSSearchField
	{
		internal Widget gtkWidget;
		internal event EventHandler<Xwt.KeyEventArgs> KeyPressed;
		internal event EventHandler LostFocus;
		new internal event EventHandler Activated;

		/// <summary>
		/// This tells whether events have been attached when created from the menu.
		/// </summary>
		internal bool EventsAttached;

		public SearchBar ()
		{
			Cell.Scrollable = true;
			Initialize ();
		}

		public SearchBar (IntPtr ptr) : base (ptr)
		{
			Initialize ();
		}

		void Initialize ()
		{
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidResignKeyNotification, notification => {
				if (notification.Object == Window)
					if (LostFocus != null)
						LostFocus (this, null);
			});
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidResizeNotification, notification => {
				if (notification.Object == Window)
					if (LostFocus != null)
						LostFocus (this, null);
			});
		}

		static Xwt.ModifierKeys TranslateMask (NSEventModifierMask mask)
		{
			Xwt.ModifierKeys xwtMask = Xwt.ModifierKeys.None;
			if (mask.HasFlag (NSEventModifierMask.CommandKeyMask))
				xwtMask |= Xwt.ModifierKeys.Command;
			if (mask.HasFlag (NSEventModifierMask.ControlKeyMask))
				xwtMask |= Xwt.ModifierKeys.Control;
			if (mask.HasFlag (NSEventModifierMask.ShiftKeyMask))
				xwtMask |= Xwt.ModifierKeys.Shift;
			if (mask.HasFlag (NSEventModifierMask.AlternateKeyMask))
				xwtMask |= Xwt.ModifierKeys.Alt;
			return xwtMask;
		}

		static Xwt.Key TranslateKey (int key)
		{
			switch (key)
			{
			case 63232: //case NSFunctionKey.UpArrow:
				return Xwt.Key.Up;
			case 63233: //case NSFunctionKey.DownArrow:
				return Xwt.Key.Down;
			case 63234: //case NSFunctionKey.LeftArrow:
				return Xwt.Key.Left;
			case 63235: //case NSFunctionKey.RightArrow:
				return Xwt.Key.Right;
			case 63273: //case NSFunctionKey.Home:
				return Xwt.Key.Home;
			case 63275: //case NSFunctionKey.End:
				return Xwt.Key.End;
			case 63276: //case NSFunctionKey.PageUp:
				return Xwt.Key.PageUp;
			case 63277: //NSFunctionKey.PageDown:
				return Xwt.Key.PageDown;
			}
			return 0;
		}

		bool SendKeyPressed (Xwt.Key key, Xwt.ModifierKeys mask)
		{
			var kargs = new Xwt.KeyEventArgs (key, mask, false, 0);
			if (KeyPressed != null)
				KeyPressed (this, kargs);

			return kargs.Handled;
		}

		public override bool PerformKeyEquivalent (NSEvent theEvent)
		{
			int charKey = (int)theEvent.CharactersIgnoringModifiers[0];
			if (charKey == 27) {
				SendKeyPressed (Xwt.Key.Escape, Xwt.ModifierKeys.None);
				base.PerformKeyEquivalent (theEvent);
				return true;
			}

			// Use CharactersIgnoringModifiers instead of KeyCode. They don't match Xwt anyway.
			if (SendKeyPressed (TranslateKey ((int)theEvent.CharactersIgnoringModifiers[0]), TranslateMask (theEvent.ModifierFlags)))
				return true;

			return base.PerformKeyEquivalent (theEvent);
		}

		public override void DidEndEditing (NSNotification notification)
		{
			base.DidEndEditing (notification);

			nint value = ((NSNumber)notification.UserInfo.ValueForKey ((NSString)"NSTextMovement")).LongValue;
			if (value == (nint)(long)NSTextMovement.Tab) {
				SelectText (this);
				return;
			}

			if (value == (nint)(long)NSTextMovement.Return) {
				if (Activated != null)
					Activated (this, null);
				return;
			}

			// This means we've reached a focus loss event.
			var replacedWith = notification.UserInfo.ValueForKey ((NSString)"_NSFirstResponderReplacingFieldEditor");
			if (replacedWith != this && LostFocus != null)
				LostFocus (this, null);
		}

		public override void ViewDidMoveToWindow ()
		{
			base.ViewDidMoveToWindow ();

			// Needs to be grabbed after it's parented.
			gtkWidget = GtkMacInterop.NSViewToGtkWidget (this);
		}
	}
}

