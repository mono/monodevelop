//
// ScreenMonitor.cs
//
// Author:
//       iain <iain@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc
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

namespace MonoDevelop.MacIntegration
{
	public static class ScreenMonitor
	{
		// Maps NSScreens to the coordinates they have in GdkScreen space.
		//
		// [ Monitor1: 1024x800 ] [ Laptop: 2046x1600 ] | [ Monitor2: 1024x800]
		//
		// In Cocoa the main screen is 0,0 and all other screens are given coordinates relative
		// to that screen, so in this example Laptop would be the main screen and the screenspace coordinates are
		// relative to it.
		// Monitor1: -1024, 0
		// Laptop: 0, 0
		// Monitor2: 2046, 0
		//
		// But GdkScreen is different, it creates a giant rectangle encompassing all the screens
		// and coordinates in that screenspace are relative to the top left corner
		// Monitor1: 0, 0
		// Laptop: 1024, 0
		// Monitor2: 3070, 0
		//
		static Dictionary<NSScreen, CGPoint> screenToGdk = null;

		static ScreenMonitor ()
		{
			screenToGdk = UpdateScreenLayout ();
			NSNotificationCenter.DefaultCenter.AddObserver (NSApplication.DidChangeScreenParametersNotification, (obj) => {
				screenToGdk = UpdateScreenLayout ();
			});
		}

		public static CGPoint GdkPointForNSScreen (NSScreen screen)
		{
			return screenToGdk [screen];
		}

		static Dictionary<NSScreen, CGPoint> UpdateScreenLayout ()
		{
			var screenMap = new Dictionary<NSScreen, CGPoint> ();

			var screens = NSScreen.Screens;
			if (screens == null) {
				return screenMap;
			}

			nfloat lowestLeft = 0.0f, highestBottom = 0.0f;
			foreach (var screen in screens) {
				if (screen.Frame.Left < lowestLeft) {
					lowestLeft = screen.Frame.Left;
				}

				if (screen.Frame.Bottom > highestBottom) {
					highestBottom = screen.Frame.Bottom;
				}
			}

			// Now we know that the GdkScreen origin is lowestLeft,highestBottom in NSScreen space
			screens = NSScreen.Screens;
			foreach (var screen in screens) {
				var gdkOrigin = new CGPoint (screen.Frame.Left - lowestLeft, highestBottom - screen.Frame.Bottom);

				screenMap [screen] = gdkOrigin;
			}

			return screenMap;
		}
	}
}

