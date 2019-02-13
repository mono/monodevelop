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

using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Core.Imaging;

using MonoDevelop.Core;
using MDImageService = MonoDevelop.Ide.ImageService;

namespace MonoDevelop.TextEditor.Cocoa
{
	// Import with AllowDefault:true
	[Export (typeof (IImageService))]
	public class ImageService : IImageService
	{
		static readonly Dictionary<ImageDescription, object> descriptionToImageMap = new Dictionary<ImageDescription, object> ();
		static readonly Dictionary<ImageTags, string []> tagsToStylesMap = new Dictionary<ImageTags, string []> ();

		public object GetImage (ImageDescription imageDescription)
		{
			if (descriptionToImageMap.TryGetValue (imageDescription, out var nativeImage))
				return nativeImage;

			if (!MDImageService.TryGetImage (imageDescription.Id, generateDefaultIcon: false, out Xwt.Drawing.Image image, out string stockId)) {
				LoggingService.LogWarning ("ImageService missing ImageDescription: {0}", imageDescription);
			}

			if (image == null)
				return null;

			if (imageDescription.Width > 0 && imageDescription.Height > 0)
				image = image.WithSize (imageDescription.Width, imageDescription.Height);

			if (imageDescription.Tags != ImageTags.None) {
				if (!tagsToStylesMap.TryGetValue (imageDescription.Tags, out var styles)) {
					var stylesList = new List<string> (8);

					if (imageDescription.Tags.HasFlag (ImageTags.Dark))
						stylesList.Add ("dark");

					if (imageDescription.Tags.HasFlag (ImageTags.Disabled))
						stylesList.Add ("disabled");

					if (imageDescription.Tags.HasFlag (ImageTags.Error))
						stylesList.Add ("error");

					if (imageDescription.Tags.HasFlag (ImageTags.Hover))
						stylesList.Add ("hover");

					if (imageDescription.Tags.HasFlag (ImageTags.Pressed))
						stylesList.Add ("pressed");

					if (imageDescription.Tags.HasFlag (ImageTags.Selected))
						stylesList.Add ("sel");

					if (stylesList.Count > 0)
						tagsToStylesMap [imageDescription.Tags] = styles = stylesList.ToArray ();
				}

				if (styles != null && styles.Length > 0)
					image = image.WithStyles (styles);
			}

			if (image != null) {
				nativeImage = Xwt.Toolkit.NativeEngine.GetNativeImage (image);
				if (nativeImage is AppKit.NSImage nsImage) {
					if (stockId != null)
						nsImage.Name = stockId;
					if (imageDescription.Tags.HasFlag (ImageTags.Template))
						nsImage.Template = true;
				}

				descriptionToImageMap [imageDescription] = nativeImage;
				return nativeImage;
			}

			return null;
		}

		public object GetImage (ImageId imageId)
			=> GetImage (new ImageDescription (imageId, ImageTags.None));
	}
}