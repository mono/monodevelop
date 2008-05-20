using Gtk;
using System;
using System.Collections;
using System.Reflection;

namespace Stetic {

	class PropertyEditor : VBox
	{
		IPropertyEditor propEditor;
		EditSession session;
		
		public PropertyEditor (PropertyDescriptor prop) : base (false, 0)
		{
			propEditor = CreateEditor (prop);
			Add ((Gtk.Widget) propEditor);
		}

		public void AttachObject (object ob)
		{
			if (session != null)
				session.AttachObject (ob);
		}
		
		public IPropertyEditor CreateEditor (PropertyDescriptor prop)
		{
			PropertyEditorCell cell = PropertyEditorCell.GetPropertyCell (prop);
			cell.Initialize (this, prop, null);

			session = cell.StartEditing (new Gdk.Rectangle (), StateType.Normal);
			if (session == null)
				return new DummyEditor ();
			
			Gtk.Widget w = (Gtk.Widget) session.Editor as Gtk.Widget;
			w.ShowAll ();
			return session.Editor;
		}

		public void Update ()
		{
			if (session != null)
				session.UpdateEditor ();
		}
	}
	
	class DummyEditor: Gtk.Label, IPropertyEditor
	{
		public void Initialize (PropertyDescriptor prop)
		{
			Text = "(" + prop.PropertyType.Name + ")";
		}
		
		public void AttachObject (object obj)
		{
		}
		
		public object Value { 
			get { return null; } 
			set { }
		}
		
		public event EventHandler ValueChanged { add {} remove {} }
	}
}
