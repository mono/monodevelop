/* 
 * ExpandableObjectEditor.cs - Temporary editor until we get expandable object support in main grid
 * 
 * Part of PropertyGrid - A Gtk# widget that displays and allows 
 * editing of all of an object's public properties 
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  Lluis Sanchez Gual
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

using Gtk;

namespace MonoDevelop.Components.PropertyGrid.PropertyEditors
{
	class ExpandableObjectEditor : PropertyEditorCell
	{
		protected override string GetValueMarkup ()
		{
			string val;
			if (Property.Converter.CanConvertTo (Context, typeof(string)))
				val = Property.Converter.ConvertToString (Context, Value);
			else
				val = Value != null ? Value.ToString () : "";
			
			return "<b>" + GLib.Markup.EscapeText (val) + "</b>";
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, StateType state)
		{
			if (Property.Converter.CanConvertTo (Context, typeof(string)) && Property.Converter.CanConvertFrom (Context, typeof(string)))
				return new PropertyTextEditor ();
			else
				return null;
		}

	}
}
