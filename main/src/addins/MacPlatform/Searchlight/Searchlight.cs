// OverlaySearchbar.cs
//
//
// Author:
//       iain <iain@xamarin.com>
//
// Copyright (c) Xamarin, Inc 2016 
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
using CoreGraphics;
using Foundation;
using MonoDevelop.Ide;

namespace MonoDevelop.MacIntegration.OverlaySearch
{
	public class Searchlight : NSPanel, INSTextFieldDelegate
	{
		public event EventHandler<EventArgs> WindowWasDismissed;
		public event EventHandler<EventArgs> SearchRequested;
		KeyHandlingTextField searchbar;
		NSStackView contentStack;
		NSScrollView scrollView;
		NSVisualEffectView visualEffect;

		NSObject globalMonitor;

		class MagicScrollView : NSScrollView
		{
			class FlippedClipView : NSClipView
			{
				public override bool IsFlipped {
					get {
						return true;
					}
				}
			}

			public MagicScrollView ()
			{
				// Need to use a flipped clipview otherwise the scrollview aligns everything to the bottom
				ContentView = new FlippedClipView ();
				SetContentHuggingPriorityForOrientation ((float)NSLayoutPriority.WindowSizeStayPut + 1, NSLayoutConstraintOrientation.Vertical);
			}

			public override CGSize IntrinsicContentSize {
				get {
					var docView = DocumentView as NSView;
					nfloat height;

					if (docView == null || docView.Frame.Height > 500f) {
						height = 500.0f;
					} else {
						height = docView.Frame.Height;
					}

					return new CGSize (NoIntrinsicMetric, height);
				}
			}

			public override NSObject DocumentView {
				get {
					return base.DocumentView;
				}
				set {
					// Disable old notifications?

					base.DocumentView = value;

					var view = value as NSView;
					view.PostsFrameChangedNotifications = true;

					NSNotificationCenter.DefaultCenter.AddObserver (FrameChangedNotification,
																	(note) => {
																		InvalidateIntrinsicContentSize ();
																	}, value);
				}
			}
		}

		internal class KeyHandlingTextField : NSTextField
		{
			public event EventHandler<MoveResultArgs> MoveResult;
			public override bool PerformKeyEquivalent (NSEvent theEvent)
			{
				MoveResultArgs args = null;

				//switch ((byte)theEvent.CharactersIgnoringModifiers[0]) {
				switch ((NSKey)theEvent.KeyCode) {
				case NSKey.UpArrow: // 0
					if ((theEvent.ModifierFlags & NSEventModifierMask.CommandKeyMask) == NSEventModifierMask.CommandKeyMask) {
						args = new MoveResultArgs (MoveResultArgs.MoveType.FirstCategory);
					} else {
						args = new MoveResultArgs (MoveResultArgs.MoveType.PreviousResult);
					}
					break;
				case NSKey.DownArrow: // 1
					if ((theEvent.ModifierFlags & NSEventModifierMask.CommandKeyMask) == NSEventModifierMask.CommandKeyMask) {
						args = new MoveResultArgs (MoveResultArgs.MoveType.LastCategory);
					} else {
						args = new MoveResultArgs (MoveResultArgs.MoveType.NextResult);
					}
					break;
				case NSKey.PageUp: // 44
					args = new MoveResultArgs (MoveResultArgs.MoveType.PreviousCategory);
					break;
				case NSKey.PageDown: //45
					args = new MoveResultArgs (MoveResultArgs.MoveType.NextCategory);
					break;
				case NSKey.Return:
					args = new MoveResultArgs (MoveResultArgs.MoveType.ActivateResult);
					break;
				case NSKey.Escape:
					args = new MoveResultArgs (MoveResultArgs.MoveType.Cancel);
					break;
				}

				if (args != null) {
					MoveResult?.Invoke (this, args);

					// Return true because Cocoa will send the event twice otherwise
					// for reasons...
					return true;
				}

				return base.PerformKeyEquivalent (theEvent);
			}

			public class MoveResultArgs : EventArgs
			{
				public enum MoveType
				{
					PreviousResult,
					NextResult,
					PreviousCategory,
					NextCategory,
					FirstCategory,
					LastCategory,
					ActivateResult,
					Cancel,
				};

				public MoveType Type { get; private set; }
				public MoveResultArgs (MoveType type)
				{
					Type = type;
				}
			}
		}

		public Searchlight ()
		{
			StyleMask = NSWindowStyle.Hud;
			ReleasedWhenClosed = true;
			NSString appearanceName = IdeApp.Preferences.UserInterfaceTheme == Theme.Dark ? NSAppearance.NameVibrantDark : NSAppearance.NameVibrantLight;
			Appearance = NSAppearance.GetAppearance (appearanceName);
			IsMovable = false;

			visualEffect = new NSVisualEffectView ();
			visualEffect.TranslatesAutoresizingMaskIntoConstraints = false;
			ContentView.AddSubview (visualEffect);

			contentStack = new NSStackView ();
			contentStack.Orientation = NSUserInterfaceLayoutOrientation.Vertical;
			contentStack.TranslatesAutoresizingMaskIntoConstraints = false;
			contentStack.SetHuggingPriority (1000, NSLayoutConstraintOrientation.Vertical);

			visualEffect.AddSubview (contentStack);

			searchbar = new KeyHandlingTextField ();
			searchbar.DrawsBackground = false;
			searchbar.Bordered = false;
			searchbar.Bezeled = false;
			searchbar.Font = NSFont.SystemFontOfSize (48.0f);
			searchbar.TranslatesAutoresizingMaskIntoConstraints = false;
			searchbar.PlaceholderString = "Search";
			searchbar.Editable = true;
			searchbar.Delegate = this;

			searchbar.Cell.FocusRingType = NSFocusRingType.None;

			InitialFirstResponder = searchbar;

			searchbar.MoveResult += HandleMoveResult;

			contentStack.AddView (searchbar, NSStackViewGravity.Leading);

			scrollView = new MagicScrollView ();

			scrollView.DrawsBackground = false;

			// Need to explicitly set the scroller style otherwise Cocoa won't scroll correctly
			// for some reason
			scrollView.ScrollerStyle = NSScrollerStyle.Overlay;
			scrollView.ScrollerKnobStyle = NSScrollerKnobStyle.Dark;
			scrollView.HasVerticalScroller = true;
			scrollView.Identifier = "resultsScroller";

			contentStack.AddView (scrollView, NSStackViewGravity.Leading);

			resultsDisplay = new SearchlightResultsDisplay ();
			resultsDisplay.Destroyed += (object o, EventArgs args) => {
				CloseWindow ();
			};

			scrollView.DocumentView = resultsDisplay;

			var viewsDict = new NSDictionary ("results", resultsDisplay,
											  "effect", visualEffect,
											  "contentStack", contentStack);

			// Pin horizontally so no scrolling.
			var constraints = NSLayoutConstraint.FromVisualFormat ("|[results]|", NSLayoutFormatOptions.None,
																   null, viewsDict);
			scrollView.ContentView.AddConstraints (constraints);

			// Pin the top only so we can scroll
			constraints = NSLayoutConstraint.FromVisualFormat ("V:|[results]", NSLayoutFormatOptions.None,
															   null, viewsDict);
			scrollView.ContentView.AddConstraints (constraints);


			constraints = NSLayoutConstraint.FromVisualFormat ("|[effect]|", NSLayoutFormatOptions.None,
															   null, viewsDict);
			ContentView.AddConstraints (constraints);

			constraints = NSLayoutConstraint.FromVisualFormat ("V:|[effect]|", NSLayoutFormatOptions.None,
																  null, viewsDict);
			ContentView.AddConstraints (constraints);

			constraints = NSLayoutConstraint.FromVisualFormat ("|-10-[contentStack]-10-|", NSLayoutFormatOptions.None,
															   null, viewsDict);
			visualEffect.AddConstraints (constraints);

			constraints = NSLayoutConstraint.FromVisualFormat ("V:|-10-[contentStack]-10-|",
			                                                   NSLayoutFormatOptions.None,
															   null, viewsDict);
			visualEffect.AddConstraints (constraints);

			// Listen to global events to know when to close the window by the users clicking outside of the window
			globalMonitor = NSEvent.AddGlobalMonitorForEventsMatchingMask (NSEventMask.LeftMouseDown |
																		   NSEventMask.RightMouseDown |
																		   NSEventMask.OtherMouseDown,
																		   CloseWindowWithEvent);
		}

		void CloseWindowWithEvent (NSEvent ev)
		{
			if (ev.Window != this) {
				CloseWindow ();
			}
		}

		public void CloseWindow ()
		{
			if (globalMonitor != null) {
				NSEvent.RemoveMonitor (globalMonitor);
				globalMonitor = null;
			} else {
				return;
			}

			WindowWasDismissed?.Invoke (this, EventArgs.Empty);
		}

		// We need to override CanBecomeKeyWindow and CanBecomeMainWindow because
		// HUD style panels do not handle key presses by default
		public override bool CanBecomeKeyWindow {
			get {
				return true;
			}
		}

		public override bool CanBecomeMainWindow {
			get {
				return true;
			}
		}

		// Listen for ResignKeyWindow to know when the user has clicked inside the window
		// AddLocalMonitorForEventsMatchingMask doesn't seem to work for this, it might be
		// a Gtk/Cocoa interaction problem?
		// 
		// Issue: Clicking outside a window sends an event to the Global listener and triggers a
		// ResignKeyWindow event to be raised. One might think that therefore we don't need the 
		// Global listener and just use ResignKeyWindow instead, but for some reason only using
		// ResignKeyWindow makes the main app window minimize when you click outside of the window.
		// Yeah... dunno.
		//
		// So anyway, we use both a Global event listener and ResignKeyWindow and ignore the second
		// call to CloseWindow.
		public override void ResignKeyWindow ()
		{
			CloseWindow ();

			base.ResignKeyWindow ();
		}

		[Export ("controlTextDidChange:")]
		public void Changed (NSNotification notification)
		{
			SearchRequested?.Invoke (this, EventArgs.Empty);
		}

		public string SearchString {
			get {
				return searchbar.StringValue;
			}
		}

		SearchlightResultsDisplay resultsDisplay;
		public SearchlightResultsDisplay ResultsDisplay {
			get {
				return resultsDisplay;
			}
		}

		void HandleMoveResult (object sender, KeyHandlingTextField.MoveResultArgs args)
		{
			if (args.Type == KeyHandlingTextField.MoveResultArgs.MoveType.Cancel) {
				// First Esc press clears search, second closes
				if (string.IsNullOrEmpty (searchbar.StringValue)) {
					CloseWindow ();
				} else {
					searchbar.StringValue = "";

					// Setting the StringValue programmatically doesn't trigger the
					// Changed notification to be emitted.
					Changed (null);
				}
				return;
			}
			resultsDisplay.HandleKeyPress (args.Type);
		}
	}
}
