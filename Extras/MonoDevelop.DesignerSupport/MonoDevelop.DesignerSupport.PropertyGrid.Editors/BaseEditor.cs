/* 
 * BaseEditor.cs - The base class for the visual type editors
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
using System.Text;
using Gtk;
using System.Reflection;
using System.Drawing.Design;

namespace AspNetEdit.UI.PropertyEditors
{
	public abstract class BaseEditor
	{
		protected GridRow parentRow;

		public BaseEditor(GridRow parentRow)
		{
			this.parentRow = parentRow;
		}

		/// <summary>
		/// whether the value can be changed
		/// </summary>
		public abstract bool InPlaceEdit {
			get;
		}

		/// <summary>
		/// Whether the editor should show a button.
		/// </summary>
		public abstract bool DialogueEdit {
			get;
		}

		/// <summary>
		/// If the property is read-only, is is usually not edited. If the editor
		/// can edit sub-properties of a read-only complex object, this must return true.
		/// <remarks>The default value is false.</remarks>
		/// </summary>
		/// <returns>True if the editor can edit read-only properties</returns>
		public virtual bool EditsReadOnlyObject {
			get { return false; }
		}

		/// <summary>
		/// Opens a dialogue box which sets the property's value. 
		/// Must be overridden if DialogueEdit is true.
		/// </summary>
		public virtual void LaunchDialogue ()
		{
			if (DialogueEdit)
				throw new NotImplementedException();
		}

		/// <summary>
		/// Whether the editor can edit at all, whether in-place, by dialogue or both.
		/// </summary>
		public bool CanEdit
		{
			get { return (InPlaceEdit || DialogueEdit); }
		}

		/// <summary>
		/// Displays the property's current value. Default implementation is simply ToString().
		/// </summary>
		/// <returns>A widget displaying the property's current value.</returns>
		public virtual Widget GetDisplayWidget ()
		{
			object o = parentRow.PropertyValue;
			return StringValue (o);
		}

		/// <summary>
		/// Edits the property's value.
		/// Must be overridden if InPlaceEdit is true.
		/// </summary>
		/// <returns>A widget that can edit the property's value.</returns>
		public virtual Widget GetEditWidget ()
		{
			if (InPlaceEdit)
				throw new NotImplementedException();

			return null;
		}

		/// <summary>
		/// A Gtk.Label suitable for returning by GetDisplayWidget (). Handles nulls. 
		/// </summary>
		/// <param name="value">The new property value Label.</param>
		/// <returns></returns>
		protected Widget StringValue (object value)
		{
			if (value == null)
				return StringValue (null, false);
			else
				return StringValue (value.ToString(), false);
		}

		/// <summary>
		/// A Gtk.Label suitable for returning by GetDisplayWidget(). Handles nulls. 
		/// </summary>
		/// <param name="value">The new property value Label.</param>
		/// <param name="markup">Whether to use GTK markup on the label</param>
		/// <returns></returns>
		protected Widget StringValue (string value, bool markup)
		{
			Label valueLabel = new Label ();
			valueLabel.Xalign = 0;
			valueLabel.Xpad = 3;

			if (value == null)
				valueLabel.Markup = "<span foreground=\"grey\">&lt;null&gt;</span>";
			else
			{
				//make value bold if changed from default
				if (!IsDefaultValue ())
					valueLabel.Markup = "<b>" + value + "</b>";
				else
					valueLabel.Text = value;

				if (markup)
					valueLabel.UseMarkup = true;
			}

			return valueLabel;
		}

		/// <summary>
		/// Checks whether the current value of this property is the default value.
		/// Use to decide whether to make value bold to indicate this.
		/// </summary>
		/// <returns>False if the value has been changed from the default,
		/// and DefaultValueAttribute exists and is not null.</returns>
		protected bool IsDefaultValue ()
		{
			DefaultValueAttribute attrib = (DefaultValueAttribute) parentRow.PropertyDescriptor.Attributes [typeof(DefaultValueAttribute)];
			
			if (attrib == null)
				return false;
				
			if (attrib.Value == null && parentRow.PropertyValue == null)
				return true;
			else if (attrib.Value == null || parentRow.PropertyValue == null)
				return false;
			else
				return (attrib.Value.Equals(parentRow.PropertyValue));
		}
	}
}
