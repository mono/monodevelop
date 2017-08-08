//
// TemplateImageProvider.cs
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
using MonoDevelop.Core;
using Xwt.Drawing;

namespace MonoDevelop.Ide.Templates
{
	class TemplateImageProvider : IDisposable
	{
		Dictionary<string, Image> images = new Dictionary<string, Image> ();
		Dictionary<string, Image> fileImages = new Dictionary<string, Image> ();
		Image defaultImage;

		public TemplateImageProvider ()
		{
			LoadDefaultTemplateImage ();
		}

		void LoadDefaultTemplateImage ()
		{
			defaultImage = LoadImageFromResource (SolutionTemplate.DefaultImageId);
			if (defaultImage != null) {
				images [SolutionTemplate.DefaultImageId] = defaultImage;
			}
		}

		Image LoadImageFromResource (string imageId)
		{
			if (IdeApp.IsInitialized)
				return IdeApp.Services.TemplatingService.LoadTemplateImage (imageId);

			// Should only happen when running unit tests.
			return null;
		}

		public void Dispose ()
		{
			Dispose (images);
			Dispose (fileImages);
		}

		static void Dispose (Dictionary<string, Image> images)
		{
			foreach (Image image in images.Values) {
				image.Dispose ();
			}
			images.Clear ();
		}

		public Image GetImage (SolutionTemplate template)
		{
			try {
				Image image = GetCachedImage (template);
				if (image != null) {
					return image;
				}

				image = GetImageFile (template);
				if (image == null) {
					image = GetImageFromId (template.ImageId);
				}

				if (image == null) {
					image = defaultImage;
				}

				return image;
			} catch (Exception ex) {
				LoggingService.LogError (String.Format ("Unable to load image for project template '{0}'.", template.Id), ex);
				return defaultImage;
			}
		}

		Image GetCachedImage (SolutionTemplate template)
		{
			Image image = null;
			if (template.HasImageFile) {
				if (fileImages.TryGetValue (template.Id, out image)) {
					return image;
				}
			} else {
				if (images.TryGetValue (template.ImageId, out image)) {
					return image;
				}
			}
			return null;
		}

		Image GetImageFile (SolutionTemplate template)
		{
			if (template.HasImageFile) {
				Image image = Image.FromFile (template.ImageFile);

				if (image != null) {
					fileImages [template.Id] = image;
					return image;
				}
			}
			return null;
		}

		Image GetImageFromId (string imageId)
		{
			if (String.IsNullOrEmpty (imageId)) {
				return null;
			}

			Image image = LoadImageFromResource (imageId);
			if (image != null) {
				images [imageId] = image;
			}
			return image;
		}
	}
}

