//
// SizeAndRectanglePreviewVisualizer.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using Mono.Debugging.Client;
using Gtk;
using MonoDevelop.Components;
using Xwt;

namespace MonoDevelop.Debugger.PreviewVisualizers
{
	class SizeAndRectanglePreviewVisualizer : PreviewVisualizer
	{
		#region implemented abstract members of PreviewVisualizer

		public override bool CanVisualize (ObjectValue val)
		{
			return DebuggingService.HasGetConverter<Xwt.Size> (val) || DebuggingService.HasGetConverter<Xwt.Rectangle> (val);
		}

		public override Control GetVisualizerWidget (ObjectValue val)
		{
			var sizeConverter = DebuggingService.GetGetConverter<Xwt.Size> (val);
			if (sizeConverter != null) {
				var size = sizeConverter.GetValue (val);
				return new SizeAndRectanglePreviewVisualizerWidget (size);
			}
			var rectangleConverter = DebuggingService.GetGetConverter<Xwt.Rectangle> (val);
			var rectangle = rectangleConverter.GetValue (val);
			return new SizeAndRectanglePreviewVisualizerWidget (rectangle);
		}

		#endregion
	}

	class SizeAndRectanglePreviewVisualizerWidget : Gtk.DrawingArea
	{
		bool sizeCalculated;

		bool drawDot;
		const int Padding = 7;
		const int TextPadding = 4;
		const int MaxRectangleDrawSize = 250;
		Cairo.Rectangle square;
		string pointString;
		Cairo.PointD pointTextPosition;
		string widthString;
		Cairo.PointD widthTextPosition;
		string heightString;
		Cairo.PointD heightTextPosition;
		Size requisition = new Size ();
		Rectangle rectangle;

		SizeAndRectanglePreviewVisualizerWidget (Rectangle rectangle, bool drawPosition)
		{
			this.drawDot = drawPosition;
			this.rectangle = rectangle;
			Show ();
		}

		public SizeAndRectanglePreviewVisualizerWidget (Xwt.Size size)
			: this (new Rectangle (Point.Zero, size), false)
		{
		}

		public SizeAndRectanglePreviewVisualizerWidget (Xwt.Rectangle rectangle)
			: this (rectangle, true)
		{
		}

		const string valuesFormat = "0,0.0####";

		void CaclulateSize ()
		{
			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				widthString = rectangle.Width.ToString (valuesFormat);
				var widthExtents = ctx.TextExtents (widthString);
				heightString = rectangle.Height.ToString (valuesFormat);
				var heightExtents = ctx.TextExtents (heightString);
				pointString = "(" + rectangle.X.ToString (valuesFormat) + ", " + rectangle.Y.ToString (valuesFormat) + ")";
				var pointExtents = ctx.TextExtents (pointString);
				if (rectangle.Width > MaxRectangleDrawSize || rectangle.Height > MaxRectangleDrawSize) {
					double ratio;
					if (rectangle.Width > rectangle.Height) {
						ratio = rectangle.Width / MaxRectangleDrawSize;
					} else {
						ratio = rectangle.Height / MaxRectangleDrawSize;
					}
					square = new Cairo.Rectangle (Padding, Padding + widthExtents.Height + TextPadding, rectangle.Width / ratio, rectangle.Height / ratio);
				} else {
					square = new Cairo.Rectangle (Padding, Padding + widthExtents.Height + TextPadding, rectangle.Width, rectangle.Height);
				}
				requisition.Width = (int)Math.Max (widthExtents.Width, square.Width);
				if (drawDot && (pointExtents.Width > requisition.Width))
					requisition.Width = (int)pointExtents.Width;
				requisition.Width += (int)heightExtents.Height + TextPadding;

				requisition.Height = (int)Math.Max (heightExtents.Width, square.Height);
				requisition.Height += (int)widthExtents.Height + TextPadding;
				if (drawDot) {
					requisition.Height += (int)pointExtents.Height + TextPadding;
				}

				requisition.Width += Padding * 2;
				requisition.Height += Padding * 2;

				widthTextPosition = new Cairo.PointD (requisition.Width / 2 - widthExtents.Width / 2, square.Y - TextPadding);
				heightTextPosition = new Cairo.PointD (Math.Max (square.X + square.Width + TextPadding, widthTextPosition.X + widthExtents.Width + TextPadding),
					requisition.Height / 2 - heightExtents.Width / 2);
				if (drawDot) {
					pointTextPosition = new Cairo.PointD (TextPadding,
						Math.Max (square.Y + square.Height, heightExtents.Width + heightTextPosition.Y));
					pointTextPosition.Y += TextPadding + pointExtents.Height;
					heightTextPosition.X = Math.Max (heightTextPosition.X, pointTextPosition.X + pointExtents.Width + TextPadding);
				}
			}
			sizeCalculated = true;
			QueueResize ();
			PreviewWindowManager.RepositionWindow ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (!sizeCalculated)
				CaclulateSize ();
			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				ctx.SetSourceRGB (219 / 256.0, 229 / 256.0, 242 / 256.0);
				ctx.Rectangle (square);
				ctx.FillPreserve ();
				ctx.SetSourceRGB (74 / 256.0, 144 / 256.0, 226 / 256.0);
				ctx.Antialias = Cairo.Antialias.None;
				ctx.LineWidth = 1;
				ctx.Stroke ();
				ctx.Antialias = Cairo.Antialias.Default;
				if (drawDot) {
					ctx.Arc (square.X, square.Y + square.Height, 4, 0, 2 * Math.PI);
					ctx.Fill ();
				}

				ctx.SetSourceRGB (0, 0, 0);

				ctx.MoveTo (widthTextPosition);
				ctx.ShowText (widthString);
				if (drawDot) {
					ctx.MoveTo (pointTextPosition);
					ctx.ShowText (pointString);
				}
				ctx.MoveTo (heightTextPosition);
				ctx.Rotate (Math.PI / 2);
				ctx.ShowText (heightString);
			}
			return true;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition = new Requisition () {
				Width = (int)this.requisition.Width,
				Height = (int)this.requisition.Height
			};
		}
	}
}

