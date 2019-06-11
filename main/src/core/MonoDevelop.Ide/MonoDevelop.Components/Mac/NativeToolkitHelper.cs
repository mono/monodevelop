//
// NativeToolkitHelper.cs
//
// Author:
//       iain <iaholmes@microsoft.com>
//
// Copyright (c) 2019 
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

#if MAC
using System;
using System.IO;

using AppKit;
using Foundation;

using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.Mac
{
	internal static class NativeToolkitHelper
	{
		static Xwt.Toolkit toolkit;
		internal static Xwt.Toolkit LoadCocoa ()
		{
			if (toolkit != null) {
				return toolkit;
			}

			var path = Path.GetDirectoryName (typeof (IdeTheme).Assembly.Location);
			System.Reflection.Assembly.LoadFrom (Path.Combine (path, "Xwt.XamMac.dll"));
			toolkit = Xwt.Toolkit.Load (Xwt.ToolkitType.XamMac);

			NSNotificationCenter.DefaultCenter.AddObserver (NSApplication.DidFinishLaunchingNotification, (note) => {
				if (note.UserInfo.TryGetValue (NSApplication.LaunchIsDefaultLaunchKey, out var val)) {
					if (val is NSNumber num) {
						IdeApp.LaunchReason = num.BoolValue ? IdeApp.LaunchType.Normal : IdeApp.LaunchType.LaunchedFromFileManager;
						LoggingService.LogDebug ($"Startup was {IdeApp.LaunchReason}");
					}
				}
			});

			return toolkit;
		}
	}
}
#endif