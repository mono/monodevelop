//
// AwesomeBar.cs
//
// Author:
//       iain <iain@xamarin.com>
//
// Copyright (c) 2015 Copyright © 2015 Xamarin, Inc
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

using AppKit;
using CoreGraphics;
using Foundation;
using MonoDevelop.Core;

using Xwt.Mac;
using MonoDevelop.Ide;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	public class AwesomeBar : NSView, INSTouchBarDelegate
	{
		//Begin variables declared as static outside of scope to prevent garbage collection crash
		private static NSSegmentedControl navSegments = null;
		private static NSSegmentedControl tabNavSegments = null;
		//End variables decl… *sigh*

		internal RunButton RunButton { get; set; }
		internal SelectorView SelectorView { get; set; }
		internal StatusBar StatusBar { get; set; }
		internal SearchBar SearchBar { get; set; }
		internal ButtonBarContainer ButtonBarContainer { get; private set; }

		public AwesomeBar ()
		{
			RunButton = new RunButton ();
			AddSubview (RunButton);

			SelectorView = new SelectorView ();
			SelectorView.SizeChanged += (object sender, EventArgs e) => UpdateLayout ();
			AddSubview (SelectorView);

			ButtonBarContainer = new ButtonBarContainer ();
			ButtonBarContainer.SizeChanged += (object sender, EventArgs e) => UpdateLayout ();
			AddSubview (ButtonBarContainer);

			StatusBar = new StatusBar ();
			AddSubview (StatusBar);

			SearchBar = new SearchBar ();
			AddSubview (SearchBar);

			Ide.Gui.Styles.Changed += (o, e) => UpdateLayout ();

			UpdateTouchBar ();
		}

		NSTouchBar MakeTouchBar ()
		{
			var aTouchbar = new NSTouchBar ();
			aTouchbar.Delegate = this;
			aTouchbar.DefaultItemIdentifiers = GetItemIdentifiers ();
			return aTouchbar;
		}

		public void UpdateTouchBar ()
		{
			NSApplication.SharedApplication.SetTouchBar (MakeTouchBar ());
		}

		string [] GetItemIdentifiers ()
		{
			List<string> ids = new List<string> ();
			if (RunButton.Enabled) {
				switch (RunButton.Icon) {
				case Components.MainToolbar.OperationIcon.Build:
					ids.Add ("build");
					break;

				case Components.MainToolbar.OperationIcon.Run:
					ids.Add ("continue");
					break;

				case Components.MainToolbar.OperationIcon.Stop:
					ids.Add ("stop");
					break;
				}
			}

			ids.Add ("navigation");
			ids.Add (NSTouchBarItemIdentifier.FixedSpaceSmall.ToString());
			ids.Add ("tabNavigation");
			
			if (ButtonBarContainer != null) {
				var extraIds = ButtonBarContainer.GetButtonBarTouchBarItems ();
				if (extraIds != null) {
					ids.AddRange (extraIds);
				}
			}

			return ids.ToArray ();
		}

		[Export ("touchBar:makeItemForIdentifier:")]
		public NSTouchBarItem MakeItem (NSTouchBar touchbar, string identifier)
		{
			NSTouchBarItem item = null;

			if (identifier.StartsWith (ButtonBarContainer.ButtonBarIdPrefix)) {
				Console.WriteLine ($"Getting items for {identifier}");

				var items = ButtonBarContainer.TouchBarItemsForIdentifier (identifier);
				if (items == null) {
					return null;
				}

				item = NSGroupTouchBarItem.CreateGroupItem (identifier, items);
				return item;
			}

			switch (identifier) {
			case "navigation": //contains navigate back & forward buttons

				//NSSegmentedControl navSegments = null; //declared as static above due to GC bug

				Action navSegmentsAction = () => {

					if (navSegments != null) {
						if (navSegments.SelectedSegment == 0) {
							IdeApp.CommandService.DispatchCommand ("MonoDevelop.Ide.Commands.NavigationCommands.NavigateBack");
						} else if (navSegments.SelectedSegment == 1) {
							IdeApp.CommandService.DispatchCommand ("MonoDevelop.Ide.Commands.NavigationCommands.NavigateForward");
						}
					}
				};

				NSImage [] navIcons =
					{
						NSImage.ImageNamed (NSImageName.TouchBarGoBackTemplate),
						NSImage.ImageNamed (NSImageName.TouchBarGoForwardTemplate)
					};


				navSegments = NSSegmentedControl.FromImages (navIcons, NSSegmentSwitchTracking.Momentary, navSegmentsAction);
				navSegments.SegmentStyle = NSSegmentStyle.Separated;

				var customItemNavSegments = new NSCustomTouchBarItem ("navigation");
				customItemNavSegments.View = navSegments;
				item = customItemNavSegments;

				return item;
				
			case "tabNavigation": //navigation again, but this time for tabs. Lack of images is a work-in-progress

				//NSSegmentedControl tabNavSegments = null; //declared as static above due to GC bug

				Action tabNavSegmentsAction = () => {
					
					if (tabNavSegments != null) {
						
						if (tabNavSegments.SelectedSegment == 0) {
							IdeApp.CommandService.DispatchCommand (MonoDevelop.Ide.Commands.WindowCommands.PrevDocument);
						} 
						else if (tabNavSegments.SelectedSegment == 1) {
							IdeApp.CommandService.DispatchCommand (MonoDevelop.Ide.Commands.WindowCommands.NextDocument);
						}
					}

				};

				tabNavSegments = NSSegmentedControl.FromLabels (new string [] { "<-tab", "tab->" }, NSSegmentSwitchTracking.Momentary, tabNavSegmentsAction);
				tabNavSegments.SegmentStyle = NSSegmentStyle.Separated;

				var customItemTabNavSegments = new NSCustomTouchBarItem ("tabNavigation");
				customItemTabNavSegments.View = tabNavSegments;
				item = customItemTabNavSegments;

				return item;

			case "continue":
			case "stop":
			case "build":
				var customItem = new NSCustomTouchBarItem (identifier);

#if WANT_TO_SEE_BIG_CRASH
				var button = NSButton.CreateButton ("", MultiResImage.CreateMultiResImage (identifier, ""), () => { RunButton.PerformClick (RunButton); });
#else
				var button = NSButton.CreateButton ("", MultiResImage.CreateMultiResImage (identifier, ""), () => { });
				button.Activated += (sender, e) => {
					RunButton.PerformClick (RunButton);
				};
#endif
				customItem.View = button;
				string label = string.Empty;
				switch (identifier) {
				case "continue":
					label = GettextCatalog.GetString ("Run");
					break;

				case "stop":
					label = GettextCatalog.GetString ("Stop");
					break;

				case "build":
					label = GettextCatalog.GetString ("Build");
					break;
				}
				customItem.CustomizationLabel = label;

				item = customItem;
				break;
			}

			return item;
		}

		const float toolbarPadding = 8.0f;
		const float maxSearchBarWidth = 270.0f;
		const float minSearchBarWidth = 150.0f;
		const float maxStatusBarWidth = 700.0f;
		const float minStatusBarWidth = 220.0f;
		const float runButtonWidth = 38.0f;
		public static float ToolbarWidgetHeight {
			get {
				return MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan ? 24.0f : 22.0f;
			}
		}

		public override void ViewDidMoveToWindow ()
		{
			base.ViewDidMoveToWindow ();

			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light) {
				return;
			}

			// I'm sorry. I'm so so sorry.
			// When the user has Graphite appearance set in System Preferences on El Capitan
			// and they enter fullscreen mode, Cocoa doesn't respect the VibrantDark appearance
			// making the toolbar background white instead of black, however the toolbar items do still respect
			// the dark appearance, making them white on white.
			//
			// So, an absolute hack is to go through the toolbar hierarchy and make all the views background colours
			// be the dark grey we wanted them to be in the first place.
			//
			// https://bugzilla.xamarin.com/show_bug.cgi?id=40160
			//
			if (Window == null || Window == MacInterop.GtkQuartz.GetWindow (IdeApp.Workbench.RootWindow)) {
				if (Superview != null) {
					Superview.WantsLayer = false;

					if (Superview.Superview != null) {
						Superview.Superview.WantsLayer = false;
					}
				}
				return;
			}

			var bgColor = Styles.DarkToolbarBackgroundColor.ToNSColor ().CGColor;

			// NSToolbarItemViewer
			if (Superview != null) {
				Superview.WantsLayer = true;
				Superview.Layer.BackgroundColor = bgColor;

				if (Superview.Superview != null) {
					// _NSToolbarViewClipView
					Superview.Superview.WantsLayer = true;
					Superview.Superview.Layer.BackgroundColor = bgColor;

					if (Superview.Superview.Superview != null && Superview.Superview.Superview.Superview != null) {
						// NSTitlebarView
						Superview.Superview.Superview.Superview.WantsLayer = true;
						Superview.Superview.Superview.Superview.Layer.BackgroundColor = bgColor;
					}
				}
			}
		}

		NSObject superviewFrameChangeObserver;
		public override void ViewWillMoveToSuperview (NSView newSuperview)
		{
			if (Superview != null && superviewFrameChangeObserver != null) {
				NSNotificationCenter.DefaultCenter.RemoveObserver (superviewFrameChangeObserver);
				superviewFrameChangeObserver = null;

				Superview.PostsFrameChangedNotifications = false;
			}

			base.ViewWillMoveToSuperview (newSuperview);
		}

		public override void ViewDidMoveToSuperview ()
		{
			base.ViewDidMoveToSuperview ();

			if (Superview != null) {
				Superview.PostsFrameChangedNotifications = true;
				superviewFrameChangeObserver = NSNotificationCenter.DefaultCenter.AddObserver (NSView.FrameChangedNotification, (note) => {
					// Centre vertically in superview frame
					Frame = new CGRect (0, Superview.Frame.Y + (Superview.Frame.Height - ToolbarWidgetHeight) / 2, Superview.Frame.Width, ToolbarWidgetHeight);
				}, Superview);
			}
		}

		void UpdateLayout ()
		{
			RunButton.Frame = new CGRect (toolbarPadding, 0, runButtonWidth, ToolbarWidgetHeight);
			var statusbarWidth = Math.Max (Math.Min (Math.Round ( Frame.Width * 0.3), maxStatusBarWidth), minStatusBarWidth);
			var searchbarWidth = maxSearchBarWidth;
			if (statusbarWidth < searchbarWidth) {
				searchbarWidth = minSearchBarWidth;
			}

			// We only need to work out the width on the left side of the window because the statusbar is centred
			// Gap + RunButton.Width + Gap + ButtonBar.Width + Gap + Half of StatusBar.Width
			var spaceLeft = (Frame.Width / 2) - (toolbarPadding + runButtonWidth + toolbarPadding + ButtonBarContainer.Frame.Width + toolbarPadding + (statusbarWidth / 2));

			StatusBar.Frame = new CGRect (Math.Round((Frame.Width - statusbarWidth) / 2), 0, statusbarWidth - 2, ToolbarWidgetHeight);

			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Dark) {
				SearchBar.Frame = new CGRect (Frame.Width - searchbarWidth, 0, searchbarWidth, ToolbarWidgetHeight);
			} else {
				nfloat elcapYOffset = 0;
				nfloat elcapHOffset = 0;

				if (MacSystemInformation.OsVersion >= MacSystemInformation.ElCapitan) {
					nfloat scaleFactor = 1;

					if (Window != null && Window.Screen != null) {
						scaleFactor = Window.Screen.BackingScaleFactor;
					}
					elcapYOffset = scaleFactor == 2 ? -0.5f : -1;
					elcapHOffset = 1.0f;
				}
				SearchBar.Frame = new CGRect (Frame.Width - searchbarWidth, 0 + elcapYOffset, searchbarWidth, ToolbarWidgetHeight + elcapHOffset);
			}

			var selectorSize = SelectorView.SizeThatFits (new CGSize (spaceLeft, ToolbarWidgetHeight));

			SelectorView.Frame = new CGRect (toolbarPadding + runButtonWidth + toolbarPadding, 0, Math.Round (selectorSize.Width), ToolbarWidgetHeight);
			ButtonBarContainer.SetFrameOrigin (new CGPoint(SelectorView.Frame.GetMaxX () + toolbarPadding, -2));

			// Finally check if the StatusBar overlaps the ButtonBarContainer (and its padding) and adjust is accordingly
			if (StatusBar.Frame.IntersectsWith (ButtonBarContainer.Frame.Inset (-toolbarPadding, 0))) {
				StatusBar.SetFrameOrigin (new CGPoint (ButtonBarContainer.Frame.GetMaxX () + toolbarPadding, StatusBar.Frame.Y));
			}
		}

		public override void MouseDown (NSEvent theEvent)
		{
			base.MouseDown (theEvent);

			var locationInSV = Superview.ConvertPointFromView (theEvent.LocationInWindow, null);
			if (theEvent.ClickCount == 2 && HitTest (locationInSV) == this) {
				bool miniaturise = false;

				if (MacSystemInformation.OsVersion < MacSystemInformation.ElCapitan) {
					miniaturise = NSUserDefaults.StandardUserDefaults.BoolForKey ("AppleMiniaturizeOnDoubleClick");
				} else {
					var action = NSUserDefaults.StandardUserDefaults.StringForKey ("AppleActionOnDoubleClick");
					if (action == "None") {
						return;
					} else if (action == "Minimize") {
						miniaturise = true;
					}
				}

				if (miniaturise) {
					Window.Miniaturize (this);
				} else {
					Window.Zoom (this);
				}
			}
		}

		public override CGRect Frame {
			get {
				return base.Frame;
			}
			set {
				base.Frame = value;
				UpdateLayout ();
			}
		}
    }
}

