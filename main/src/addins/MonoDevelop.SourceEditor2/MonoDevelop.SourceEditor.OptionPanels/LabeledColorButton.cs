//
// XwtLabeledColorButton.cs
//
// Author:
//       Aleksandr Shevchenko <alexandre.shevchenko@gmail.com>
//
// Copyright (c) 2014 Aleksandr Shevchenko
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
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public class LabeledColorButton:VBox
	{
		Color color = Colors.AliceBlue;
		Label label = new Label ();
		Button colorButton = new Button () { ImagePosition = ContentPosition.Center, Style = ButtonStyle.Flat };
		ImageBuilder builder = new ImageBuilder (40, 15);

		public LabeledColorButton ()
		{
			PackStart (label);
			PackStart (colorButton);
			SetColorToButton (Color);
			colorButton.Clicked += ColorButton_Clicked;
		}

		private void SetColorToButton (Color color)
		{
			builder.Context.SetColor (color);
			builder.Context.Rectangle (0, 0, builder.Width, builder.Height);
			builder.Context.Fill ();
			colorButton.Image = builder.ToBitmap ();
		}

		public LabeledColorButton (string labelText)
			: this ()
		{
			this.label.Text = labelText;
		}

		public event EventHandler ColorSet;

		public string LabelText {
			get{ return label.Text; }
			set{ label.Text = value; }
		}

		public Color Color {
			get{ return color; }
			set {
				color = value;
				SetColorToButton (color);

				if (ColorSet != null)
					ColorSet (this, new EventArgs ());
			}
		}

		void ColorButton_Clicked (object sender, EventArgs e)
		{
			var colorDialog = new SelectColorDialog (label.Text);
			colorDialog.Color = this.Color;
			var result = colorDialog.Run (ParentWindow);

			if (result)
				Color = colorDialog.Color;
		}
	}
}

