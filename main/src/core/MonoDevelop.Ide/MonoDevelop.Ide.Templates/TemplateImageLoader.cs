//
// TemplateImageLoader.cs
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
using System.IO;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Codons;
using Xwt.Backends;
using Xwt.Drawing;

namespace MonoDevelop.Ide.Templates
{
	internal class TemplateImageLoader
	{
		public Image LoadImageFromResource (string id)
		{
			ImageCodon imageCodon = IdeApp.Services.TemplatingService.GetTemplateImageCodon (id);
			if (imageCodon != null) {
				return LoadImageFromResource (imageCodon);
			}

			return null;
		}

		public Image LoadImageFromResource (ImageCodon imageCodon)
		{
			Func<Stream[]> imageLoader = delegate {
				var stream = imageCodon.Addin.GetResource (imageCodon.Resource);
				var stream2x = GetResource2x (imageCodon.Addin, imageCodon.Resource);
				if (stream2x == null)
					return new [] { stream };
				else
					return new [] { stream, stream2x };
			};
			return LoadImage (imageLoader);
		}

		public Image LoadImageFromFile (string file)
		{
			Func<Stream[]> imageLoader = delegate {
				var stream = File.OpenRead (file);
				Stream stream2x = null;
				var file2x = Path.Combine (Path.GetDirectoryName (file), Path.GetFileNameWithoutExtension (file) + "@2x" + Path.GetExtension (file));
				if (File.Exists (file2x))
					stream2x = File.OpenRead (file2x);
				else {
					file2x = file + "@2x";
					if (File.Exists (file2x))
						stream2x = File.OpenRead (file2x);
				}
				if (stream2x == null)
					return new [] { stream };
				else
					return new [] { stream, stream2x };
			};
			return LoadImage (imageLoader);
		}

		static Image LoadImage (Func<Stream[]> imageLoader)
		{
			Gdk.Pixbuf pixbuf = null, pixbuf2x = null;

			// using the stream directly produces a gdk warning.
			byte[] buffer;

			var streams = imageLoader ();

			var st = streams[0];
			var st2x = streams.Length > 1 ? streams[1] : null;

			using (st) {
				if (st == null || st.Length < 0) {
					return null;
				}
				buffer = new byte [st.Length];
				st.Read (buffer, 0, (int)st.Length);
			}
			pixbuf = new Gdk.Pixbuf (buffer);

			using (st2x) {
				if (st2x != null && st2x.Length >= 0) {
					buffer = new byte [st2x.Length];
					st2x.Read (buffer, 0, (int)st2x.Length);
					pixbuf2x = new Gdk.Pixbuf (buffer);
				}
			}

			var img = Xwt.Toolkit.CurrentEngine.WrapImage (pixbuf);
			if (pixbuf2x != null) {
				var img2x = Xwt.Toolkit.CurrentEngine.WrapImage (pixbuf2x);
				img = Image.CreateMultiResolutionImage (new[] {
					img,
					img2x
				});
			}
			if (imageLoader != null)
				img.SetStreamSource (imageLoader);
			return img;
		}

		static Stream GetResource2x (RuntimeAddin addin, string id)
		{
			var stream = addin.GetResource (Path.GetFileNameWithoutExtension (id) + "@2x" + Path.GetExtension (id));
			if (stream == null)
				stream = addin.GetResource (id + "@2x");
			return stream;
		}
	}
}

