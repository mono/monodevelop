/* 
 * NumberEditor.cs - Visual editor for most simple numerical types.
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

namespace AspNetEdit.UI.PropertyEditors
{

	[PropertyEditorType (typeof (Int16)),
	PropertyEditorType (typeof (Int32)),
	PropertyEditorType (typeof (Int64)),
	PropertyEditorType (typeof (Double)),
	PropertyEditorType (typeof (Single)),
	PropertyEditorType (typeof (Decimal))]
	public class IntEditor : BaseEditor
	{
		

		public IntEditor (GridRow parentRow)
			: base (parentRow)
		{
		}

		public override bool InPlaceEdit {
			get { return true; }
		}

		public override Gtk.Widget GetEditWidget ()
		{
			Gtk.SpinButton spin;

			if (parentRow.PropertyDescriptor.PropertyType == typeof (Int16))
				spin = new SpinButton(Int16.MinValue, Int16.MaxValue, 1);
			else if (parentRow.PropertyDescriptor.PropertyType == typeof (Int32))
				spin = new SpinButton(Int32.MinValue, Int32.MaxValue, 1);
			else if (parentRow.PropertyDescriptor.PropertyType == typeof (Int64))
				spin = new SpinButton(Int64.MinValue, Int64.MaxValue, 1);
			else  //TODO: process floats etc nicely
				spin = new SpinButton(Int64.MinValue, Int64.MaxValue, 1);
			
			spin.HasFrame = false;
			spin.Value = Convert.ToDouble (parentRow.PropertyValue);
			spin.ValueChanged += new EventHandler (spin_ValueChanged);

			return spin;
		}

		void spin_ValueChanged (object sender, EventArgs e)
		{
			Gtk.SpinButton spin = (SpinButton) sender;

			object newValue = Convert.ChangeType (spin.Value, parentRow.PropertyDescriptor.PropertyType);
			parentRow.PropertyValue = newValue;

			//if there's an error such as out-of-range, and value not accepted by parent, restore old value
			if (parentRow.PropertyValue != newValue)
				spin.Value = Convert.ToDouble (parentRow.PropertyValue);		
		}

		public override bool DialogueEdit {
			get { return false; }
		}
}
}