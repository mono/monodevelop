// 
// SigningCombo.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Gtk;
using System.ComponentModel;
using MonoDevelop.Core;

namespace MonoDevelop.MacDev
{
	[ToolboxItem (true)]
	[Category ("MonoDevelop.MacDev")]
	public class SigningIdentityCombo : ComboBox
	{
		ListStore store;
		
		public SigningIdentityCombo () : base (new ListStore (typeof (string), typeof (string), typeof (object)))
		{
			store = (ListStore) this.Model;
			
			var txtRenderer = new CellRendererText ();
			txtRenderer.Ellipsize = Pango.EllipsizeMode.End;
			PackStart (txtRenderer, true);
			AddAttribute (txtRenderer, "markup", 0);
			
			RowSeparatorFunc = delegate (TreeModel model, TreeIter iter) {
				return (string)model.GetValue (iter, 0) == "-";
			};
		}
		
		public TreeIter AddItem (string label, string name, object item)
		{
			return store.AppendValues (GLib.Markup.EscapeText (label), name, item);
		}
		
		public TreeIter AddItemWithMarkup (string labelMarkup, string name, object item)
		{
			return store.AppendValues (labelMarkup, name, item);
		}
		
		public void AddSeparator ()
		{
			store.AppendValues ("-", "-", null);
		}
		
		public object SelectedItem {
			get {
				return this.GetActiveValue<object> (2);
			}
		}
		
		public string SelectedName {
			get {
				return this.GetActiveValue<string> (1);
			}
			set {
				if (string.IsNullOrEmpty (value) && store.IterNChildren () > 0) {
					Active = 0;
				} else if (!this.SelectMatchingItem (1, value)) {
					var name = GettextCatalog.GetString ("Unknown ({0})", value);
					var iter = AddItem (GLib.Markup.EscapeText (name), value, new object ());
					this.SetActiveIter (iter);
				}
				Sensitive = store.IterNChildren () > 1;
				OnChanged ();
			}
		}
		
		public void ClearList ()
		{
			store.Clear ();
		}
	}
	
	static class GtkExtensions
	{
		public static bool SelectMatchingItem (this ComboBox combo, int column, object value)
		{
			var m = combo.Model;
			TreeIter iter;
			int i = 0;
			if (m.GetIterFirst (out iter)) {
				do {
					if (value.Equals (m.GetValue (iter, column))) {
						combo.Active = i;
						return true;
					}
					i++;
				} while (m.IterNext (ref iter));
			}
			return false;
		}
		
		public static T GetActiveValue<T> (this ComboBox combo, int column) where T : class
		{
			TreeIter iter;
			if (combo.GetActiveIter (out iter))
					return (T) combo.Model.GetValue (iter, column);
			return null;
		}
	}
}

