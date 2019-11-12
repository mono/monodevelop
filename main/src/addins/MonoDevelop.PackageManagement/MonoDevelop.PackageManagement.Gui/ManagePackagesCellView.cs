//
// PackageCellView.cs
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
using MonoDevelop.Core;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.PackageManagement
{
	internal class ManagePackagesCellView : CanvasCellView
	{
		int packageIdFontSize;
		int packageDescriptionFontSize;

		public ManagePackagesCellView ()
		{
			CellWidth = 260;

			if (Platform.IsWindows) {
				packageIdFontSize = 10;
				packageDescriptionFontSize = 9;
			} else {
				packageIdFontSize = 12;
				packageDescriptionFontSize = 11;
			}
		}

		public IDataField<ManagePackagesSearchResultViewModel> PackageField { get; set; }
		public IDataField<Image> ImageField { get; set; }

		public double CellWidth { get; set; }

		protected override void OnDraw (Context ctx, Rectangle cellArea)
		{
			ManagePackagesSearchResultViewModel packageViewModel = GetValue (PackageField);
			if (packageViewModel == null) {
				return;
			}

			UpdateTextColor (ctx);

			DrawPackageImage (ctx, cellArea);

			double packageIdWidth = cellArea.Width - packageDescriptionPadding.HorizontalSpacing - packageDescriptionLeftOffset;

			// Package download count.
			if (packageViewModel.HasDownloadCount) {
				var downloadCountTextLayout = new TextLayout ();
				downloadCountTextLayout.Text = packageViewModel.GetDownloadCountOrVersionDisplayText ();
				Size size = downloadCountTextLayout.GetSize ();
				Point location = new Point (cellArea.Right - packageDescriptionPadding.Right, cellArea.Top + packageDescriptionPadding.Top);
				Point downloadLocation = location.Offset (-size.Width, 0);
				ctx.DrawTextLayout (downloadCountTextLayout, downloadLocation);

				packageIdWidth = downloadLocation.X - cellArea.Left - packageIdRightHandPaddingWidth - packageDescriptionPadding.HorizontalSpacing - packageDescriptionLeftOffset;
			}

			// Package Id.
			// Use the package id and not the package title to prevent a pango crash if the title
			// contains Chinese characters.
			var packageIdTextLayout = new TextLayout ();
			packageIdTextLayout.Font = packageIdTextLayout.Font.WithSize (packageIdFontSize);
			packageIdTextLayout.Markup = packageViewModel.GetIdMarkup ();
			packageIdTextLayout.Trimming = TextTrimming.WordElipsis;
			Size packageIdTextSize = packageIdTextLayout.GetSize ();
			packageIdTextLayout.Width = packageIdWidth;
			ctx.DrawTextLayout (
				packageIdTextLayout,
				cellArea.Left + packageDescriptionPadding.Left + packageDescriptionLeftOffset,
				cellArea.Top + packageDescriptionPadding.Top);

			// Package description.
			var descriptionTextLayout = new TextLayout ();
			descriptionTextLayout.Font = descriptionTextLayout.Font.WithSize (packageDescriptionFontSize);
			descriptionTextLayout.Width = cellArea.Width - packageDescriptionPadding.HorizontalSpacing - packageDescriptionLeftOffset;
			descriptionTextLayout.Height = cellArea.Height - packageIdTextSize.Height - packageDescriptionPadding.VerticalSpacing;
			descriptionTextLayout.Text = packageViewModel.Summary;
			descriptionTextLayout.Trimming = TextTrimming.Word;

			ctx.DrawTextLayout (
				descriptionTextLayout,
				cellArea.Left + packageDescriptionPadding.Left + packageDescriptionLeftOffset,
				cellArea.Top + packageIdTextSize.Height + packageDescriptionPaddingHeight + packageDescriptionPadding.Top);
		}

		void UpdateTextColor (Context ctx)
		{
			if (Selected) {
				ctx.SetColor (Styles.CellTextSelectionColor);
			} else {
				ctx.SetColor (Styles.CellTextColor);
			}
		}

		void DrawPackageImage (Context ctx, Rectangle cellArea)
		{
			Image image = GetValue (ImageField);

			if (image == null) {
				image = defaultPackageImage;
			}

			if (Selected)
				image = image.WithStyles ("sel");

			if (PackageImageNeedsResizing (image)) {
				Point imageLocation = GetPackageImageLocation (maxPackageImageSize, cellArea);
				ctx.DrawImage (
					image,
					cellArea.Left + packageImagePadding.Left + imageLocation.X,
					Math.Round( cellArea.Top + packageImagePadding.Top + imageLocation.Y),
					maxPackageImageSize.Width,
					maxPackageImageSize.Height);
			} else {
				Point imageLocation = GetPackageImageLocation (image.Size, cellArea);
				ctx.DrawImage (
					image,
					cellArea.Left + packageImagePadding.Left + imageLocation.X,
					Math.Round (cellArea.Top + packageImagePadding.Top + imageLocation.Y));
			}
		}

		bool PackageImageNeedsResizing (Image image)
		{
			return (image.Width > maxPackageImageSize.Width) || (image.Height > maxPackageImageSize.Height);
		}

		Point GetPackageImageLocation (Size imageSize, Rectangle cellArea)
		{
			double width = (packageImageAreaWidth - imageSize.Width) / 2;
			double height = (cellArea.Height - imageSize.Height - packageImagePadding.Bottom) / 2;
			return new Point (width, height);
		}

		protected override Size OnGetRequiredSize (SizeConstraint widthConstraint)
		{
			var layout = new TextLayout ();
			layout.Text = "W";
			layout.Font = layout.Font.WithSize (packageDescriptionFontSize);
			Size size = layout.GetSize ();
			return new Size (CellWidth, size.Height * linesDisplayedCount + packageDescriptionPaddingHeight + packageDescriptionPadding.VerticalSpacing);
		}

		const int packageDescriptionPaddingHeight = 5;
		const int packageIdRightHandPaddingWidth = 5;
		const int linesDisplayedCount = 4;

		const int packageImageAreaWidth = 54;
		const int packageDescriptionLeftOffset = packageImageAreaWidth + 8;

		WidgetSpacing packageDescriptionPadding = new WidgetSpacing (5, 5, 5, 5);
		WidgetSpacing packageImagePadding = new WidgetSpacing (0, 0, 0, 0);

		Size maxPackageImageSize = new Size (48, 48);

		static readonly Image defaultPackageImage = Image.FromResource (typeof(ManagePackagesCellView), "package-48.png");
	}
}

