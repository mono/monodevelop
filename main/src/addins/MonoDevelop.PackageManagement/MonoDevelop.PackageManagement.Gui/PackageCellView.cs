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
using ICSharpCode.PackageManagement;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.PackageManagement
{
	public class PackageCellView : CanvasCellView
	{
		public IDataField<PackageViewModel> PackageField { get; set; }

		protected override void OnDraw(Context ctx, Rectangle cellArea)
		{
			PackageViewModel packageViewModel = GetValue (PackageField);
			if (packageViewModel == null) {
				return;
			}

			// Package Id.
			var layout = new TextLayout ();
			layout.Markup = packageViewModel.GetNameMarkup ();
			Size packageIdTextSize = layout.GetSize ();
			ctx.DrawTextLayout (layout, cellArea.Left, cellArea.Top);

			// Package download count.
			if (packageViewModel.HasDownloadCount) {
				layout = new TextLayout ();
				layout.Text = packageViewModel.GetDownloadCountDisplayText ();
				Size size = layout.GetSize ();
				Point location = new Point (cellArea.Right, cellArea.Top);
				Point downloadLocation = location.Offset (-size.Width, 0);
				ctx.DrawTextLayout (layout, downloadLocation);
			}

			// Package description.
			layout = new TextLayout ();
			layout.Width = cellArea.Width;
			layout.Height = cellArea.Height - packageIdTextSize.Height;
			layout.Text = packageViewModel.Description;
			layout.Trimming = TextTrimming.Word;

			ctx.DrawTextLayout (layout, cellArea.Left, cellArea.Top + packageIdTextSize.Height + packageDescriptionPaddingHeight);
		}

		protected override Size OnGetRequiredSize()
		{
			var layout = new TextLayout ();
			layout.Text = "W";
			Size size = layout.GetSize ();
			return new Size (packageCellWidth, size.Height * 3 + packageDescriptionPaddingHeight);
		}

		const int packageCellWidth = 260;
		const int packageDescriptionPaddingHeight = 5;
	}
}

