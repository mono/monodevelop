//
// ColorPreviewVisualizer.cs
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
using MonoDevelop.Components;
using Gtk;

namespace MonoDevelop.Debugger.PreviewVisualizers
{
	public class ColorPreviewVisualizer : PreviewVisualizer
	{
		public override bool CanVisualize (ObjectValue val)
		{
			return DebuggingService.HasGetConverter<Xwt.Drawing.Color> (val);
		}

		public override Control GetVisualizerWidget (ObjectValue val)
		{
			var color = DebuggingService.GetGetConverter<Xwt.Drawing.Color> (val).GetValue (val);
			var mainBox = new HBox ();

			var colorBox = new ColorBox { Color = color };
			mainBox.PackStart (colorBox);


			var mainTable = new Table (3, 6, false);
			mainTable.RowSpacing = 2;
			mainTable.ColumnSpacing = 3;
			mainTable.SetColSpacing (1, 12);
			mainTable.SetColSpacing (3, 12);

			var titleColor = new Gdk.Color (139, 139, 139);
			var titleLabel = new Label ("R");
			titleLabel.ModifyFg (StateType.Normal, titleColor);
			mainTable.Attach (titleLabel, 0, 1, 0, 1);
			mainTable.Attach (new Label (((byte)(color.Red * 255.0)).ToString ()){ Xalign = 0 }, 1, 2, 0, 1);
			titleLabel = new Label ("G");
			titleLabel.ModifyFg (StateType.Normal, titleColor);
			mainTable.Attach (titleLabel, 0, 1, 1, 2);
			mainTable.Attach (new Label (((byte)(color.Green * 255.0)).ToString ()){ Xalign = 0 }, 1, 2, 1, 2);
			titleLabel = new Label ("B");
			titleLabel.ModifyFg (StateType.Normal, titleColor);
			mainTable.Attach (titleLabel, 0, 1, 2, 3);
			mainTable.Attach (new Label (((byte)(color.Blue * 255.0)).ToString ()){ Xalign = 0 }, 1, 2, 2, 3);

			titleLabel = new Label ("H");
			titleLabel.ModifyFg (StateType.Normal, titleColor);
			mainTable.Attach (titleLabel, 2, 3, 0, 1);
			mainTable.Attach (new Label ((color.Hue * 360.0).ToString ("0.##") + "°"){ Xalign = 0 }, 3, 4, 0, 1);
			titleLabel = new Label ("S");
			titleLabel.ModifyFg (StateType.Normal, titleColor);
			mainTable.Attach (titleLabel, 2, 3, 1, 2);
			mainTable.Attach (new Label ((color.Saturation * 100.0).ToString ("0.##") + "%"){ Xalign = 0 }, 3, 4, 1, 2);
			titleLabel = new Label ("L");
			titleLabel.ModifyFg (StateType.Normal, titleColor);
			mainTable.Attach (titleLabel, 2, 3, 2, 3);
			mainTable.Attach (new Label ((color.Light * 100.0).ToString ("0.##") + "%"){ Xalign = 0 }, 3, 4, 2, 3);

			titleLabel = new Label ("A");
			titleLabel.ModifyFg (StateType.Normal, titleColor);
			mainTable.Attach (titleLabel, 4, 5, 0, 1);
			mainTable.Attach (new Label (((byte)(color.Alpha * 255.0)).ToString () + " (" + (color.Alpha * 100.0).ToString ("0.##") + "%)"){ Xalign = 0 }, 5, 6, 0, 1);
			titleLabel = new Label ("#");
			titleLabel.ModifyFg (StateType.Normal, titleColor);
			mainTable.Attach (titleLabel, 4, 5, 1, 2);
			mainTable.Attach (new Label (
				((byte)(color.Red * 255.0)).ToString ("X2") +
				((byte)(color.Green * 255.0)).ToString ("X2") +
				((byte)(color.Blue * 255.0)).ToString ("X2")){ Xalign = 0 }, 5, 6, 1, 2);

			mainBox.PackStart (mainTable, true, true, 3);
			mainBox.ShowAll ();
			return mainBox;
		}
	}

	class ColorBox : DrawingArea
	{
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (GdkWindow)) {
				cr.RoundedRectangle (2, 2, 42, 42, 2);
				cr.SetSourceRGB (Color.Red, Color.Green, Color.Blue);
				cr.FillPreserve ();
				var darkColor = Color.WithIncreasedLight (-0.3);
				cr.SetSourceRGB (darkColor.Red, darkColor.Green, darkColor.Blue);
				cr.LineWidth = 1;
				cr.Stroke ();
			}
			return base.OnExposeEvent (evnt);
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition = new Requisition () {
				Width = 46,
				Height = 46
			};
		}

		public Xwt.Drawing.Color Color { get; set; }
	}
}

