//
// GtkWorkaroundsTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using NUnit.Framework;

namespace MonoDevelop.Components
{
	[TestFixture]
	public class GtkWorkaroundsTests : IdeTestBase
	{
		[Test]
		public void TestUsableGeometryMac ()
		{
			var rectangle = GtkWorkarounds.GetUsableMonitorGeometry (Gdk.Screen.Default, 0);

			var macScreen = NSScreen.MainScreen;

			var offsetRect = GetDockSize ();
			var frame = macScreen.Frame;

			frame.Y += NSStatusBar.SystemStatusBar.Thickness + 1;
			frame.Height -= NSStatusBar.SystemStatusBar.Thickness + 1;

			Assert.That (rectangle.Width, Is.EqualTo (frame.Width).Or.EqualTo (frame.Width + offsetRect.Width));
			Assert.That (rectangle.Height, Is.EqualTo (frame.Height).Or.EqualTo (frame.Height + offsetRect.Height));
			Assert.That (rectangle.X, Is.EqualTo (frame.X).Or.EqualTo (frame.X + offsetRect.X));
			Assert.That (rectangle.Y, Is.EqualTo (frame.Y).Or.EqualTo (frame.Y + offsetRect.Y));
		}

		static CGRect GetDockSize ()
		{
			var dockDomain = NSUserDefaults.StandardUserDefaults.PersistentDomainForName ("com.apple.dock");

			var orientation = (NSString)dockDomain.ValueForKey (new NSString ("orientation"));
			var size = ((NSNumber)dockDomain.ValueForKey (new NSString ("tilesize"))).Int32Value + 11;

			var offsetRect = new CGRect ();
			if (orientation == "bottom") {
				offsetRect.Height = -size;
			} else if (orientation == "left") {
				offsetRect.X = size;
				offsetRect.Width = -size;
			} else if (orientation == "right") {
				offsetRect.Width -= size;
			}
			return offsetRect;
		}
	}
}
