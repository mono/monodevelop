/* 
 * ColorEditor.cs - Visual type editor for System.Drawing.Color values
 * 
 * Part of PropertyGrid - A Gtk# widget that displays and allows 
 * editing of all of an object's public properties 
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.ComponentModel;
using Gtk;
using Gdk;

namespace AspNetEdit.UI.PropertyEditors
{
	[PropertyEditorType (typeof (System.Drawing.Color))]
	public class ColorEditor : BaseEditor
	{ 
		public ColorEditor (GridRow parentRow)
			: base (parentRow)
		{
		}

		public override bool InPlaceEdit {
			get { return false; }
		}

		public override Gtk.Widget GetDisplayWidget ()
		{
			DrawingArea colorPreview = new DrawingArea ();

			colorPreview.ModifyBg(StateType.Normal, GetColor ());
			colorPreview.WidthRequest = 15;

			Alignment colorPreviewAlign = new Alignment (0, 0, 0, 1);
			colorPreviewAlign.SetPadding (2, 2, 2, 2);
			colorPreviewAlign.Add (colorPreview);

			string labelText;

			System.Drawing.Color color = (System.Drawing.Color) parentRow.PropertyValue;
			//TODO: dropdown known color selector so this does something
			if (color.IsKnownColor)
				labelText = color.Name;
			else if (color.IsEmpty)
				labelText = "[empty]";
			else
				labelText = String.Format("#{0:x2}{1:x2}{2:x2}", color.R, color.G, color.B);

			//we use StringValue as it auto-bolds the text for non-default values 
			Label theLabel = (Label) base.StringValue (labelText);
			theLabel.Xalign = 0;
			theLabel.Xpad = 3;

			HBox hbox = new HBox ();
			hbox.PackStart (colorPreviewAlign, false, false, 0);
			hbox.PackStart (theLabel, true, true, 0);

			return hbox;
		}

		public override bool DialogueEdit {
			get { return true; }
		}

		private Gdk.Color GetColor ()
		{
			System.Drawing.Color color = (System.Drawing.Color) parentRow.PropertyValue;
			//TODO: Property.Converter.ConvertTo() fails: why?
			return new Gdk.Color (color.R, color.G, color.B);
		}

		public override void LaunchDialogue ()
		{
			ColorSelectionDialog dialog = new ColorSelectionDialog ("Select a new color");
			dialog.ColorSelection.CurrentColor = GetColor ();
			dialog.Run ();
			dialog.Destroy ();

			int red = (int) (255 * (float) dialog.ColorSelection.CurrentColor.Red / ushort.MaxValue);
			int green = (int) (255 * (float) dialog.ColorSelection.CurrentColor.Green / ushort.MaxValue);
			int blue = (int) (255 * (float) dialog.ColorSelection.CurrentColor.Blue / ushort.MaxValue);
			
			System.Drawing.Color color = System.Drawing.Color.FromArgb (red, green, blue);
			//TODO: Property.Converter.ConvertFrom() fails: why?
			parentRow.PropertyValue = color; 
		}
	}
}

