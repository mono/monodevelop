/* 
 * EventEditor.cs - Visual editor for Events
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
using System.ComponentModel.Design;
using System.Collections;
using Gtk;

namespace AspNetEdit.UI.PropertyEditors
{

	[PropertyEditorType (typeof (Delegate), true)]
	public class EventEditor : BaseEditor
	{
		IEventBindingService evtBind;

		public EventEditor (GridRow parentRow)
			: base (parentRow)
		{
			IComponent comp = parentRow.ParentGrid.CurrentObject as IComponent;
			evtBind = (IEventBindingService) comp.Site.GetService (typeof (IEventBindingService));
		}

		public override bool InPlaceEdit {
			get { return true; }
		}
		
		public override Widget GetDisplayWidget ()
		{
			string str = (string) parentRow.PropertyValue;
			return StringValue ((str == null)? String.Empty : str);
		}

		public override Widget GetEditWidget ()
		{
			//get existing method names
			ICollection IColl = evtBind.GetCompatibleMethods (evtBind.GetEvent (parentRow.PropertyDescriptor)) ;
			string[] methods = new string [IColl.Count + 1];
			IColl.CopyTo (methods, 1);
			
			//add a suggestion
			methods [0] = evtBind.CreateUniqueMethodName ((IComponent) parentRow.ParentGrid.CurrentObject, evtBind.GetEvent (parentRow.PropertyDescriptor));
			
			ComboBoxEntry combo = new ComboBoxEntry (methods);

			if (parentRow.PropertyValue != null)
				combo.Entry.Text = (string) parentRow.PropertyValue;
			
			combo.WidthRequest = 30; //Don't artificially inflate the width. It expands anyway.
			//TODO: do we want events created even if not activated?
			combo.Entry.Destroyed += new EventHandler (entry_Changed);
			combo.Entry.Activated += new EventHandler (entry_Activated);
			combo.Changed += new EventHandler (combo_Activated);

			return combo;
		}
		
		void combo_Activated (object sender, EventArgs e)
		{
			entry_Changed (((ComboBoxEntry) sender).Entry, e);
			evtBind.ShowCode ((IComponent) parentRow.ParentGrid.CurrentObject, evtBind.GetEvent (parentRow.PropertyDescriptor));
		}
		
		void entry_Activated (object sender, EventArgs e)
		{
			entry_Changed (sender, e);
			evtBind.ShowCode ((IComponent) parentRow.ParentGrid.CurrentObject, evtBind.GetEvent (parentRow.PropertyDescriptor));
		}

		void entry_Changed (object sender, EventArgs e)
		{
			string text = ((Entry) sender).Text;
			//if value was null and new value is empty, leave as null
			if (!(text == "" && parentRow.PropertyValue== null))
				parentRow.PropertyValue = ((Entry) sender).Text;
		}

		public override bool DialogueEdit {
			get { return false; }
		}
	}
}