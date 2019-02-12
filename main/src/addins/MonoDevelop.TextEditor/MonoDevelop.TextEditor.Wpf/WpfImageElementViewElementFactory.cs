//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.TextEditor.Wpf
{
	[Export (typeof (IViewElementFactory))]
	[Name ("MonoDevelop ImageElement to UIElement")]
	[TypeConversion (from: typeof (ImageElement), to: typeof (UIElement))]
	[Order(Before = "default ImageElement to UIElement")]
	sealed class WpfImageElementViewElementFactory : IViewElementFactory
	{
		public TView CreateViewElement<TView> (ITextView textView, object model) where TView : class
		{
			// Should never happen if the service's code is correct, but it's good to be paranoid.
			if (typeof (TView) != typeof (UIElement) || !(model is ImageElement element)) {
				throw new ArgumentException ($"Invalid type conversion. Unsupported {nameof (model)} or {nameof (TView)} type");
			}

			var image = new Image ();
			if (MonoDevelop.Ide.ImageService.TryGetImage (element.ImageId, out var xwtImage)) {
				var nativeImage = Xwt.Toolkit.NativeEngine.GetNativeImage (xwtImage);
				image.Source = nativeImage as BitmapSource;
			}

			return image as TView;
		}
	}
}