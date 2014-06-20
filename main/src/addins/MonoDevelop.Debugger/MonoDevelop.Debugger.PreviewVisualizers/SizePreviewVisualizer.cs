//
// SizePreviewVisualizer.cs
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

namespace MonoDevelop.Debugger.PreviewVisualizers
{
	class SizePreviewVisualizer : PreviewVisualizer
	{
		#region implemented abstract members of PreviewVisualizer

		public override bool CanVisualize (ObjectValue val)
		{
			return val.TypeName == "System.Drawing.Size" ||
			val.TypeName == "Gdk.Size" ||
			val.TypeName == "Xamarin.Forms.Size" ||
			val.TypeName == "System.Drawing.SizeF";
		}

		public override Gtk.Widget GetVisualizerWidget (ObjectValue val)
		{
			var ops = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
			ops.AllowTargetInvoke = true;

			double width, height;

			if (val.TypeName == "System.Drawing.SizeF") {
				width = (float)val.GetChild ("Width", ops).GetRawValue (ops);
				height = (float)val.GetChild ("Height", ops).GetRawValue (ops);
			} else {
				width = (int)val.GetChild ("Width", ops).GetRawValue (ops);
				height = (int)val.GetChild ("Height", ops).GetRawValue (ops);
			}
			return new SizePreviewVisualizerWidget (width, height);
		}

		#endregion
	}

	class SizePreviewVisualizerWidget : Gtk.DrawingArea
	{
		double width;
		double height;

		public SizePreviewVisualizerWidget (double width, double height)
		{
			this.width = width;
			this.height = height;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				ctx.SetSourceRGB (219 / 256.0, 229 / 256.0, 242 / 256.0);
				ctx.Rectangle (10, 10, width, height);
				ctx.FillPreserve ();
				ctx.SetSourceRGB (74 / 256.0, 144 / 256.0, 226 / 256.0);
				ctx.Antialias = Cairo.Antialias.None;
				ctx.LineWidth = 1;
				ctx.Stroke ();
				ctx.Antialias = Cairo.Antialias.Default;

				//TODO: Rectangle only dot
//				ctx.Arc (10, height + 10, 4, 0, 2 * Math.PI);
//				ctx.Fill ();

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
				Height = (int)height + 20,
				Width = (int)width + 20
			};
		}
	}
}

