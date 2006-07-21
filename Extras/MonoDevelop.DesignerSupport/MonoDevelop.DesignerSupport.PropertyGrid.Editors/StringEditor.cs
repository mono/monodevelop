/* 
 * StringEditor.cs - Visual editor for strings, or values that
 *	convert to/from strings.
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

	[PropertyEditorType (typeof (string))]
	public class StringEditor : BaseEditor
	{

		public StringEditor (GridRow parentRow)
			: base (parentRow)
		{
		}

		public override bool InPlaceEdit {
			get { return true; }
		}

		public override Widget GetEditWidget ()
		{
			Entry entry = new Entry ();

			if (parentRow.PropertyValue == null)
				entry.Text = null;
			else
				entry.Text = parentRow.PropertyDescriptor.Converter.ConvertToString (parentRow.PropertyValue);
			
			entry.HasFrame = false;
			entry.WidthRequest = 30; //Don't artificially inflate the width. It expands anyway.
			//TODO: Is entry.Changed too responsive?
			//entry.Changed += new EventHandler (entry_Changed);
			entry.Destroyed += new EventHandler (entry_Changed);
			entry.Activated += new EventHandler (entry_Changed);

			return entry;
		}

		void entry_Changed (object sender, EventArgs e)
		{
			//Catching all exceptions is bad, but converter can throw all sorts of exception
			//with invalid entries. We just want to ignore bad entries.
			string text = ((Entry) sender).Text;
			try {
				//if value was null and new value is empty, leave as null
				if (!(text == "" && parentRow.PropertyValue== null))
					parentRow.PropertyValue = parentRow.PropertyDescriptor.Converter.ConvertFromString (((Entry) sender).Text);
			}
			catch (Exception ex)
			{
				//we want to give a helpful error message: even if we ignore these exceptions
				//most of the time, the error may still be useful when debugging controls
				System.Diagnostics.Trace.WriteLine (
					"PropertyGrid String Editor: TypeConverter could not convert string \"" + text +
					"\" to " + parentRow.PropertyDescriptor.PropertyType.ToString () +
					" for property \"" + parentRow.PropertyDescriptor.DisplayName + "\".\n" +
					"Error details: "+ ex.Message
				);
			}
		}

		public override bool DialogueEdit {
			get { return false; }
		}
	}

}
