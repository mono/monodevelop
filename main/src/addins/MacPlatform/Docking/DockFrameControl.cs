//
// DockFrameControl.cs
//
// Author:
//       iain <>
//
// Copyright (c) 2017 
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

using MonoDevelop.Components.Mac;
using MonoDevelop.Components.Docking;

using AppKit;
using Foundation;

using Gdk;
using Gtk;
#if false
namespace MonoDevelop.MacIntegration.Docking
{
	internal class DockFrameControl : NSView, IDockFrameControl
	{
		DockFrame parentFrame;

		DockBar dockbarTop, dockbarBottom, dockbarLeft, dockbarRight;
		//GtkEmbed dockbarTopEmbed, dockbarBottomEmbed, dockbarLeftEmbed, dockbarRightEmbed;

		DockContainer container;
		//GtkEmbed containerEmbed;

		public DockFrameControl (DockFrame frame)
		{
			parentFrame = frame;

			var button = new NSButton (new CoreGraphics.CGRect (100, 100, 100, 100));
			button.Title = "Add UI here";
			AddSubview (button);

			container = null;
			/*
			dockbarTop = new DockBar (this, PositionType.Top);
			dockbarTopEmbed = new GtkEmbed (dockbarTop);
			dockbarTopEmbed.TranslatesAutoresizingMaskIntoConstraints = false;

			dockbarBottom = new DockBar (this, PositionType.Bottom);
			dockbarBottomEmbed = new GtkEmbed (dockbarBottom);
			dockbarBottomEmbed.TranslatesAutoresizingMaskIntoConstraints = false;

			dockbarLeft = new DockBar (this, PositionType.Left);
			dockbarLeftEmbed = new GtkEmbed (dockbarLeft);
			dockbarLeftEmbed.TranslatesAutoresizingMaskIntoConstraints = false;

			dockbarRight = new DockBar (this, PositionType.Right);
			dockbarRightEmbed = new GtkEmbed (dockbarRight);
			dockbarRightEmbed.TranslatesAutoresizingMaskIntoConstraints = false;

			container = new DockContainer (frame);
			containerEmbed = new GtkEmbed (container);
			containerEmbed.TranslatesAutoresizingMaskIntoConstraints = false;

			var innerView = new NSView ();
			innerView.TranslatesAutoresizingMaskIntoConstraints = false;

			innerView.AddSubview (dockbarLeftEmbed);
			innerView.AddSubview (containerEmbed);
			innerView.AddSubview (dockbarRightEmbed);

			AddSubview (dockbarTopEmbed);
			AddSubview (innerView);
			AddSubview (dockbarBottomEmbed);

			var viewsDict = new NSDictionary ("left", dockbarLeftEmbed,
											  "container", containerEmbed,
											  "right", dockbarRightEmbed,
											  "top", dockbarTopEmbed,
											  "bottom", dockbarBottomEmbed,
											  "inner", innerView);

			var constraints = NSLayoutConstraint.FromVisualFormat ("|[left][container][right]|",
																   NSLayoutFormatOptions.AlignAllCenterY | NSLayoutFormatOptions.AlignAllTop | NSLayoutFormatOptions.AlignAllBottom,
																   null, viewsDict);
			innerView.AddConstraints (constraints);
			constraints = NSLayoutConstraint.FromVisualFormat ("V:|[container]|", NSLayoutFormatOptions.None,
															   null, viewsDict);
			innerView.AddConstraints (constraints);

			constraints = NSLayoutConstraint.FromVisualFormat ("V:|[top][inner][bottom]|",
															   NSLayoutFormatOptions.AlignAllCenterX | NSLayoutFormatOptions.AlignAllLeading | NSLayoutFormatOptions.AlignAllTrailing,
															   null, viewsDict);
			AddConstraints (constraints);

			constraints = NSLayoutConstraint.FromVisualFormat ("|[inner]|", NSLayoutFormatOptions.None,
															   null, viewsDict);
			AddConstraints (constraints);
			*/
		}

		public bool DockbarsVisible => true;

		public bool UseWindowsForTopLevelFrames => true;

		public DockContainer Container => container;

		public bool OverlayWidgetVisible => false;

		public DockBar DockBarTop { get => dockbarTop; set => dockbarTop = value; }
		public DockBar DockBarBottom { get => dockbarBottom; set => dockbarBottom = value; }
		public DockBar DockBarLeft { get => dockbarLeft; set => dockbarLeft = value; }
		public DockBar DockBarRight { get => dockbarRight; set => dockbarRight = value; }

		DockFrame IDockFrameControl.Frame => parentFrame;

		public void AddOverlayWidget (Widget widget, bool animate = false)
		{
			//throw new NotImplementedException ();
		}

		public void AddTopLevel (DockFrameTopLevel w, int x, int y, int width, int height)
		{
			//throw new NotImplementedException ();
		}

		public void AutoHide (DockItem item, AutoHideBox widget, bool animate)
		{
			//throw new NotImplementedException ();
		}

		public AutoHideBox AutoShow (DockItem item, DockBar bar, int size)
		{
			throw new NotImplementedException ();
		}

		public void BatchBegin ()
		{
		}

		public void BatchCommit ()
		{
		}

		public Rectangle GetCoordinates (Widget w)
		{
			throw new NotImplementedException ();
		}

		public void RemoveOverlayWidget (bool animate = false)
		{
			//throw new NotImplementedException ();
		}

		public void RemoveTopLevel (DockFrameTopLevel w)
		{
			//throw new NotImplementedException ();
		}

		public void SwitchLayout (DockLayout dl)
		{
			//throw new NotImplementedException ();
		}

		public void UpdateSize (DockBar bar, AutoHideBox aframe)
		{
			//throw new NotImplementedException ();
		}
	}
}
#endif
