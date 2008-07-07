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
using System.Text;
using System.Collections.Generic;
using Gtk;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;

namespace MonoDevelop.Debugger
{
	public class ObjectValueTreeView: Gtk.TreeView
	{
		List<string> valueNames = new List<string> ();
		List<ObjectValue> values = new List<ObjectValue> ();
		IValueTreeSource source;
		TreeStore store;
		TreeViewState state;
		string createMsg;
		bool allowAdding;
		bool allowEditing;
		bool compact;
		
		CellRendererText crtExp;
		CellRendererText crtValue;
		CellRendererText crtType;
		
		const int NameCol = 0;
		const int ValueCol = 1;
		const int TypeCol = 2;
		const int ObjectCol = 3;
		const int ExpandedCol = 4;
		const int NameEditableCol = 5;
		const int ValueEditableCol = 6;
		const int IconCol = 7;
		
		public ObjectValueTreeView ()
		{
			store = new TreeStore (typeof(string), typeof(string), typeof(string), typeof(ObjectValue), typeof(bool), typeof(bool), typeof(bool), typeof(string));
			Model = store;
			RulesHint = true;
			
			Pango.FontDescription newFont = this.Style.FontDescription.Copy ();
			newFont.Size = (newFont.Size * 8) / 10;
			
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Name");
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock_id", IconCol);
			crtExp = new CellRendererText ();
			col.PackStart (crtExp, true);
			col.AddAttribute (crtExp, "markup", NameCol);
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
			crtType = new CellRendererText ();
			col.PackStart (crtType, true);
			col.AddAttribute (crtType, "text", TypeCol);
			AppendColumn (col);
			
			state = new TreeViewState (this, NameCol);
			
			crtExp.Edited += OnExpEdited;
			crtExp.EditingStarted += OnExpEditing;
			crtValue.Edited += OnValueEdited;
			
			createMsg = GettextCatalog.GetString ("Click here to add a new watch");
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
		
		public bool CompactView {
			get {
				return compact; 
			}
			set {
				compact = value;
				Pango.FontDescription newFont;
				if (compact) {
					newFont = this.Style.FontDescription.Copy ();
					newFont.Size = (newFont.Size * 8) / 10;
				} else {
					newFont = this.Style.FontDescription;
				}
				crtExp.FontDesc = newFont;
				crtValue.FontDesc = newFont;
				crtType.FontDesc = newFont;
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
		
		public void AddValue (ObjectValue value)
		{
			values.Add (value);
			Update ();
		}
		
		public void RemoveValue (ObjectValue value)
		{
			values.Remove (value);
			Update ();
		}
		
		public void Update ()
		{
			state.Save ();
			
			store.Clear ();
			
			foreach (ObjectValue val in values)
				AppendValue (TreeIter.Zero, null, val);
			
			if (source != null) {
				ObjectValue[] expValues = source.GetValues (valueNames.ToArray ());
				for (int n=0; n<expValues.Length; n++)
					AppendValue (TreeIter.Zero, valueNames [n], expValues [n]);
			}
			
			if (AllowAdding)
				store.AppendValues ("<span color='gray'>" + createMsg + "</span>", "", "", null, true, true);
			
			state.Load ();
		}
		
		void AppendValue (TreeIter parent, string name, ObjectValue val)
		{
			string strval;
			bool canEdit;
			if (val.IsUnknown) {
				strval = GettextCatalog.GetString ("The name '{0}' does not exist in the current context.", val.Name);
				canEdit = false;
			}
			else if (val.IsError) {
				strval = val.Value;
				int i = strval.IndexOf ('\n');
				if (i != -1)
					strval = strval.Substring (0, i);
				strval = "<span color='red'>" + GLib.Markup.EscapeText (strval) + "</span>";
				canEdit = false;
			}
			else {
				canEdit = val.IsPrimitive && !val.IsReadOnly && allowEditing;
				strval = val.Value != null ? val.Value.ToString () : "(null)";
				if (canEdit) strval = Escape (strval);
				strval = GLib.Markup.EscapeText (strval);
			}
			
			if (name == null)
				name = val.Name;
			name = GLib.Markup.EscapeText (name);
			string icon = GetIcon (val.Flags);

			TreeIter it;
			if (parent.Equals (TreeIter.Zero))
				it = store.AppendValues (name, strval, val.TypeName, val, !val.HasChildren, allowAdding, canEdit, icon);
			else
				it = store.AppendValues (parent, name, strval, val.TypeName, val, !val.HasChildren, false, canEdit, icon);
			
			if (val.HasChildren) {
				// Add dummy node
				it = store.AppendValues (it, "", "", "", null, true);
			}
		}
		
		string GetIcon (ObjectValueFlags flags)
		{
			if ((flags & ObjectValueFlags.Field) != 0 && (flags & ObjectValueFlags.ReadOnly) != 0)
				return "md-literal";
			
			string source;
			switch (flags & ObjectValueFlags.OriginMask) {
				case ObjectValueFlags.Property: source = "property"; break;
				case ObjectValueFlags.Literal: return "md-literal";
				default: source = "field"; break;
			}
			string access;
			switch (flags & ObjectValueFlags.AccessMask) {
				case ObjectValueFlags.Private: access = "-private-"; break;
				case ObjectValueFlags.Internal: access = "-internal-"; break;
				case ObjectValueFlags.InternalProtected:
				case ObjectValueFlags.Protected: access = "-protected-"; break;
				default: access = "-"; break;
			}
			
			return "md" + access + source;
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
					AppendValue (iter, null, cval);
				return base.OnTestExpandRow (iter, path);
			}
			else
				return false;
		}
		
		protected override void OnRowCollapsed (TreeIter iter, TreePath path)
		{
			base.OnRowCollapsed (iter, path);
			if (compact)
				ColumnsAutosize ();
		}
		
		protected override void OnRowExpanded (TreeIter iter, TreePath path)
		{
			base.OnRowExpanded (iter, path);
			if (compact)
				ColumnsAutosize ();
		}

		void OnExpEditing (object s, Gtk.EditingStartedArgs args)
		{
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			Gtk.Entry e = (Gtk.Entry) args.Editable;
			if (e.Text == createMsg)
				e.Text = string.Empty;
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
				string newVal = Unscape (args.NewText);
				if (newVal == null) {
					MessageService.ShowError (GettextCatalog.GetString ("Unregognized escape sequence."));
					return;
				}
				val.Value = newVal;
			} catch (Exception ex) {
				LoggingService.LogError ("Could not set value for object '" + val.Name + "'", ex);
			}
			store.SetValue (it, ValueCol, GLib.Markup.EscapeText (Escape (val.Value)));
		}
		
		public static string Unscape (string text)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				if (c != '\\') {
					sb.Append (c);
					continue;
				}
				i++;
				if (i >= text.Length)
					return null;
				
				switch (text[i]) {
					case '\\': c = '\\'; break;
					case 'a': c = '\a'; break;
					case 'b': c = '\b'; break;
					case 'f': c = '\f'; break;
					case 'v': c = '\v'; break;
					case 'n': c = '\n'; break;
					case 'r': c = '\r'; break;
					case 't': c = '\t'; break;
					default: return null;
				}
				sb.Append (c);
			}
			return sb.ToString ();
		}
		
		public static string Escape (string text)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				string txt;
				switch (c) {
					case '\\': txt = @"\\"; break;
					case '\a': txt = @"\a"; break;
					case '\b': txt = @"\b"; break;
					case '\f': txt = @"\f"; break;
					case '\v': txt = @"\v"; break;
					case '\n': txt = @"\n"; break;
					case '\r': txt = @"\r"; break;
					case '\t': txt = @"\t"; break;
					default: 
						sb.Append (c);
						continue;
				}
				sb.Append (txt);
			}
			return sb.ToString ();
		}
	}
	
	public interface IValueTreeSource
	{
		ObjectValue[] GetValues (string[] name);
	}
}
