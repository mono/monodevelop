//
// EnumerationEditorCell.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace MonoDevelop.DesignerSupport.PropertyGrid.PropertyEditors
{
	[PropertyEditorType(typeof(System.Enum))]
	public class EnumerationEditorCell: PropertyEditorCell
	{
		protected override string GetValueText ()
		{
			if (Value == null)
				return "";

			string val = Value.ToString ();
			
			// If the enum value has a Description attribute, return the description
			foreach (FieldInfo f in Property.PropertyType.GetFields ()) {
				if (f.Name == val) {
					DescriptionAttribute att = (DescriptionAttribute) Attribute.GetCustomAttribute (f, typeof(DescriptionAttribute));
					if (att != null)
						return att.Description;
					else
						return val;
				}
			}
			return val;
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new EnumerationEditor ();
		}
	}
	
	public class EnumerationEditor : Gtk.HBox, IPropertyEditor {

		Gtk.EventBox ebox;
		Gtk.ComboBoxEntry combo;
		Gtk.Tooltips tips;
		Array values;

		public EnumerationEditor () : base (false, 0)
		{
		}
		
		public void Initialize (EditSession session)
		{
			PropertyDescriptor prop = session.Property;
			
			if (!prop.PropertyType.IsEnum)
				throw new ApplicationException ("Enumeration editor does not support editing values of type " + prop.PropertyType);
			
			values = System.Enum.GetValues (prop.PropertyType);
			Hashtable names = new Hashtable ();
			foreach (FieldInfo f in prop.PropertyType.GetFields ()) {
				DescriptionAttribute att = (DescriptionAttribute) Attribute.GetCustomAttribute (f, typeof(DescriptionAttribute));
				if (att != null)
					names [f.Name] = att.Description;
				else
					names [f.Name] = f.Name;
			}
				       
			
			ebox = new Gtk.EventBox ();
			ebox.Show ();
			PackStart (ebox, true, true, 0);

			combo = Gtk.ComboBoxEntry.NewText ();
			combo.Changed += combo_Changed;
			combo.Entry.IsEditable = false;
			combo.Entry.HasFrame = false;
			combo.Entry.HeightRequest = combo.SizeRequest ().Height;
			combo.Show ();
			ebox.Add (combo);

			tips = new Gtk.Tooltips ();

			foreach (object value in values) {
				string str = prop.Converter.ConvertToString (value);
				if (names.Contains (str))
					str = (string) names [str];
				combo.AppendText (str);
			}
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			((IDisposable)this).Dispose ();
		}

		void IDisposable.Dispose ()
		{
			if (tips != null) {
				tips.Destroy ();
				tips = null;
			}
		}

		public object Value {
			get {
				return values.GetValue (combo.Active);
			}
			set {
				int i = Array.IndexOf (values, value);
				if (i != -1)
					combo.Active = i;
			}
		}

		public event EventHandler ValueChanged;

		void combo_Changed (object o, EventArgs args)
		{
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
			if (Value != null)
				tips.SetTip (ebox, Value.ToString (), Value.ToString ());
			else
				tips.SetTip (ebox, null, null);
		}
	}
}
