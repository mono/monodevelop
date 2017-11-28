//
// RunButton.cs
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
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Xwt.Mac;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	[Register ("RunButton")]
	class RunButton : NSFocusButton, INSAccessibilityButton, INSAccessibility
	{
		NSImage stopIcon, continueIcon, buildIcon;

		public RunButton ()
		{
			UpdateIcons ();

			Ide.Gui.Styles.Changed +=  (o, e) => UpdateCell ();

			Cell = new ColoredButtonCell ();

			icon = OperationIcon.Run;
			ImagePosition = NSCellImagePosition.ImageOnly;
			BezelStyle = NSBezelStyle.TexturedRounded;

			Enabled = false;
			UpdateAccessibilityValues ();
		}

		void UpdateIcons (object sender = null, EventArgs e = null)
		{
			stopIcon = MultiResImage.CreateMultiResImage ("stop", "");
			continueIcon = MultiResImage.CreateMultiResImage ("continue", "");
			buildIcon = MultiResImage.CreateMultiResImage ("build", "");

			// We can use Template images supported by NSButton, thus no reloading
			// on theme change is required.
			stopIcon.Template = continueIcon.Template = buildIcon.Template = true;
		}

		void UpdateCell ()
		{
			Appearance = NSAppearance.GetAppearance (IdeApp.Preferences.UserInterfaceTheme == Theme.Dark ? NSAppearance.NameVibrantDark : NSAppearance.NameAqua);
			NeedsDisplay = true;
		}

		NSImage GetIcon ()
		{
			switch (icon) {
			case OperationIcon.Stop:
				return stopIcon;
			case OperationIcon.Run:
				return continueIcon;
			case OperationIcon.Build:
				return buildIcon;
			}
			throw new InvalidOperationException ();
		}

		void GetTitleAndHelpForIcon (out string title, out string help)
		{
			title = "";
			help = "";

			switch (icon) {
			case OperationIcon.Stop:
				title = GettextCatalog.GetString ("Stop");
				help = GettextCatalog.GetString ("Stop the executing solution");
				break;
			case OperationIcon.Run:
				title = GettextCatalog.GetString ("Run");
				help = GettextCatalog.GetString ("Build and run the current solution");
				break;
			case OperationIcon.Build:
				title = GettextCatalog.GetString ("Build");
				help = GettextCatalog.GetString ("Build the current solution");
				break;
			}
		}

		public override bool Enabled {
			get {
				return base.Enabled;
			}
			set {
				base.Enabled = value;
				Image = GetIcon ();

				UpdateAccessibilityValues ();
			}
		}

		OperationIcon icon;
		public OperationIcon Icon {
			get { return icon; }
			set {
				if (value == icon)
					return;
				icon = value;
				Image = GetIcon ();

				UpdateAccessibilityValues ();
			}
		}

		void UpdateAccessibilityValues ()
		{
			var nsa = (INSAccessibility) this;
			nsa.AccessibilityIdentifier = "MainToolbar.RunButton";

			string help, title;
			GetTitleAndHelpForIcon (out title, out help);

			AccessibilityHelp = help;
			AccessibilityTitle = title;
			AccessibilityEnabled = Enabled;
			AccessibilitySubrole = NSAccessibilitySubroles.ToolbarButtonSubrole;

			// FIXME: Setting this doesn't appear to change anything.
			// Nor does overriding the INSAccessibilityButton.AccessibilityLabel getter
			nsa.AccessibilityLabel = title;
		}

		// This method override is required so that Cocoa will pick up that our button subclass
		// has a PerformPress action.
		public override bool AccessibilityPerformPress ()
		{
			return base.AccessibilityPerformPress ();
		}

		public override CGSize IntrinsicContentSize {
			get {
				return new CGSize (38, 25);
			}
		}
	}

	class ColoredButtonCell : NSButtonCell
	{
		public override void DrawBezelWithFrame (CGRect frame, NSView controlView)
		{
			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Dark) {
				var inset = frame.Inset (0.25f, 0.25f);

				var path = NSBezierPath.FromRoundedRect (inset, 3, 3);
				path.LineWidth = 0.5f;

				// The first time the view is drawn it has a filter of some sort attached so that the colours set here
				// are made lighter onscreen.
				// NSColor.FromRgba (0.244f, 0.247f, 0.245f, 1).SetStroke ();
				// would make the initial colour actually be .56,.56,.56
				//
				// However after switching theme this filter is removed and the colour set here is the actual colour
				// displayed onscreen.

				// This also seems to happen in fullscreen mode and always on High Sierra
				if (MainToolbar.IsFullscreen || MacSystemInformation.OsVersion >= MacSystemInformation.HighSierra) {
					Styles.DarkBorderColor.ToNSColor ().SetStroke ();
				} else {
					Styles.DarkBorderBrokenColor.ToNSColor ().SetStroke ();
				}

				path.Stroke ();
			} else {
				if (controlView.Window?.Screen?.BackingScaleFactor == 2) {
					frame = new CGRect (frame.X, frame.Y + 0.5f, frame.Width, frame.Height);
				}
				base.DrawBezelWithFrame (frame, controlView);
			}
		}

		public override void DrawInteriorWithFrame (CGRect cellFrame, NSView inView)
		{
			cellFrame = new CGRect (cellFrame.X, cellFrame.Y + 0.5f, cellFrame.Width, cellFrame.Height);

			var old = Enabled;

			// In fullscreen mode with dark theme on El Capitan, the disabled icon picked is for the
			// normal appearance so it is too dark. Hack this so it comes up lighter.
			// This also happens in all modes on High Sierra.
			// For further information see the comment in AwesomeBar.cs
			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Dark && (MainToolbar.IsFullscreen || MacSystemInformation.OsVersion >= MacSystemInformation.HighSierra)) {
				Enabled = true;
			}
			base.DrawInteriorWithFrame (cellFrame, inView);
			Enabled = old;
		}
	}
}

