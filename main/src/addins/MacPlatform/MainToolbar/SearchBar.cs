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
using CoreGraphics;
using Gtk;
using MonoDevelop.Core;

using MonoDevelop.Ide;
using Xwt.Mac;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	[Register]
	class SearchBar : NSSearchField
	{
		internal Widget gtkWidget;
		internal event EventHandler<Xwt.KeyEventArgs> KeyPressed;
		internal event EventHandler LostFocus;
		internal event EventHandler SelectionActivated;
		public event EventHandler GainedFocus;

		// To only draw the border, NSSearchFieldCell needs to be subclassed. Unfortunately this stops the 
		// animation on activation working. I suspect this is implemented inside the NSSearchField rather
		// than the NSSearchFieldCell which can't do animation.
		class DarkThemeSearchFieldCell : NSSearchFieldCell
		{
			public override void DrawWithFrame (CGRect cellFrame, NSView inView)
			{
				if (IdeApp.Preferences.UserInterfaceTheme == Theme.Dark) {
					var inset = cellFrame.Inset (0.25f, 0.25f);
					if (!ShowsFirstResponder) {
						var path = NSBezierPath.FromRoundedRect (inset, 3, 3);
						path.LineWidth = 0.5f;

						// Hack to make the border be the correct colour in fullscreen mode
						// See comment in AwesomeBar.cs for more details
						if (MainToolbar.IsFullscreen) {
							Styles.DarkBorderBrokenColor.ToNSColor ().SetStroke ();
						} else {
							Styles.DarkBorderColor.ToNSColor ().SetStroke ();
						}
						path.Stroke ();
					}

					// Can't just call base.DrawInteriorWithFrame because it draws the placeholder text
					// with a strange emboss effect when it the view is not first responder.
					// Again, probably because the NSSearchField handles the not first responder state itself
					// rather than using NSSearchFieldCell
					//base.DrawInteriorWithFrame (inset, inView);

					// So instead, draw the various extra cells and text in the correct places
					SearchButtonCell.DrawWithFrame (SearchButtonRectForBounds (inset), inView);

					if (!ShowsFirstResponder) {
						PlaceholderAttributedString.DrawInRect (SearchTextRectForBounds (inset));
					}

					if (!string.IsNullOrEmpty (StringValue)) {
						CancelButtonCell.DrawWithFrame (CancelButtonRectForBounds (inset), inView);
					}
				} else {
					if (inView.Window?.Screen?.BackingScaleFactor == 2) {
						nfloat yOffset = 0f;
						nfloat hOffset = 0f;

						if (MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan) {
							if (inView.Window.IsKeyWindow) {
								yOffset = 0.5f;
								hOffset = -0.5f;
							} else {
								yOffset = 0f;
								hOffset = 1.0f;
							}
						} else {
							yOffset = 1f;
							hOffset = -1f;
						}
						cellFrame = new CGRect (cellFrame.X, cellFrame.Y + yOffset, cellFrame.Width, cellFrame.Height + hOffset);
					} else {
						nfloat yOffset = 0f;
						nfloat hOffset = 0f;

						cellFrame = new CGRect (cellFrame.X, cellFrame.Y + yOffset, cellFrame.Width, cellFrame.Height + hOffset);
					}
					base.DrawWithFrame (cellFrame, inView);
				}
			}

			// This is the rect for the placeholder text, not the text field entry
			public override CGRect SearchTextRectForBounds (CGRect rect)
			{
				if (ShowsFirstResponder) {
					rect = new CGRect (rect.X + 26, 0, rect.Width - 52, 22);
				} else {
					nfloat y = MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 4 : 3;
					rect = new CGRect (rect.X + 28, y, rect.Width - 56, 22);
				}

				return rect;
			}

			// The rect for the search icon
			public override CGRect SearchButtonRectForBounds (CGRect rect)
			{
				rect = new CGRect (0, 0, 26, rect.Height);
				return rect;
			}

			// The rect for the cancel button
			public override CGRect CancelButtonRectForBounds (CGRect rect)
			{
				rect = new CGRect (rect.X + rect.Width - 26.0, 0, 26, rect.Height);

				return rect;
			}

			// When customising the NSCell these are the methods which determine
			// where the editing and selecting text appears
			public override void EditWithFrame (CGRect aRect, NSView inView, NSText editor, NSObject delegateObject, NSEvent theEvent)
			{
				aRect = new CGRect (aRect.X, aRect.Y + 10, aRect.Width - 66, aRect.Height);
				base.EditWithFrame (aRect, inView, editor, delegateObject, theEvent);
			}

			public override void SelectWithFrame (CGRect aRect, NSView inView, NSText editor, NSObject delegateObject, nint selStart, nint selLength)
			{
				nfloat xOffset = 0;
				if (IdeApp.Preferences.UserInterfaceTheme == Theme.Dark) {
					xOffset = -1.5f;
				}
				// y does not appear to affect anything. Whatever value is set here for y will always be 1px below the
				// placeholder text
				aRect = new CGRect (aRect.X + xOffset, aRect.Y, aRect.Width, aRect.Height);
				base.SelectWithFrame (aRect, inView, editor, delegateObject, selStart, selLength);
			}
		}

		static string FirstResponderPlaceholder {
			get {
				return GettextCatalog.GetString ("Search");
			}
		}

		string placeholderText;
		public string PlaceholderText {
			get {
				return placeholderText ?? FirstResponderPlaceholder;
			}
			set {
				placeholderText = value ?? FirstResponderPlaceholder;
				PlaceholderAttributedString = MakePlaceholderString (placeholderText);
			}
		}

		public SearchBar ()
		{
			Cell = new DarkThemeSearchFieldCell ();

			var nsa = (INSAccessibility)this;

			AccessibilitySubrole = NSAccessibilitySubroles.SearchFieldSubrole;
			nsa.AccessibilityIdentifier = "MainToolbar.SearchField";
			AccessibilityHelp = GettextCatalog.GetString ("Search");
			// Hide this from the A11y system because we actually care about the inner search field
			// and not this one according to Cocoa?
			AccessibilityElement = false;

			Initialize ();

			Ide.Gui.Styles.Changed +=  (o, e) => UpdateLayout ();
			UpdateLayout ();
		}

		public override bool AccessibilityPerformShowMenu ()
		{
			Cell.SearchButtonCell.PerformClick (this);
			return true;
		}

		public override bool AccessibilityPerformConfirm ()
		{
			return true;
		}

		NSAttributedString MakePlaceholderString (string t)
		{
			return new NSAttributedString (t, foregroundColor: NSColor.FromRgba (0.63f, 0.63f, 0.63f, 1.0f));
		}

		void UpdateLayout ()
		{
			Bezeled = true;
			BezelStyle = NSTextFieldBezelStyle.Rounded;
			Editable = true;
			Cell.Scrollable = true;
			Selectable = true;

			PlaceholderAttributedString = MakePlaceholderString (PlaceholderText);
		}

		void Initialize ()
		{
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidResignKeyNotification, notification => Runtime.RunInMainThread (() => {
				var other = (NSWindow)notification.Object;

				if (notification.Object == Window) {
					if (LostFocus != null)
						LostFocus (this, null);
				}
			}));
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidResizeNotification, notification => Runtime.RunInMainThread (() => {
				var other = (NSWindow)notification.Object;
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

			return kargs.Handled;
		}

		public override bool PerformKeyEquivalent (NSEvent theEvent)
		{
			var popupHandled = SendKeyPressed (theEvent.ToXwtKeyEventArgs ());
			if (popupHandled)
				return true;
			return base.PerformKeyEquivalent (theEvent);;
		}

		bool ignoreEndEditing = false;
		public override void DidEndEditing (NSNotification notification)
		{
			base.DidEndEditing (notification);

			if (ignoreEndEditing) {
				ignoreEndEditing = false;
				return;
			}

			PlaceholderAttributedString = MakePlaceholderString (PlaceholderText);

			nint value = ((NSNumber)notification.UserInfo.ValueForKey ((NSString)"NSTextMovement")).LongValue;
			if (value == (nint)(long)NSTextMovement.Tab) {
				Window.MakeFirstResponder(null);
				LostFocus?.Invoke(this, EventArgs.Empty);
				return;
			}

			if (value == (nint)(long)NSTextMovement.Return) {
				if (SelectionActivated != null)
					SelectionActivated (this, null);
				return;
			}

			// This means we've reached a focus loss event.
			var replacedWith = notification.UserInfo.ValueForKey ((NSString)"_NSFirstResponderReplacingFieldEditor");
			if (replacedWith != this && LostFocus != null) {
				LostFocus (this, null);
			}
		}

		public override void ViewDidMoveToWindow ()
		{
			base.ViewDidMoveToWindow ();

			// Needs to be grabbed after it's parented.
			gtkWidget = Components.Mac.GtkMacInterop.NSViewToGtkWidget (this);
		}

		public override bool BecomeFirstResponder ()
		{
			bool firstResponder = base.BecomeFirstResponder ();
			if (firstResponder) {
				ignoreEndEditing = true;
				PlaceholderAttributedString = MakePlaceholderString (FirstResponderPlaceholder);
				ignoreEndEditing = false;

				GainedFocus?.Invoke (this, EventArgs.Empty);
			}

			return firstResponder;
		}

		public void Focus ()
		{
			Window.MakeFirstResponder (this);
		}
	}
}

