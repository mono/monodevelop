//
// DateTimeEditorCell.cs
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
using Gtk;
using Gdk;
using System.Text;
using System.ComponentModel;

namespace MonoDevelop.Components.PropertyGrid.PropertyEditors
{
	[PropertyEditorType (typeof (DateTime))]
	public class DateTimeEditorCell: PropertyEditorCell
	{
		protected override string GetValueText ()
		{
			return ((DateTime)Value).ToLongDateString ();
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new DateTimeEditor ();
		}
	}
	
	public class DateTimeEditor: Gtk.HBox, IPropertyEditor
	{
		Gtk.Entry entry;
		DateTime time;
		
		public DateTimeEditor()
		{
			entry = new Gtk.Entry ();
			entry.Changed += OnChanged;
			entry.HasFrame = false;
			PackStart (entry, true, true, 0);
			ShowAll ();
		}
		
		public void Initialize (EditSession session)
		{
		}
		
		public object Value {
			get { return time; }
			set {
				time = (DateTime) value;
				entry.Changed -= OnChanged;
				entry.Text = time.ToString ("G");
				entry.Changed += OnChanged;
			}
		}
		
		void OnChanged (object o, EventArgs a)
		{
			string s = entry.Text;
			
			foreach (string form in formats) {
				try {
					time = DateTime.ParseExact (s, form, null);
					if (ValueChanged != null)
						ValueChanged (this, a);
					break;
				} catch {
				}
			}
		}
		
		public event EventHandler ValueChanged;
		
		static string[] formats = {"u", "G", "g", "d", "T", "t"};
	}
}
