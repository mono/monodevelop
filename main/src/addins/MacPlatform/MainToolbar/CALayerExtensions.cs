//
// CALayerExtensions.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
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
using CoreAnimation;
using MonoDevelop.Components;
using CoreGraphics;
using MonoDevelop.Ide;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	static class CALayerExtensions
	{
		internal static void SetImage (this CALayer layer, string resource, nfloat scale)
		{
			SetImage (layer, ImageService.GetIcon (resource, Gtk.IconSize.Menu), scale);
		}

		internal static void SetImage (this CALayer layer, Xwt.Drawing.Image xwtImage, nfloat scale)
		{
			var image = xwtImage.ToNSImage ();
			layer.ContentsScale = scale;
			var layerContents = image.GetLayerContentsForContentsScale (layer.ContentsScale);

			void_objc_msgSend_IntPtr (layer.Handle, setContentsSelector, layerContents.Handle);
			layer.Bounds = new CGRect (0, 0, image.Size.Width, image.Size.Height);
		}

		static readonly IntPtr setContentsSelector = ObjCRuntime.Selector.GetHandle ("setContents:");
		const string LIBOBJC_DYLIB = "/usr/lib/libobjc.dylib";
		[System.Runtime.InteropServices.DllImport (LIBOBJC_DYLIB, EntryPoint="objc_msgSend")]
		public extern static void void_objc_msgSend_IntPtr (IntPtr receiver, IntPtr selector, IntPtr arg);
	}
}

