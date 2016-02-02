//
// MultiResImage.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
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
using System.Reflection;
using AppKit;
using Foundation;

using MonoDevelop.Ide;
using MonoDevelop.Core;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	public static class MultiResImage
	{
		public static NSImage CreateMultiResImage (string filename, string style)
		{
			var image = new NSImage ();

			var image1x = NSImageFromResource (MakeResName (filename, style));
			var image2x = NSImageFromResource (MakeResName (filename, style, true));

			if (image1x != null) {
				image.AddRepresentations (image1x.Representations ());
			}

			if (image2x != null) {
				image.AddRepresentations (image2x.Representations ());
			}

			image.Size = new CoreGraphics.CGSize (0, 0);
			return image;
		}

		static string MakeResName (string filename, string style, bool retina = false)
		{
			bool dark = IdeApp.Preferences.UserInterfaceSkin == Skin.Dark;

			if (!string.IsNullOrEmpty (style)) {
				style = "~" + style;
			}

			string resname = string.Format ("{0}{1}{2}{3}.png", filename, dark ? "~dark" : "", style, retina ? "@2x" : "");
			if (Assembly.GetCallingAssembly ().GetManifestResourceInfo (resname) != null) {
				return resname;
			}

			resname = string.Format ("{0}{1}{2}.png", filename, dark ? "~dark" : "", retina ? "@2x" : "");
			if (Assembly.GetCallingAssembly ().GetManifestResourceInfo (resname) != null) {
				return resname;
			}

			resname = string.Format ("{0}{1}.png", filename, retina ? "@2x" : "");
			if (Assembly.GetCallingAssembly ().GetManifestResourceInfo (resname) != null) {
				return resname;
			}

			// If all those failed, try again, but without retina
			if (retina) {
				LoggingService.LogWarning ("{0} {1} missing @2x", filename, style);
				return MakeResName (filename, style);
			}

			return null;
		}

		static NSImage NSImageFromResource (string res)
		{
			if (string.IsNullOrEmpty (res)) {
				return null;
			}

			var stream = Assembly.GetCallingAssembly ().GetManifestResourceStream (res);
			using (stream)
			using (NSData data = NSData.FromStream (stream)) {
				return new NSImage (data);
			}
		}
	}
}

