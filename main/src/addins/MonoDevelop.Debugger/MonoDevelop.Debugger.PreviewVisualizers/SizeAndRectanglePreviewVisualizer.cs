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
		bool drawLocation;
		Xwt.Rectangle rectangle;

		public SizeAndRectanglePreviewVisualizerWidget (Xwt.Size size)
		{
			rectangle = new Xwt.Rectangle (Xwt.Point.Zero, size);
			drawLocation = false;
		}

		public SizeAndRectanglePreviewVisualizerWidget (Xwt.Rectangle rectangle)
		{
			this.rectangle = rectangle;
			drawLocation = true;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				ctx.SetSourceRGB (219 / 256.0, 229 / 256.0, 242 / 256.0);
				ctx.Rectangle (10, 10, rectangle.Width, rectangle.Height);
				ctx.FillPreserve ();
				ctx.SetSourceRGB (74 / 256.0, 144 / 256.0, 226 / 256.0);
				ctx.Antialias = Cairo.Antialias.None;
				ctx.LineWidth = 1;
				ctx.Stroke ();
				ctx.Antialias = Cairo.Antialias.Default;

				if (drawLocation) {
					ctx.Arc (10, rectangle.Height + 10, 4, 0, 2 * Math.PI);
					ctx.Fill ();
				}

				//TODO: Write width and height text(rectangle dot point)
//				ctx.MoveTo (20, 20);
//				ctx.SetSourceRGB (0, 0, 0);
//				ctx.SelectFontFace ("sans-serif", Cairo.FontSlant.Normal, Cairo.FontWeight.Normal);
//				ctx.SetFontSize (10);
//				ctx.Rotate (Math.PI / 2);
//				ctx.ShowText ("500.0");
			}
			return true;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition = new Requisition () {
				Height = (int)rectangle.Height + 20,
				Width = (int)rectangle.Width + 20
			};
		}
	}
}

