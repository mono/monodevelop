/* 
 * BoolEditor.cs - Visual type editor for boolean values
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

	[PropertyEditorType (typeof (bool))]
	public class BoolEditor : BaseEditor
	{
		public BoolEditor (GridRow parentRow)
			: base (parentRow)
		{
		}


		public override bool InPlaceEdit {
			get { return true; }
		}

		public override Gtk.Widget GetEditWidget ()
		{
			CheckButton check = new CheckButton ();
			check.Active = (bool) parentRow.PropertyValue;
			check.Label = check.Active.ToString ();
			check.Toggled += new EventHandler (check_Toggled);

			return check;
		}

		void check_Toggled(object sender, EventArgs e)
		{
			CheckButton check = (CheckButton) sender;
			check.Label = check.Active.ToString ();
			parentRow.PropertyValue = check.Active;
		}

		public override bool DialogueEdit {
			get { return false; }
		}
}
}
