/* 
 * EventEditor.cs - Visual editor for Events
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
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using Gtk;

namespace MonoDevelop.Components.PropertyGrid.PropertyEditors
{
	[PropertyEditorType (typeof (Delegate), true)]
	public class EventEditorCell : PropertyEditorCell
	{
		IEventBindingService evtBind;

		protected override void Initialize ()
		{
			IComponent comp = Instance as IComponent;
			evtBind = (IEventBindingService) comp.Site.GetService (typeof (IEventBindingService));
			base.Initialize ();
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			//get existing method names
			ICollection IColl = evtBind.GetCompatibleMethods (evtBind.GetEvent (Property)) ;
			string[] methods = new string [IColl.Count + 1];
			IColl.CopyTo (methods, 1);
			
			//add a suggestion
			methods [0] = evtBind.CreateUniqueMethodName ((IComponent) Instance, evtBind.GetEvent (Property));
			
			EventEditor combo = new EventEditor (evtBind, methods);

			if (Value != null)
				combo.Entry.Text = (string) Value;
			
			combo.WidthRequest = 30; //Don't artificially inflate the width. It expands anyway.

			return combo;
		}
		
	}
	
	class EventEditor: ComboBoxEntry, IPropertyEditor
	{
		bool isNull;
		PropertyDescriptor prop;
		IEventBindingService evtBind;
		object component;
		
		public EventEditor (IEventBindingService evtBind, string[] ops): base (ops)
		{
			this.evtBind = evtBind;
		}
		
		public void Initialize (EditSession session)
		{
			this.prop = session.Property;
			component = session.Instance;
			Entry.Destroyed += new EventHandler (entry_Changed);
			Entry.Activated += new EventHandler (entry_Activated);
		}

		public object Value {
			get {
				//if value was null and new value is empty, leave as null
				if (Entry.Text.Length == 0 && isNull)
					return null;
				else
					return Entry.Text;
			}
			set {
				isNull = value == null;
				if (isNull)
					Entry.Text = "";
				else
					Entry.Text = (string) value;
			}
		}

		protected override void OnChanged ()
		{
			if (component == null)
				return;
			entry_Changed (this, null);
			evtBind.ShowCode ((IComponent) component, evtBind.GetEvent (prop));
		}

		void entry_Activated (object sender, EventArgs e)
		{
			entry_Changed (sender, e);
			evtBind.ShowCode ((IComponent) component, evtBind.GetEvent (prop));
		}
		
		void entry_Changed (object sender, EventArgs e)
		{
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}
		
		public event EventHandler ValueChanged;
	}
}