//
// ImageCache.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Xwt.Drawing;

namespace MonoDevelop.PackageManagement
{
	public class ImageCache
	{
		const int MaxImageCount = 50;

		Queue<Uri> orderedKeys = new Queue<Uri> ();
		Dictionary<Uri, Image> images = new Dictionary<Uri, Image> ();

		public Image GetImage (Uri uri)
		{
			Image image = null;
			if (images.TryGetValue (uri, out image)) {
				return image;
			}
			return null;
		}

		public void AddImage (Uri uri, Image image)
		{
			if (images.ContainsKey (uri))
				return;

			images [uri] = image;
			orderedKeys.Enqueue (uri);
		}

		Image RemoveImage ()
		{
			Uri uri = orderedKeys.Dequeue ();
			Image image = GetImage (uri);
			images.Remove (uri);
			return image;
		}

		public void ShrinkImageCache ()
		{
			while (orderedKeys.Count > MaxImageCount) {
				Image image = RemoveImage ();
				image.Dispose ();
			}
		}
	}
}

