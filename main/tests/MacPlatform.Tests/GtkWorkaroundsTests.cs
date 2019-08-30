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
using System.Runtime.InteropServices;
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
		[DllImport (ObjCRuntime.Constants.AppKitLibrary)]
		static extern void CoreDockGetRect (out CGRect rect);

		[DllImport (ObjCRuntime.Constants.AppKitLibrary)]
		static extern void CoreDockGetOrientationAndPinning (out int orientation, out int pinning);

		const int DockOrientationBottom = 2;
		const int DockOrientationLeft = 3;
		const int DockOrientationRight = 4;

		[Test]
		public void TestUsableGeometryMac ()
		{
			var rectangle = Gdk.Screen.Default.GetUsableMonitorGeometry (0);

			var macScreen = NSScreen.MainScreen;

			var frame = macScreen.Frame;

			frame.Y += NSStatusBar.SystemStatusBar.Thickness + 1;
			frame.Height -= NSStatusBar.SystemStatusBar.Thickness + 1;

			CoreDockGetRect (out CGRect dockRect);
			CoreDockGetOrientationAndPinning (out int orientation, out _);

			switch (orientation) {
			case DockOrientationBottom:
				frame.Height -= dockRect.Height;
				break;
			case DockOrientationLeft:
				frame.X += dockRect.Width;
				goto case DockOrientationRight;
			case DockOrientationRight:
				frame.Width -= dockRect.Width;
				break;
			default:
				Assert.Fail ("Unknown orientation value {0}", orientation);
				break;
			}

			Assert.AreEqual (rectangle.Width, frame.Width);
			Assert.AreEqual (rectangle.Height, frame.Height);
			Assert.AreEqual (rectangle.X, frame.X);
			Assert.AreEqual (rectangle.Y, frame.Y);
		}
	}
}
