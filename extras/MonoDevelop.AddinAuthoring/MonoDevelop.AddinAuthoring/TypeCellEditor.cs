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
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.PropertyGrid;

namespace MonoDevelop.AddinAuthoring
{
	public class TypeCellEditor: PropertyEditorCell
	{
		protected override string GetValueText ()
		{
			return Value.ToString ();
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new TypeEditor ();
		}
	}
	
	public class TypeEditor: Gtk.HBox, IPropertyEditor
	{
		Gtk.Entry entry;
		Gtk.Button goButton;
		Gtk.Button createButton;
		
		static TypeEditor ()
		{
			Gtk.Rc.ParseString ("style \"MonoDevelop.AddinAuthoring.TypeEditor\" {\n GtkButton::inner-border = {0,0,0,0}\n }\n");
			Gtk.Rc.ParseString ("widget \"*.MonoDevelop.AddinAuthoring.TypeEditor\" style  \"MonoDevelop.AddinAuthoring.TypeEditor\"\n");
		}
		
		public TypeEditor()
		{
			entry = new Gtk.Entry ();
			entry.Changed += OnChanged;
			entry.HasFrame = false;
			PackStart (entry, true, true, 0);
			goButton = new Button (new Gtk.Image (Gtk.Stock.JumpTo, IconSize.Menu));
			goButton.Relief = ReliefStyle.None;
			PackStart (goButton, false, false, 0);
			goButton.Name = "MonoDevelop.AddinAuthoring.TypeEditor";
			createButton = new Button (new Gtk.Image ("md-addinauthoring-newclass", IconSize.Menu));
			createButton.Relief = ReliefStyle.None;
			createButton.Name = "MonoDevelop.AddinAuthoring.TypeEditor";
			PackStart (createButton, false, false, 0);
			ShowAll ();
		}
		
		public void Initialize (EditSession session)
		{
		}
		
		public object Value {
			get { return entry.Text; }
			set { entry.Text = (string) value; }
		}
		
		void OnChanged (object o, EventArgs a)
		{
			if (ValueChanged != null)
				ValueChanged (this, a);
		}
		
		public event EventHandler ValueChanged;
	}
}
