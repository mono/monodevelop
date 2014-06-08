﻿//
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
using ICSharpCode.PackageManagement;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.PackageManagement
{
	public class PackageCellView : CanvasCellView
	{
		public PackageCellView ()
		{
			CellWidth = 260;

			BackgroundColor = Color.FromBytes (243, 246, 250);
			StrongSelectionColor = Color.FromBytes (49, 119, 216);
			SelectionColor = Color.FromBytes (204, 204, 204);

			UseStrongSelectionColor = true;
		}

		public IDataField<PackageViewModel> PackageField { get; set; }
		public IDataField<Image> ImageField { get; set; }
		public IDataField<bool> HasBackgroundColorField { get; set; }
		public IDataField<double> CheckBoxAlphaField { get; set; }

		public double CellWidth { get; set; }

		public Color BackgroundColor { get; set; }
		public Color StrongSelectionColor { get; set; }
		public Color SelectionColor { get; set; }

		public bool UseStrongSelectionColor { get; set; }

		public event EventHandler<PackageCellViewEventArgs> PackageChecked;

		protected override void OnDraw (Context ctx, Rectangle cellArea)
		{
			PackageViewModel packageViewModel = GetValue (PackageField);
			if (packageViewModel == null) {
				return;
			}

			FillCellBackground (ctx);
			UpdateTextColor (ctx);

			DrawCheckBox (ctx, packageViewModel, cellArea);
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
			var packageIdTextLayout = new TextLayout ();
			packageIdTextLayout.Markup = packageViewModel.GetNameMarkup ();
			packageIdTextLayout.Trimming = TextTrimming.WordElipsis;
			Size packageIdTextSize = packageIdTextLayout.GetSize ();
			packageIdTextLayout.Width = packageIdWidth;
			ctx.DrawTextLayout (
				packageIdTextLayout,
				cellArea.Left + packageDescriptionPadding.Left + packageDescriptionLeftOffset,
				cellArea.Top + packageDescriptionPadding.Top);

			// Package description.
			var descriptionTextLayout = new TextLayout ();
			descriptionTextLayout.Font = descriptionTextLayout.Font.WithScaledSize (0.9);
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
			if (UseStrongSelectionColor && Selected) {
				ctx.SetColor (Colors.White);
			} else {
				ctx.SetColor (Colors.Black);
			}
		}

		void FillCellBackground (Context ctx)
		{
			if (Selected) {
				FillCellBackground (ctx, GetSelectedColor ());
			} else if (IsBackgroundColorFieldSet ()) {
				FillCellBackground (ctx, BackgroundColor);
			}
		}

		Color GetSelectedColor ()
		{
			if (UseStrongSelectionColor) {
				return StrongSelectionColor;
			}
			return SelectionColor;
		}

		bool IsBackgroundColorFieldSet ()
		{
			return GetValue (HasBackgroundColorField, false);
		}

		void FillCellBackground (Context ctx, Color color)
		{
			ctx.Rectangle (BackgroundBounds);
			ctx.SetColor (color);
			ctx.Fill ();
		}

		void DrawCheckBox (Context ctx, PackageViewModel packageViewModel, Rectangle cellArea)
		{
			CreateCheckboxImages ();

			Image image = GetCheckBoxImage (packageViewModel.IsChecked);
			double alpha = GetCheckBoxImageAlpha ();
			ctx.DrawImage (
				image,
				cellArea.Left + checkBoxPadding.Left,
				cellArea.Top + ((cellArea.Height - checkBoxImageSize.Height - 2) / 2),
				alpha);
		}

		void CreateCheckboxImages ()
		{
			if (whiteCheckedCheckBoxImage != null)
				return;

			var widget = Toolkit.CurrentEngine.GetNativeWidget (ParentWidget);
			var checkbox = new PackageCellViewCheckBox (ParentWidget.ParentWindow.Screen.ScaleFactor);
			checkbox.Container = (Gtk.Widget)widget;
			checkbox.Size = (int)checkBoxImageSize.Width + 1;

			// White checkbox.
			whiteUncheckedCheckBoxImage = checkbox.CreateImage ();
			checkbox.Active = true;
			whiteCheckedCheckBoxImage = checkbox.CreateImage ();

			// Odd numbered checkbox.
			checkbox.BackgroundColor = BackgroundColor;
			checkedCheckBoxWithBackgroundColorImage = checkbox.CreateImage ();
			checkbox.Active = false;
			uncheckedCheckBoxWithBackgroundColorImage = checkbox.CreateImage ();

			// Grey check box.
			checkbox.BackgroundColor = SelectionColor;
			greyUncheckedCheckBoxImage = checkbox.CreateImage ();
			checkbox.Active = true;
			greyCheckedCheckBoxImage = checkbox.CreateImage ();

			// Blue check box.
			checkbox.BackgroundColor = StrongSelectionColor;
			blueCheckedCheckBoxImage = checkbox.CreateImage ();
			checkbox.Active = false;
			blueUncheckedCheckBoxImage = checkbox.CreateImage ();
		}

		double GetCheckBoxImageAlpha ()
		{
			return GetValue (CheckBoxAlphaField, 1);
		}

		Image GetCheckBoxImage (bool checkBoxActive)
		{
			if (Selected && UseStrongSelectionColor && checkBoxActive) {
				return blueCheckedCheckBoxImage;
			} else if (Selected && checkBoxActive) {
				return greyCheckedCheckBoxImage;
			} else if (Selected && UseStrongSelectionColor) {
				return blueUncheckedCheckBoxImage;
			} else if (Selected) {
				return greyUncheckedCheckBoxImage;
			} else if (checkBoxActive && IsBackgroundColorFieldSet ()) {
				return checkedCheckBoxWithBackgroundColorImage;
			} else if (checkBoxActive) {
				return whiteCheckedCheckBoxImage;
			} else if (IsBackgroundColorFieldSet ()) {
				return uncheckedCheckBoxWithBackgroundColorImage;
			} else {
				return whiteUncheckedCheckBoxImage;
			}
		}

		void DrawPackageImage (Context ctx, Rectangle cellArea)
		{
			Image image = GetValue (ImageField);

			if (image == null) {
				image = Selected ? defaultPackageImageDark : defaultPackageImageLight;
			}

			if (PackageImageNeedsResizing (image)) {
				Point imageLocation = GetPackageImageLocation (maxPackageImageSize, cellArea);
				ctx.DrawImage (
					image,
					cellArea.Left + packageImagePadding.Left + checkBoxAreaWidth + imageLocation.X,
					Math.Round( cellArea.Top + packageImagePadding.Top + imageLocation.Y),
					maxPackageImageSize.Width,
					maxPackageImageSize.Height);
			} else {
				Point imageLocation = GetPackageImageLocation (image.Size, cellArea);
				ctx.DrawImage (
					image,
					cellArea.Left + packageImagePadding.Left + checkBoxAreaWidth + imageLocation.X,
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

		protected override Size OnGetRequiredSize ()
		{
			var layout = new TextLayout ();
			layout.Text = "W";
			layout.Font = layout.Font.WithScaledSize (0.9);
			Size size = layout.GetSize ();
			return new Size (CellWidth, size.Height * linesDisplayedCount + packageDescriptionPaddingHeight + packageDescriptionPadding.VerticalSpacing);
		}

		protected override void OnButtonPressed (ButtonEventArgs args)
		{
			PackageViewModel packageViewModel = GetValue (PackageField);
			if (packageViewModel == null) {
				base.OnButtonPressed (args);
				return;
			}

			double x = args.X - Bounds.X;
			double y = args.Y - Bounds.Y;

			if (checkBoxImageClickableRectangle.Contains (x, y)) {
				packageViewModel.IsChecked = !packageViewModel.IsChecked;
				OnPackageChecked (packageViewModel);
			}
		}

		void OnPackageChecked (PackageViewModel packageViewModel)
		{
			if (PackageChecked != null) {
				PackageChecked (this, new PackageCellViewEventArgs (packageViewModel));
			}
		}

		const int packageDescriptionPaddingHeight = 5;
		const int packageIdRightHandPaddingWidth = 5;
		const int linesDisplayedCount = 4;

		const int checkBoxAreaWidth = 36;
		const int packageImageAreaWidth = 54;
		const int packageDescriptionLeftOffset = checkBoxAreaWidth + packageImageAreaWidth + 8;

		WidgetSpacing packageDescriptionPadding = new WidgetSpacing (5, 5, 5, 10);
		WidgetSpacing packageImagePadding = new WidgetSpacing (0, 0, 0, 5);
		WidgetSpacing checkBoxPadding = new WidgetSpacing (10, 0, 0, 10);

		Size maxPackageImageSize = new Size (48, 48);
		Size checkBoxImageSize = new Size (16, 16);
		Rectangle checkBoxImageClickableRectangle = new Rectangle (0, 10, 40, 50);

		Image whiteCheckedCheckBoxImage;
		Image whiteUncheckedCheckBoxImage;
		Image greyCheckedCheckBoxImage;
		Image greyUncheckedCheckBoxImage;
		Image blueCheckedCheckBoxImage;
		Image blueUncheckedCheckBoxImage;
		Image checkedCheckBoxWithBackgroundColorImage;
		Image uncheckedCheckBoxWithBackgroundColorImage;

		static readonly Image defaultPackageImageLight = Image.FromResource (typeof(PackageCellView), "reference-light-48.png");
		static readonly Image defaultPackageImageDark = Image.FromResource (typeof(PackageCellView), "reference-dark-48.png");
	}
}

