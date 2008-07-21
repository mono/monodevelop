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
using System.Xml;
using System.Text;
using System.Collections.Generic;
using Gtk;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.Debugger
{
	public class ObjectValueTreeView: Gtk.TreeView, ICompletionWidget
	{
		List<string> valueNames = new List<string> ();
		List<ObjectValue> values = new List<ObjectValue> ();
		TreeStore store;
		TreeViewState state;
		string createMsg;
		bool allowAdding;
		bool allowEditing;
		bool compact;
		StackFrame frame;
		
		CellRendererText crtExp;
		CellRendererText crtValue;
		CellRendererText crtType;
		Gtk.Entry editEntry;
		CompletionData currentCompletionData;
		
		TreeViewColumn valueCol;
		TreeViewColumn typeCol;
		
		const int NameCol = 0;
		const int ValueCol = 1;
		const int TypeCol = 2;
		const int ObjectCol = 3;
		const int ExpandedCol = 4;
		const int NameEditableCol = 5;
		const int ValueEditableCol = 6;
		const int IconCol = 7;
		const int NamePlainCol = 8;
		
		public event EventHandler StartEditing;
		public event EventHandler EndEditing;

		public ObjectValueTreeView ()
		{
			store = new TreeStore (typeof(string), typeof(string), typeof(string), typeof(ObjectValue), typeof(bool), typeof(bool), typeof(bool), typeof(string), typeof(string));
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
			col.Resizable = true;
			AppendColumn (col);
			
			valueCol = new TreeViewColumn ();
			valueCol.Expand = true;
			valueCol.Title = GettextCatalog.GetString ("Value");
			crtValue = new CellRendererText ();
			valueCol.PackStart (crtValue, true);
			valueCol.AddAttribute (crtValue, "markup", ValueCol);
			valueCol.AddAttribute (crtValue, "editable", ValueEditableCol);
			valueCol.Resizable = true;
			AppendColumn (valueCol);
			
			typeCol = new TreeViewColumn ();
			typeCol.Expand = true;
			typeCol.Title = GettextCatalog.GetString ("Type");
			crtType = new CellRendererText ();
			typeCol.PackStart (crtType, true);
			typeCol.AddAttribute (crtType, "text", TypeCol);
			typeCol.Resizable = true;
			AppendColumn (typeCol);
			
			state = new TreeViewState (this, NameCol);
			
			crtExp.Edited += OnExpEdited;
			crtExp.EditingStarted += OnExpEditing;
			crtExp.EditingCanceled += OnEditingCancelled;
			crtValue.EditingStarted += OnValueEditing;
			crtValue.Edited += OnValueEdited;
			crtValue.EditingCanceled += OnEditingCancelled;
			
			createMsg = GettextCatalog.GetString ("Click here to add a new watch");
		}
		
		public StackFrame Frame {
			get {
				return frame;
			}
			set {
				frame = value;
				Update ();
			}
		}
				
		public void SaveState ()
		{
			state.Save ();
		}
		
		public void LoadState ()
		{
			state.Load ();
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
		
		public void AddExpression (string exp)
		{
			valueNames.Add (exp);
			Update ();
		}
		
		public void AddExpressions (IEnumerable<string> exps)
		{
			valueNames.AddRange (exps);
			Update ();
		}
		
		public void RemoveExpression (string exp)
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
		
		public IEnumerable<string> Expressions {
			get { return valueNames; }
		}
		
		public void Update ()
		{
			state.Save ();
			
			store.Clear ();
			
			foreach (ObjectValue val in values)
				AppendValue (TreeIter.Zero, null, val);
			
			if (valueNames.Count > 0) {
				ObjectValue[] expValues = GetValues (valueNames.ToArray ());
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
				strval = GLib.Markup.EscapeText (strval);
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
				strval = GLib.Markup.EscapeText (strval);
			}
			
			if (name == null)
				name = val.Name;
			string nameMarkup = GLib.Markup.EscapeText (name);
			string icon = GetIcon (val.Flags);

			TreeIter it;
			if (parent.Equals (TreeIter.Zero))
				it = store.AppendValues (nameMarkup, strval, val.TypeName, val, !val.HasChildren, allowAdding, canEdit, icon, name);
			else
				it = store.AppendValues (parent, nameMarkup, strval, val.TypeName, val, !val.HasChildren, false, canEdit, icon, name);
			
			if (val.HasChildren) {
				// Add dummy node
				it = store.AppendValues (it, "", "", "", null, true);
			}
		}
		
		internal static string GetIcon (ObjectValueFlags flags)
		{
			if ((flags & ObjectValueFlags.Field) != 0 && (flags & ObjectValueFlags.ReadOnly) != 0)
				return "md-literal";
			
			string source;
			switch (flags & ObjectValueFlags.OriginMask) {
				case ObjectValueFlags.Property: source = "property"; break;
				case ObjectValueFlags.Type: source = "class"; break;
				case ObjectValueFlags.Literal: return "md-literal";
				case ObjectValueFlags.Namespace: return "md-name-space";
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
			OnStartEditing (args);
		}
		
		void OnExpEdited (object s, Gtk.EditedArgs args)
		{
			OnEndEditing ();
			
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			if (store.GetValue (it, ObjectCol) == null) {
				if (args.NewText.Length > 0) {
					valueNames.Add (args.NewText);
					Update ();
				}
			} else {
				string exp = (string) store.GetValue (it, NamePlainCol);
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
		
		void OnValueEditing (object s, Gtk.EditingStartedArgs args)
		{
			Gtk.Entry e = (Gtk.Entry) args.Editable;
			e.GrabFocus ();
			OnStartEditing (args);
		}
		
		void OnValueEdited (object s, Gtk.EditedArgs args)
		{
			OnEndEditing ();
			
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			ObjectValue val = store.GetValue (it, ObjectCol) as ObjectValue;
			try {
				string newVal = args.NewText;
/*				if (newVal == null) {
					MessageService.ShowError (GettextCatalog.GetString ("Unregognized escape sequence."));
					return;
				}
*/				val.Value = newVal;
			} catch (Exception ex) {
				LoggingService.LogError ("Could not set value for object '" + val.Name + "'", ex);
			}
			store.SetValue (it, ValueCol, GLib.Markup.EscapeText (val.Value));
		}
		
		void OnEditingCancelled (object s, EventArgs args)
		{
			OnEndEditing ();
		}
		
		void OnStartEditing (Gtk.EditingStartedArgs args)
		{
			editEntry = (Gtk.Entry) args.Editable;
			editEntry.KeyPressEvent += OnEditKeyPress;
			if (StartEditing != null)
				StartEditing (this, EventArgs.Empty);
		}
		
		void OnEndEditing ()
		{
			editEntry.KeyPressEvent -= OnEditKeyPress;
			CompletionWindowManager.HideWindow ();
			currentCompletionData = null;
			if (EndEditing != null)
				EndEditing (this, EventArgs.Empty);
		}
		
		[GLib.ConnectBeforeAttribute]
		void OnEditKeyPress (object s, Gtk.KeyPressEventArgs args)
		{
			Gtk.Entry entry = (Gtk.Entry) s;
			
			if (currentCompletionData != null) {
				bool ret = CompletionWindowManager.ProcessKeyEvent (args.Event.Key, args.Event.State);
				args.RetVal = ret;
			}
			
			Gtk.Application.Invoke (delegate {
				char c = (char) Gdk.Keyval.ToUnicode (args.Event.KeyValue);
				if (currentCompletionData == null && IsCompletionChar (c)) {
					string exp = entry.Text.Substring (0, entry.CursorPosition);
					currentCompletionData = GetCompletionData (exp);
					if (currentCompletionData != null && currentCompletionData.Items.Count > 1) {
						DebugCompletionDataProvider dataProvider = new DebugCompletionDataProvider (currentCompletionData);
						ICodeCompletionContext ctx = ((ICompletionWidget)this).CreateCodeCompletionContext (entry.CursorPosition - currentCompletionData.ExpressionLenght);
						CompletionWindowManager.ShowWindow (c, dataProvider, this, ctx, OnCompletionWindowClosed);
					} else
						currentCompletionData = null;
				}
			});
		}
		
		bool IsCompletionChar (char c)
		{
			return (char.IsLetterOrDigit (c) || char.IsPunctuation (c) || char.IsSymbol (c) || char.IsWhiteSpace (c));
		}
		
		void OnCompletionWindowClosed ()
		{
			currentCompletionData = null;
		}
		
		#region ICompletionWidget implementation 
		
		EventHandler completionContextChanged;
		
		event EventHandler ICompletionWidget.CompletionContextChanged {
			add { completionContextChanged += value; }
			remove { completionContextChanged -= value; }
		}
		
		string ICompletionWidget.GetText (int startOffset, int endOffset)
		{
			if (startOffset < 0) startOffset = 0;
			if (endOffset > editEntry.Text.Length) endOffset = editEntry.Text.Length;
			return editEntry.Text.Substring (startOffset, endOffset - startOffset);
		}
		
		char ICompletionWidget.GetChar (int offset)
		{
			string txt = editEntry.Text;
			if (offset >= txt.Length)
				return (char)0;
			else
				return txt [offset];
		}
		
		CodeCompletionContext ICompletionWidget.CreateCodeCompletionContext (int triggerOffset)
		{
			CodeCompletionContext c = new CodeCompletionContext ();
			c.TriggerLine = 0;
			c.TriggerOffset = triggerOffset;
			c.TriggerLineOffset = c.TriggerOffset;
			c.TriggerTextHeight = editEntry.SizeRequest ().Height;
			c.TriggerWordLength = currentCompletionData.ExpressionLenght;
			
			int x, y;
			int tx, ty;
			editEntry.GdkWindow.GetOrigin (out x, out y);
			editEntry.GetLayoutOffsets (out tx, out ty);
			int cp = editEntry.TextIndexToLayoutIndex (editEntry.Position);
			Pango.Rectangle rect = editEntry.Layout.IndexToPos (cp);
			tx += Pango.Units.ToPixels (rect.X) + x;
			y += editEntry.Allocation.Height;
				
			c.TriggerXCoord = tx;
			c.TriggerYCoord = y;
			return c;
		}
		
		string ICompletionWidget.GetCompletionText (ICodeCompletionContext ctx)
		{
			return editEntry.Text.Substring (ctx.TriggerOffset, ctx.TriggerWordLength);
		}
		
		void ICompletionWidget.SetCompletionText (ICodeCompletionContext ctx, string partial_word, string complete_word)
		{
			int sp = editEntry.Position - partial_word.Length;
			editEntry.DeleteText (sp, sp + partial_word.Length);
			editEntry.InsertText (complete_word, ref sp);
			editEntry.Position = sp; // sp is incremented by InsertText
		}
		
		int ICompletionWidget.TextLength {
			get {
				return editEntry.Text.Length;
			}
		}
		
		int ICompletionWidget.SelectedLength {
			get {
				return 0;
			}
		}
		
		Style ICompletionWidget.GtkStyle {
			get {
				return editEntry.Style;
			}
		}
		#endregion 

		ObjectValue[] GetValues (string[] names)
		{
			if (frame != null)
				return frame.GetExpressionValues (names, true);
			else {
				ObjectValue[] vals = new ObjectValue [names.Length];
				for (int n=0; n<vals.Length; n++)
					vals [n] = ObjectValue.CreateUnknown (names [n]);
				return vals;
			}
		}
		
		CompletionData GetCompletionData (string exp)
		{
			if (frame != null)
				return frame.GetExpressionCompletionData (exp);
			else
				return null;
		}
	}
	
	class DebugCompletionDataProvider: ICompletionDataProvider
	{
		CompletionData data;
		
		public DebugCompletionDataProvider (CompletionData data)
		{
			this.data = data;
		}
		
		public void Dispose ()
		{
		}

		public ICompletionData[] GenerateCompletionData (ICompletionWidget widget, char charTyped)
		{
			List<ICompletionData> list = new List<ICompletionData> ();
			foreach (CompletionItem it in data.Items)
				list.Add (new DebugCompletionData (it));
			return list.ToArray ();
		}
		
		public string DefaultCompletionString {
			get {
				return string.Empty;
			}
		}
	}
	
	class DebugCompletionData: ICompletionData
	{
		CompletionItem item;
		
		public DebugCompletionData (CompletionItem item)
		{
			this.item = item;
		}
		
		public string Image {
			get {
				return ObjectValueTreeView.GetIcon (item.Flags);
			}
		}
		
		public string[] Text {
			get {
				return new string [] { item.Name };
			}
		}
		
		public string Description {
			get {
				return string.Empty;
			}
		}
		
		public string CompletionString {
			get {
				return item.Name;
			}
		}
	}
}
