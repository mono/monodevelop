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
using MonoDevelop.Ide;
using MonoDevelop.Components;
using Xwt.Mac;
using CoreImage;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	[Register]
	class RunButton : NSButton
	{
		NSImage stopIcon, continueIcon, buildIcon;
		NSImage stopIconDisabled, continueIconDisabled, buildIconDisabled;


		public RunButton ()
		{
			stopIcon = ImageService.GetIcon ("stop").ToNSImage ();
			continueIcon = ImageService.GetIcon ("continue").ToNSImage ();
			buildIcon = ImageService.GetIcon ("build").ToNSImage ();
			stopIconDisabled = ImageService.GetIcon ("stop").WithStyles("disabled").ToNSImage ();
			continueIconDisabled = ImageService.GetIcon ("continue").WithStyles("disabled").ToNSImage ();
			buildIconDisabled = ImageService.GetIcon ("build").WithStyles("disabled").ToNSImage ();

			Ide.Gui.Styles.Changed +=  (o, e) => UpdateCell ();

			Cell = new ColoredButtonCell ();

			icon = OperationIcon.Run;
			ImagePosition = NSCellImagePosition.ImageOnly;
			BezelStyle = NSBezelStyle.TexturedRounded;

			Enabled = false;
			Cell.ImageDimsWhenDisabled = false;
		}

		void UpdateCell ()
		{
			Appearance = NSAppearance.GetAppearance (IdeApp.Preferences.UserInterfaceSkin == Skin.Dark ? NSAppearance.NameVibrantDark : NSAppearance.NameAqua);
			NeedsDisplay = true;
		}

		NSImage GetIcon ()
		{
			switch (icon) {
			case OperationIcon.Stop:
				return Enabled ? stopIcon : stopIconDisabled;
			case OperationIcon.Run:
				return Enabled ? continueIcon : continueIconDisabled;
			case OperationIcon.Build:
				return Enabled ? buildIcon : buildIconDisabled;
			}
			throw new InvalidOperationException ();
		}

		public override bool Enabled {
			get {
				return base.Enabled;
			}
			set {
				base.Enabled = value;
				Image = GetIcon ();
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
			}
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
			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Dark) {
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
				NSColor.FromRgba (0.56f, 0.56f, 0.56f, 1f).SetStroke ();
				path.Stroke ();
			} else {
				base.DrawBezelWithFrame (frame, controlView);
			}
		}
	}
}

