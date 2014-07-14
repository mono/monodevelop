//
// BezierPathPreviewVisualizer.cs
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
using Xwt.Drawing;
using MonoDevelop.Components;
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Debugger.Converters;

namespace MonoDevelop.Debugger.PreviewVisualizers
{
	public class BezierPathPreviewVisualizer : PreviewVisualizer
	{
		#region implemented abstract members of PreviewVisualizer

		public override bool CanVisualize (ObjectValue val)
		{
			return DebuggingService.HasGetConverter<List<BezierPathElement>> (val);
		}

		public override Control GetVisualizerWidget (ObjectValue val)
		{
			var elements = DebuggingService.GetGetConverter<List<BezierPathElement>> (val).GetValue (val);
			if (elements == null) {
				//This happens on older mono runtimes which don't support outArgs
				//Returning null means it will fallback to GenericPreview
				return null;
			}
			var bezierPathWidget = new BezierPathWidget (new Gdk.Size (300, 300), elements);
			bezierPathWidget.Show ();
			return bezierPathWidget;
		}

		#endregion
	}

	class BezierPathWidget : Gtk.DrawingArea
	{
		List<BezierPathElement> elements;
		Gdk.Size size;

		public BezierPathWidget (Gdk.Size size, List<BezierPathElement> elements)
		{
			this.elements = elements;
			this.size = size;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				foreach (var element in elements) {
					switch (element.ElementType) {
					case BezierPathElementType.MoveTo:
						ctx.MoveTo (element.Point1.X, element.Point1.Y);
						break;
					case BezierPathElementType.LineTo:
						ctx.LineTo (element.Point1.X, element.Point1.Y);
						break;
					case BezierPathElementType.ClosePath:
						ctx.ClosePath ();
						break;
					case BezierPathElementType.CurveTo:
						ctx.CurveTo (
							element.Point1.X, element.Point1.Y,
							element.Point2.X, element.Point2.Y,
							element.Point3.X, element.Point3.Y);
						break;
					}
				}

				//Scale down if it's bigger then size
				var ext = ctx.StrokeExtents ();
				double scaleRatio = 1;
				if (ext.Width > size.Width || ext.Height > size.Height) {
					scaleRatio = Math.Min (
						ext.Width < 1 ? 1 : size.Width / ext.Width,
						ext.Height < 1 ? 1 : size.Height / ext.Height);
					ctx.Scale (scaleRatio, scaleRatio);
				}

				//Center object
				double targetX = (size.Width - ext.Width * scaleRatio) / 2;
				double targetY = (size.Height - ext.Height * scaleRatio) / 2;
				ctx.Translate (targetX - ext.X, targetY - ext.Y);

				ctx.NewPath ();
				foreach (var element in elements) {
					switch (element.ElementType) {
					case BezierPathElementType.MoveTo:
						ctx.MoveTo (element.Point1.X, element.Point1.Y);
						break;
					case BezierPathElementType.LineTo:
						ctx.LineTo (element.Point1.X, element.Point1.Y);
						break;
					case BezierPathElementType.ClosePath:
						ctx.ClosePath ();
						break;
					case BezierPathElementType.CurveTo:
						ctx.CurveTo (
							element.Point1.X, element.Point1.Y,
							element.Point2.X, element.Point2.Y,
							element.Point3.X, element.Point3.Y);
						break;
					}
				}


				ctx.NewPath ();
				foreach (var element in elements) {
					switch (element.ElementType) {
					case BezierPathElementType.MoveTo:
						ctx.MoveTo (element.Point1.X, element.Point1.Y);
						break;
					case BezierPathElementType.LineTo:
						ctx.LineTo (element.Point1.X, element.Point1.Y);
						break;
					case BezierPathElementType.ClosePath:
						ctx.ClosePath ();
						break;
					case BezierPathElementType.CurveTo:
						ctx.CurveTo (
							element.Point1.X, element.Point1.Y,
							element.Point2.X, element.Point2.Y,
							element.Point3.X, element.Point3.Y);
						break;
					}
				}

				ctx.SetSourceRGB (219 / 256.0, 229 / 256.0, 242 / 256.0);
				ctx.FillPreserve ();
				ctx.SetSourceRGB (74 / 256.0, 144 / 256.0, 226 / 256.0);
				//ctx.Antialias = Cairo.Antialias.None;
				ctx.LineWidth = 1;
				ctx.Stroke ();
			}
			return true;
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			requisition = new Requisition () {
				Width = size.Width,
				Height = size.Height
			};
		}

	}
}

