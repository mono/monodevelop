//
// ImageHelper.cs
//
// Author:
//       Vsevolod Kukol <sevo@sevo.org>
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
using System.Collections.Generic;
using System.Windows.Media;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Xwt.Drawing;

namespace WindowsPlatform
{
	public static class ImageHelper
	{
		static Dictionary<string, Image> cachedIcons = new Dictionary<string, Image> ();

		public static Image GetStockIcon (this IconId stockId)
		{
			if (stockId.IsNull)
				return null;
			Image image;
			if (!cachedIcons.TryGetValue (stockId, out image)) {
				var resourceName = stockId + ".png";

				// first check if such a resource exists to avoid a first-chance exception
				using (var stream = typeof (ImageHelper).Assembly.GetManifestResourceStream (resourceName)) {
					if (stream != null) {
						image = Image.FromResource (typeof (ImageHelper), resourceName);
					}
				}

				if (image == null) {
					image = ImageService.GetIcon (stockId);
				}
			}
			if (image == null)
				throw new InvalidOperationException ("Icon not found: " + stockId);
			cachedIcons [stockId] = image;
			return image;
		}

		public static ImageSource GetImageSource (this IconId stockId, Xwt.IconSize size)
		{
			if (stockId.IsNull)
				return null;
			try {
				return stockId.GetStockIcon ().WithSize (size).GetImageSource ();
			} catch (Exception ex) {
				LoggingService.LogError ("Failed loading icon: " + stockId, ex);
			}
			return null;
		}

		public static ImageSource GetImageSource (this Image image)
		{
			return (ImageSource)MonoDevelop.Platform.WindowsPlatform.WPFToolkit.GetNativeImage (image);
		}
	}
}

