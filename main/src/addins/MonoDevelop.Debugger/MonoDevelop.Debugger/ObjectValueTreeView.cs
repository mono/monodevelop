// ObjectValueTree.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using Gtk;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Components;

namespace MonoDevelop.Debugger
{
	public class ObjectValueTreeView: Gtk.TreeView
	{
		List<string> valueNames = new List<string> ();
		IValueTreeSource source;
		TreeStore store;
		TreeViewState state;
		bool allowAdding;
		bool allowEditing;
		
		CellRendererText crtExp;
		CellRendererText crtValue;
		
		const int NameCol = 0;
		const int ValueCol = 1;
		const int TypeCol = 2;
		const int ObjectCol = 3;
		const int ExpandedCol = 4;
		const int NameEditableCol = 5;
		const int ValueEditableCol = 6;
		
		public ObjectValueTreeView ()
		{
			store = new TreeStore (typeof(string), typeof(string), typeof(string), typeof(ObjectValue), typeof(bool), typeof(bool), typeof(bool));
			Model = store;
			
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Name");
			crtExp = new CellRendererText ();
			col.PackStart (crtExp, true);
			col.AddAttribute (crtExp, "text", NameCol);
			col.AddAttribute (crtExp, "editable", NameEditableCol);
			AppendColumn (col);
			
			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Value");
			crtValue = new CellRendererText ();
			col.PackStart (crtValue, true);
			col.AddAttribute (crtValue, "markup", ValueCol);
			col.AddAttribute (crtValue, "editable", ValueEditableCol);
			AppendColumn (col);
			
			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Type");
			CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", TypeCol);
			AppendColumn (col);
			
			state = new TreeViewState (this, NameCol);
			
			crtExp.Edited += OnExpEdited;
			crtValue.Edited += OnValueEdited;
		}
		
		public IValueTreeSource Source {
			get {
				return source;
			}
			set {
				source = value;
				Update ();
			}
		}
		
		public bool AllowAdding {
			get {
				return allowAdding;
			}
			set {
				allowAdding = value;
				Update ();
			}
		}
		
		public bool AllowEditing {
			get {
				return allowEditing;
			}
			set {
				allowEditing = value;
				Update ();
			}
		}
		
		public void AddValue (string exp)
		{
			valueNames.Add (exp);
			Update ();
		}
		
		public void RemoveValue (string exp)
		{
			valueNames.Remove (exp);
			Update ();
		}
		
		public void Update ()
		{
			state.Save ();
			
			store.Clear ();
			
			if (source != null) {
				foreach (ObjectValue val in source.GetValues (valueNames.ToArray ()))
					AppendValue (TreeIter.Zero, val);
			}
			
			if (AllowAdding)
				store.AppendValues ("", "", "", null, true, true);
			
			state.Load ();
		}
		
		void AppendValue (TreeIter parent, ObjectValue val)
		{
			string strval;
			bool canEdit;
			if (val.Kind == ObjectValueKind.Unknown) {
				strval = GettextCatalog.GetString ("The name '{0}' does not exist in the current context.", val.Name);
				canEdit = false;
			}
			else {
				canEdit = val.Kind == ObjectValueKind.Primitive;
				strval = val.Value != null ? val.Value.ToString () : "(null)";
				strval = GLib.Markup.EscapeText (strval);
			}
			
			canEdit = canEdit && allowEditing;

			TreeIter it;
			if (parent.Equals (TreeIter.Zero))
				it = store.AppendValues (val.Name, strval, val.TypeName, val, !val.HasChildren, allowAdding, canEdit);
			else
				it = store.AppendValues (parent, val.Name, strval, val.TypeName, val, !val.HasChildren, false, canEdit);
			
			if (val.HasChildren) {
				// Add dummy node
				it = store.AppendValues (it, "", "", "", null, true);
			}
		}
		
		protected override bool OnTestExpandRow (TreeIter iter, TreePath path)
		{
			bool expanded = (bool) store.GetValue (iter, ExpandedCol);
			if (!expanded) {
				store.SetValue (iter, ExpandedCol, true);
				TreeIter it;
				store.IterChildren (out it, iter);
				store.Remove (ref it);
				ObjectValue val = (ObjectValue) store.GetValue (iter, ObjectCol);
				foreach (ObjectValue cval in val.GetAllChildren ())
					AppendValue (iter, cval);
				return base.OnTestExpandRow (iter, path);
			}
			else
				return false;
		}
		
		void OnExpEdited (object s, Gtk.EditedArgs args)
		{
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			if (store.GetValue (it, ObjectCol) == null) {
				if (args.NewText.Length > 0) {
					valueNames.Add (args.NewText);
					Update ();
				}
			} else {
				string exp = (string) store.GetValue (it, NameCol);
				if (args.NewText == exp)
					return;
				int i = valueNames.IndexOf (exp);
				if (args.NewText.Length != 0)
					valueNames [i] = args.NewText;
				else
					valueNames.RemoveAt (i);
				Update ();
			}
		}
		
		void OnValueEdited (object s, Gtk.EditedArgs args)
		{
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			ObjectValue val = store.GetValue (it, ObjectCol) as ObjectValue;
			try {
				val.Value = args.NewText;
			} catch (Exception ex) {
				LoggingService.LogError ("Could not set value for object '" + val.Name + "'", ex);
			}
			store.SetValue (it, ValueCol, GLib.Markup.EscapeText (val.Value));
		}
	}
	
	public interface IValueTreeSource
	{
		ObjectValue[] GetValues (string[] name);
	}
}
