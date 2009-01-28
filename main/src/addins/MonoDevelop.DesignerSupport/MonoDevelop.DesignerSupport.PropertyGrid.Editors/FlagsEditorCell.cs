//
// FlagsEditorCell.cs
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

using Gtk;
using System;
using System.Collections;
using System.ComponentModel;

namespace MonoDevelop.DesignerSupport.PropertyGrid.PropertyEditors {

	public class FlagsEditorCell: PropertyEditorCell
	{
		protected override string GetValueText ()
		{
			if (Value == null)
				return "";

			ulong value = Convert.ToUInt64 (Value);
			Array values = System.Enum.GetValues (base.Property.PropertyType);
			string txt = "";
			foreach (object val in values) {
				if ((value & Convert.ToUInt64 (value)) != 0) {
					if (txt.Length > 0) txt += ", ";
					txt += val.ToString ();
				}
			}
			return txt;
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new FlagsEditor ();
		}
	}
	
	public class FlagsEditor : Gtk.HBox, IPropertyEditor
	{
		Hashtable flags;
		Gtk.Tooltips tips;
		Gtk.Entry flagsLabel;
		string property;
		Type propType;
		Array values;

		public FlagsEditor ()
		{
		}
		
		public void Initialize (EditSession session)
		{
			PropertyDescriptor prop = session.Property;
			
			if (!prop.PropertyType.IsEnum)
				throw new ApplicationException ("Flags editor does not support editing values of type " + prop.PropertyType);
			
			Spacing = 3;
			propType = prop.PropertyType;
			
			property = prop.Description;
			if (property == null || property.Length == 0)
				property = prop.Name;

			// For small enums, the editor is a list of checkboxes inside a frame
			// For large enums (>5), use a selector dialog.

			values = System.Enum.GetValues (prop.PropertyType);
			
			//FIXME: The checkboxes are all getting squashed together due to an incorrect height request calculation
			/*
			if (values.Length < 6) 
			{
				Gtk.VBox vbox = new Gtk.VBox (true, 3);

				tips = new Gtk.Tooltips ();
				flags = new Hashtable ();

				foreach (object value in values) {
					Gtk.CheckButton check = new Gtk.CheckButton (value.ToString ());
					tips.SetTip (check, value.ToString (), value.ToString ());
					ulong uintVal = Convert.ToUInt64 (value);
					flags[check] = uintVal;
					flags[uintVal] = check;
					
					check.Toggled += FlagToggled;
					vbox.PackStart (check, false, false, 0);
				}

				Gtk.Frame frame = new Gtk.Frame ();
				frame.Add (vbox);
				frame.ShowAll ();
				PackStart (frame, true, true, 0);
			} 
			else */
			{
				flagsLabel = new Gtk.Entry ();
				flagsLabel.IsEditable = false;
				flagsLabel.HasFrame = false;
				flagsLabel.ShowAll ();
				PackStart (flagsLabel, true, true, 0);
				
				Gtk.Button but = new Gtk.Button ("...");
				but.Clicked += OnSelectFlags;
				but.ShowAll ();
				PackStart (but, false, false, 0);
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
				return Enum.ToObject (propType, UIntValue);
			}
			set {
				ulong newVal = Convert.ToUInt64 (value);
				if (flagsLabel != null) {
					string txt = "";
					foreach (object val in values) {
						if ((newVal & Convert.ToUInt64(val)) != 0) {
							if (txt.Length > 0) txt += ", ";
							txt += val.ToString ();
						}
					}
					flagsLabel.Text = txt;
					UIntValue = newVal;
				}
				else {
					for (ulong i = 1; i <= uintValue || i <= newVal; i = i << 1) {
						if ((uintValue & i) != (newVal & i)) {
							Gtk.CheckButton check = (Gtk.CheckButton)flags[i];
							if (check != null)
								check.Active = !check.Active;
						}
					}
				}
			}
		}

		public event EventHandler ValueChanged;

		ulong uintValue;
		
		ulong UIntValue {
			get {
				return uintValue;
			}
			set {
				if (uintValue != value) {
					uintValue = value;
					if (ValueChanged != null)
						ValueChanged (this, EventArgs.Empty);
				}
			}
		}

		void FlagToggled (object o, EventArgs args)
		{
			Gtk.CheckButton check = (Gtk.CheckButton)o;
			ulong val = (ulong)flags[o];

			if (check.Active)
				UIntValue |= val;
			else
				UIntValue &= ~val;
		}
		
		void OnSelectFlags (object o, EventArgs args)
		{
			using (FlagsSelectorDialog dialog = new FlagsSelectorDialog (null, propType, UIntValue, property)) {
				if (dialog.Run () == (int) ResponseType.Ok) {
					Value = Enum.ToObject (propType, dialog.Value);
				}
			}
		}
	}
}
