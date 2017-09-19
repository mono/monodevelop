﻿//
// Cursor.cs
//
// Author:
//       mkrueger <>
//
// Copyright (c) 2017 ${CopyrightHolder}
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
using CoreImage;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor
{
	class OSXEditor
	{
		static Xwt.Drawing.Image image;

		public static Xwt.Drawing.Image IBeamCursorImage {
			get {
				if (image != null)
					return image;
				var cacheFileName = Path.Combine(UserProfile.Current.CacheDir, "MacCursorImage.tiff");
				if (!File.Exists(cacheFileName))
					NSCursor.IBeamCursor.Image.AsTiff().Save(cacheFileName, true);
				var img = Xwt.Drawing.Image.FromFile(cacheFileName);
				var size = NSCursor.IBeamCursor.Image.Size;
				image = img.WithSize(size.Width, size.Height);
				return image;
			}
		}

		public static int GetLineHeight(string fontName)
		{
			var editorFont = Xwt.Drawing.Font.FromName(fontName);

			using (var nsFont = NSFont.FromFontName(editorFont.Family, (nfloat)editorFont.Size))
				using (var lm = new NSLayoutManager())
			 		return (int)lm.DefaultLineHeightForFont(nsFont);
		}
	}
}
#endif