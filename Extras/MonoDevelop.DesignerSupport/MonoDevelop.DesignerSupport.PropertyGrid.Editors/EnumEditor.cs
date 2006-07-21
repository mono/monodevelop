/* 
 * EnumEditor.cs - Visual editor for Enumerations
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
using System.Collections;
using AspNetEdit.UI;

namespace AspNetEdit.UI.PropertyEditors
{

	[PropertyEditorType(typeof(System.Enum))]
	public class EnumEditor : BaseEditor
	{
		ListStore namestore;

		public EnumEditor (GridRow parentRow)
			: base (parentRow)
		{
			if (!parentRow.PropertyDescriptor.PropertyType.IsEnum)
				throw new Exception ("property is not an enum");
		}

		public override bool InPlaceEdit {
			get { return true; }
		}

		public override Widget GetDisplayWidget ()
		{
			return base.StringValue (parentRow.PropertyDescriptor.Converter.ConvertToString (parentRow.PropertyValue));
		}

		public override Gtk.Widget GetEditWidget ()
		{
			namestore = new ListStore (typeof(string));
			ComboBox combo = new ComboBox (namestore);
			CellRenderer rdr = new CellRendererText ();
			combo.PackStart (rdr, true);
			combo.AddAttribute (rdr, "text", 0);

			Array values = System.Enum.GetValues (parentRow.PropertyDescriptor.PropertyType);

			foreach (object s in values) {
				string str = parentRow.PropertyDescriptor.Converter.ConvertToString (s);
				TreeIter t = namestore.AppendValues (str);
				if (str == parentRow.PropertyDescriptor.Converter.ConvertToString (parentRow.PropertyValue))
					combo.SetActiveIter (t);
			}

			combo.Changed += new EventHandler (combo_Changed);
			combo.Destroyed += new EventHandler (combo_Destroyed);
			return combo;
		}

		void combo_Destroyed (object sender, EventArgs e)
		{
			namestore.Dispose ();
		}

		void combo_Changed (object sender, EventArgs e)
		{
			TreeIter t;
			((ComboBox) sender).GetActiveIter(out t);
			parentRow.PropertyValue = parentRow.PropertyDescriptor.Converter.ConvertFromString ((string) namestore.GetValue (t, 0));
		}

		public override bool DialogueEdit {
			get { return false; }
		}
}
}
