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
using System.Text;
using Gtk;
using Mono.Debugging.Client;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using Mono.TextEditor;


namespace MonoDevelop.Debugger
{
	[System.ComponentModel.ToolboxItem (true)]
	public class ObjectValueTreeView: Gtk.TreeView, ICompletionWidget
	{
		List<string> valueNames = new List<string> ();
		Dictionary<string,string> oldValues = new Dictionary<string,string> ();
		List<ObjectValue> values = new List<ObjectValue> ();
		Dictionary<ObjectValue,TreeRowReference> nodes = new Dictionary<ObjectValue, TreeRowReference> ();
		Dictionary<string,ObjectValue> cachedValues = new Dictionary<string,ObjectValue> ();
		TreeStore store;
		TreeViewState state;
		string createMsg;
		bool allowAdding;
		bool allowEditing;
		bool allowExpanding = true;
		bool restoringState = false;
		bool compact;
		StackFrame frame;
		bool disposed;
		Gdk.Pixbuf noLiveIcon;
		Gdk.Pixbuf liveIcon;
		
		bool columnsAdjusted;
		bool columnSizesUpdating;
		bool allowStoreColumnSizes;
		double expColWidth;
		double valueColWidth;
		double typeColWidth;
		
		CellRendererText crtExp;
		CellRendererText crtValue;
		CellRendererText crtType;
		CellRendererIcon crpButton;
		CellRendererIcon crpPin;
		CellRendererIcon crpLiveUpdate;
		CellRendererIcon crpViewer;
		Gtk.Entry editEntry;
		Mono.Debugging.Client.CompletionData currentCompletionData;
		
		TreeViewColumn expCol;
		TreeViewColumn valueCol;
		TreeViewColumn typeCol;
		TreeViewColumn pinCol;
		
		string errorColor = "red";
		string modifiedColor = "blue";
		string disabledColor = "gray";
		
		static CommandEntrySet menuSet;
		
		const int NameCol = 0;
		const int ValueCol = 1;
		const int TypeCol = 2;
		const int ObjectCol = 3;
		const int ExpandedCol = 4;
		const int NameEditableCol = 5;
		const int ValueEditableCol = 6;
		const int IconCol = 7;
		const int NameColorCol = 8;
		const int ValueColorCol = 9;
		const int ValueButtonVisibleCol = 10;
		const int PinIconCol = 11;
		const int LiveUpdateIconCol = 12;
		const int ViewerButtonVisibleCol = 13;
		
		public event EventHandler StartEditing;
		public event EventHandler EndEditing;
		public event EventHandler PinStatusChanged;

		enum LocalCommands
		{
			AddWatch
		}
		
		static ObjectValueTreeView ()
		{
			// Context menu definition
			
			menuSet = new CommandEntrySet ();
			menuSet.AddItem (DebugCommands.AddWatch);
			menuSet.AddSeparator ();
			menuSet.AddItem (EditCommands.Copy);
			menuSet.AddItem (EditCommands.Rename);
			menuSet.AddItem (EditCommands.DeleteKey);
		}
		
		public ObjectValueTreeView ()
		{
			store = new TreeStore (typeof(string), typeof(string), typeof(string), typeof(ObjectValue), typeof(bool), typeof(bool), typeof(bool), typeof(string), typeof(string), typeof(string), typeof(bool), typeof(string), typeof(Gdk.Pixbuf), typeof(bool));
			Model = store;
			RulesHint = true;
			EnableSearch = false;
			Selection.Mode = Gtk.SelectionMode.Multiple;
			ResetColumnSizes ();
			
			Pango.FontDescription newFont = this.Style.FontDescription.Copy ();
			newFont.Size = (newFont.Size * 8) / 10;
			
			liveIcon = ImageService.GetPixbuf (Gtk.Stock.Execute, IconSize.Menu);
			noLiveIcon = ImageService.MakeTransparent (liveIcon, 0.5);
			
			expCol = new TreeViewColumn ();
			expCol.Title = GettextCatalog.GetString ("Name");
			CellRendererIcon crp = new CellRendererIcon ();
			expCol.PackStart (crp, false);
			expCol.AddAttribute (crp, "stock_id", IconCol);
			crtExp = new CellRendererText ();
			expCol.PackStart (crtExp, true);
			expCol.AddAttribute (crtExp, "text", NameCol);
			expCol.AddAttribute (crtExp, "editable", NameEditableCol);
			expCol.AddAttribute (crtExp, "foreground", NameColorCol);
			expCol.Resizable = true;
			expCol.Sizing = TreeViewColumnSizing.Fixed;
			expCol.MinWidth = 15;
			expCol.AddNotification ("width", OnColumnWidthChanged);
//			expCol.Expand = true;
			AppendColumn (expCol);
			
			valueCol = new TreeViewColumn ();
			valueCol.Title = GettextCatalog.GetString ("Value");
			crpViewer = new CellRendererIcon ();
			crpViewer.IconId = Gtk.Stock.ZoomIn;
			valueCol.PackStart (crpViewer, false);
			valueCol.AddAttribute (crpViewer, "visible", ViewerButtonVisibleCol);
			crpButton = new CellRendererIcon ();
			crpButton.StockSize = (uint)Gtk.IconSize.Menu;
			crpButton.IconId = Gtk.Stock.Refresh;
			valueCol.PackStart (crpButton, false);
			valueCol.AddAttribute (crpButton, "visible", ValueButtonVisibleCol);
			crtValue = new CellRendererText ();
			valueCol.PackStart (crtValue, true);
			valueCol.AddAttribute (crtValue, "text", ValueCol);
			valueCol.AddAttribute (crtValue, "editable", ValueEditableCol);
			valueCol.AddAttribute (crtValue, "foreground", ValueColorCol);
			valueCol.Resizable = true;
			valueCol.MinWidth = 15;
			valueCol.AddNotification ("width", OnColumnWidthChanged);
//			valueCol.Expand = true;
			valueCol.Sizing = TreeViewColumnSizing.Fixed;
			AppendColumn (valueCol);
			
			typeCol = new TreeViewColumn ();
			typeCol.Title = GettextCatalog.GetString ("Type");
			crtType = new CellRendererText ();
			typeCol.PackStart (crtType, true);
			typeCol.AddAttribute (crtType, "text", TypeCol);
			typeCol.Resizable = true;
			typeCol.Sizing = TreeViewColumnSizing.Fixed;
			typeCol.MinWidth = 15;
			typeCol.AddNotification ("width", OnColumnWidthChanged);
//			typeCol.Expand = true;
			AppendColumn (typeCol);
			
			pinCol = new TreeViewColumn ();
			crpPin = new CellRendererIcon ();
			pinCol.PackStart (crpPin, false);
			pinCol.AddAttribute (crpPin, "stock_id", PinIconCol);
			crpLiveUpdate = new CellRendererIcon ();
			pinCol.PackStart (crpLiveUpdate, false);
			pinCol.AddAttribute (crpLiveUpdate, "pixbuf", LiveUpdateIconCol);
			pinCol.Resizable = false;
			pinCol.Visible = false;
			pinCol.Expand = false;
			AppendColumn (pinCol);
			
			state = new TreeViewState (this, NameCol);
			
			crtExp.Edited += OnExpEdited;
			crtExp.EditingStarted += OnExpEditing;
			crtExp.EditingCanceled += OnEditingCancelled;
			crtValue.EditingStarted += OnValueEditing;
			crtValue.Edited += OnValueEdited;
			crtValue.EditingCanceled += OnEditingCancelled;
			
			this.EnableAutoTooltips ();
			
			createMsg = GettextCatalog.GetString ("Click here to add a new watch");
			CompletionWindowManager.WindowClosed += HandleCompletionWindowClosed;
		}

		void HandleCompletionWindowClosed (object sender, EventArgs e)
		{
			currentCompletionData = null;
		}

		protected override void OnDestroyed ()
		{
			CompletionWindowManager.WindowClosed -= HandleCompletionWindowClosed;
			crtExp.Edited -= OnExpEdited;
			crtExp.EditingStarted -= OnExpEditing;
			crtExp.EditingCanceled -= OnEditingCancelled;
			crtValue.EditingStarted -= OnValueEditing;
			crtValue.Edited -= OnValueEdited;
			crtValue.EditingCanceled -= OnEditingCancelled;
			
			base.OnDestroyed ();
			disposed = true;
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			AdjustColumnSizes ();
		}
		
		protected override void OnShown ()
		{
			base.OnShown ();
			AdjustColumnSizes ();
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			AdjustColumnSizes ();
		}

		void OnColumnWidthChanged (object o, GLib.NotifyArgs args)
		{
			if (!columnSizesUpdating && allowStoreColumnSizes) {
				StoreColumnSizes ();
			}
		}

		void AdjustColumnSizes ()
		{
			if (!Visible || Allocation.Width <= 0 || columnSizesUpdating || compact)
				return;
			
			columnSizesUpdating = true;
			
			double width = (double) Allocation.Width;
			
			int texp = Math.Max ((int) (width * expColWidth), 1);
			if (texp != expCol.FixedWidth) {
				expCol.FixedWidth = texp;
			}
			
			int ttype = 0;
			if (typeCol.Visible) {
				ttype = Math.Max ((int) (width * typeColWidth), 1);
				if (ttype != typeCol.FixedWidth) {
					typeCol.FixedWidth = ttype;
				}
			}
			
			int tval = Math.Max ((int) (width * valueColWidth), 1);

			if (tval != valueCol.FixedWidth) {
				valueCol.FixedWidth = tval;
				Application.Invoke (delegate { QueueResize (); });
			}
			
			columnSizesUpdating = false;
			columnsAdjusted = true;
		}
		
		void StoreColumnSizes ()
		{
			if (!IsRealized || !Visible || !columnsAdjusted || compact)
				return;
			
			double width = (double) Allocation.Width;
			expColWidth = ((double) expCol.Width) / width;
			valueColWidth = ((double) valueCol.Width) / width;
			if (typeCol.Visible)
				typeColWidth = ((double) typeCol.Width) / width;
		}
		
		void ResetColumnSizes ()
		{
			expColWidth = 0.3;
			valueColWidth = 0.5;
			typeColWidth = 0.2;
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
			restoringState = true;
			state.Load ();
			restoringState = false;
		}
		
		public bool AllowAdding {
			get {
				return allowAdding;
			}
			set {
				allowAdding = value;
				Refresh ();
			}
		}
		
		public bool AllowEditing {
			get {
				return allowEditing;
			}
			set {
				allowEditing = value;
				Refresh ();
			}
		}
		
		public bool AllowPinning {
			get { return pinCol.Visible; }
			set { pinCol.Visible = value; }
		}
		
		public bool RootPinAlwaysVisible { get; set; }
		
		public bool AllowExpanding {
			get { return this.allowExpanding; }
			set { this.allowExpanding = value; }
		}
		
		
		public PinnedWatch PinnedWatch { get; set; }
		
		public string PinnedWatchFile { get; set; }
		public int PinnedWatchLine { get; set; }
		
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
					expCol.Sizing = TreeViewColumnSizing.Autosize;
					valueCol.Sizing = TreeViewColumnSizing.Autosize;
					valueCol.MaxWidth = 800;
					crpButton.Pixbuf = ImageService.GetPixbuf (Gtk.Stock.Refresh).ScaleSimple (12, 12, Gdk.InterpType.Hyper);
					crpViewer.Pixbuf = ImageService.GetPixbuf (Gtk.Stock.ZoomIn).ScaleSimple (12, 12, Gdk.InterpType.Hyper);
					ColumnsAutosize ();
				} else {
					newFont = this.Style.FontDescription;
					expCol.Sizing = TreeViewColumnSizing.Fixed;
					valueCol.Sizing = TreeViewColumnSizing.Fixed;
					valueCol.MaxWidth = int.MaxValue;
				}
				typeCol.Visible = !compact;
				crtExp.FontDesc = newFont;
				crtValue.FontDesc = newFont;
				crtType.FontDesc = newFont;
				ResetColumnSizes ();
				AdjustColumnSizes ();
			}
		}
		
		public void AddExpression (string exp)
		{
			valueNames.Add (exp);
			Refresh ();
		}
		
		public void AddExpressions (IEnumerable<string> exps)
		{
			valueNames.AddRange (exps);
			Refresh ();
		}
		
		public void RemoveExpression (string exp)
		{
			cachedValues.Remove (exp);
			valueNames.Remove (exp);
			Refresh ();
		}
		
		public void AddValue (ObjectValue value)
		{
			values.Add (value);
			Refresh ();
		}
		
		public void AddValues (IEnumerable<ObjectValue> newValues)
		{
			foreach (ObjectValue val in newValues)
				values.Add (val);
			Refresh ();
		}
		
		public void RemoveValue (ObjectValue value)
		{
			values.Remove (value);
			Refresh ();
		}

		public void ReplaceValue (ObjectValue old, ObjectValue @new)
		{
			int idx = values.IndexOf (old);
			if (idx == -1)
				return;

			values [idx] = @new;
			Refresh ();
		}
		
		public void ClearValues ()
		{
			values.Clear ();
			Refresh ();
		}
		
		public void ClearExpressions ()
		{
			valueNames.Clear ();
			Update ();
		}
		
		public IEnumerable<string> Expressions {
			get { return valueNames; }
		}
		
		public void Update ()
		{
			cachedValues.Clear ();
			Refresh ();
		}
		
		public void Refresh ()
		{
			foreach (ObjectValue val in new List<ObjectValue> (nodes.Keys))
				UnregisterValue (val);
			nodes.Clear ();
			
			SaveState ();
			
			CleanPinIcon ();
			store.Clear ();
			
			bool showExpanders = AllowAdding;

			foreach (ObjectValue val in values) {
				AppendValue (TreeIter.Zero, null, val);
				if (val.HasChildren)
					showExpanders = true;
			}
			
			if (valueNames.Count > 0) {
				ObjectValue[] expValues = GetValues (valueNames.ToArray ());
				for (int n=0; n<expValues.Length; n++) {
					AppendValue (TreeIter.Zero, valueNames [n], expValues [n]);
					if (expValues [n].HasChildren)
						showExpanders = true;
				}
			}

			if (showExpanders)
				ShowExpanders = true;
			
			if (AllowAdding)
				store.AppendValues (createMsg, "", "", null, true, true, null, disabledColor, disabledColor);

			LoadState ();
		}
		
		void RefreshRow (TreeIter it)
		{
			ObjectValue val = (ObjectValue) store.GetValue (it, ObjectCol);
			UnregisterValue (val);
			
			RemoveChildren (it);
			TreeIter parent;
			if (!store.IterParent (out parent, it))
				parent = TreeIter.Zero;
			
			EvaluationOptions ops = frame.DebuggerSession.Options.EvaluationOptions.Clone ();
			ops.AllowMethodEvaluation = true;
			ops.AllowToStringCalls = true;
			ops.AllowTargetInvoke = true;
			ops.EllipsizeStrings = false;
			
			string oldName = val.Name;
			val.Refresh (ops);
			
			// Don't update the name for the values entered by the user
			if (store.IterDepth (it) == 0)
				val.Name = oldName;
			
			SetValues (parent, it, val.Name, val);
			RegisterValue (val, it);
		}
		
		void RemoveChildren (TreeIter it)
		{
			TreeIter cit;
			while (store.IterChildren (out cit, it)) {
				ObjectValue val = (ObjectValue) store.GetValue (cit, ObjectCol);
				if (val != null)
					UnregisterValue (val);
				RemoveChildren (cit);
				store.Remove (ref cit);
			}
		}

		void RegisterValue (ObjectValue val, TreeIter it)
		{
			if (val.IsEvaluating) {
				nodes [val] = new TreeRowReference (store, store.GetPath (it));
				val.ValueChanged += OnValueUpdated;
			}
		}

		void UnregisterValue (ObjectValue val)
		{
			val.ValueChanged -= OnValueUpdated;
			nodes.Remove (val);
		}

		void OnValueUpdated (object o, EventArgs a)
		{
			Application.Invoke (delegate {
				if (disposed)
					return;
				ObjectValue val = (ObjectValue) o;
				TreeIter it;
				if (FindValue (val, out it)) {
					// Keep the expression name entered by the user
					if (store.IterDepth (it) == 0)
						val.Name = (string) store.GetValue (it, NameCol);
					RemoveChildren (it);
					TreeIter parent;
					if (!store.IterParent (out parent, it))
						parent = TreeIter.Zero;
					
					// If it was an evaluating group, replace the node with the new nodes
					if (val.IsEvaluatingGroup) {
						if (val.ArrayCount == 0) {
							store.Remove (ref it);
						} else {
							SetValues (parent, it, null, val.GetArrayItem (0));
							RegisterValue (val, it);
							for (int n=1; n<val.ArrayCount; n++) {
								TreeIter cit = store.InsertNodeAfter (it);
								ObjectValue cval = val.GetArrayItem (n);
								SetValues (parent, cit, null, cval);
								RegisterValue (cval, cit);
							}
						}
					} else {
						SetValues (parent, it, val.Name, val);
					}
				}
				UnregisterValue (val);
			});
		}

		bool FindValue (ObjectValue val, out TreeIter it)
		{
			TreeRowReference row;
			
			if (!nodes.TryGetValue (val, out row)) {
				it = TreeIter.Zero;
				return false;
			}
			
			return store.GetIter (out it, row.Path);
		}
		
		public void ResetChangeTracking ()
		{
			oldValues.Clear ();
		}
		
		public void ChangeCheckpoint ()
		{
			oldValues.Clear ();
			
			TreeIter it;
			if (!store.GetIterFirst (out it))
				return;
			
			ChangeCheckpoint (it, "/");
		}
		
		void ChangeCheckpoint (TreeIter it, string path)
		{
			do {
				string name = (string) store.GetValue (it, NameCol);
				string val = (string) store.GetValue (it, ValueCol);
				oldValues [path + name] = val;
				TreeIter cit;
				if (store.IterChildren (out cit, it))
					ChangeCheckpoint (cit, name + "/");
			} while (store.IterNext (ref it));
		}
		
		void AppendValue (TreeIter parent, string name, ObjectValue val)
		{
			TreeIter it;
			if (parent.Equals (TreeIter.Zero))
				it = store.AppendNode ();
			else
				it = store.AppendNode (parent);
			SetValues (parent, it, name, val);
			RegisterValue (val, it);
		}
		
		void SetValues (TreeIter parent, TreeIter it, string name, ObjectValue val)
		{
			string strval;
			bool canEdit;
			string nameColor = null;
			string valueColor = null;
			string valueButton = null;
			
			if (name == null)
				name = val.Name;

			bool hasParent = !parent.Equals (TreeIter.Zero);
			
			string valPath;
			if (!hasParent)
				valPath = "/" + name;
			else
				valPath = GetIterPath (parent) + "/" + name;
			
			string oldValue;
			oldValues.TryGetValue (valPath, out oldValue);
			
			if (val.IsUnknown) {
				strval = GettextCatalog.GetString ("The name '{0}' does not exist in the current context.", val.Name);
				nameColor = disabledColor;
				canEdit = false;
			}
			else if (val.IsError) {
				strval = val.Value;
				int i = strval.IndexOf ('\n');
				if (i != -1)
					strval = strval.Substring (0, i);
				valueColor = errorColor;
				canEdit = false;
			}
			else if (val.IsNotSupported) {
				strval = val.Value;
				valueColor = disabledColor;
				if (val.CanRefresh)
					valueButton = Gtk.Stock.Refresh;
				canEdit = false;
			}
			else if (val.IsEvaluating) {
				strval = GettextCatalog.GetString ("Evaluating...");
				valueColor = disabledColor;
				if (val.IsEvaluatingGroup) {
					nameColor = disabledColor;
					name = val.Name;
				}
				canEdit = false;
			}
			else {
				canEdit = val.IsPrimitive && !val.IsReadOnly && allowEditing;
				strval = val.DisplayValue ?? "(null)";
				if (oldValue != null && strval != oldValue)
					nameColor = valueColor = modifiedColor;
			}
			
			strval = strval.Replace (Environment.NewLine, " ");
			
			bool showViewerButton = DebuggingService.HasValueVisualizers (val);

			bool hasChildren = val.HasChildren;
			string icon = GetIcon (val.Flags);

			store.SetValue (it, NameCol, name);
			store.SetValue (it, ValueCol, strval);
			store.SetValue (it, TypeCol, val.TypeName);
			store.SetValue (it, ObjectCol, val);
			store.SetValue (it, ExpandedCol, !hasChildren);
			store.SetValue (it, NameEditableCol, !hasParent && allowAdding);
			store.SetValue (it, ValueEditableCol, canEdit);
			store.SetValue (it, IconCol, icon);
			store.SetValue (it, NameColorCol, nameColor);
			store.SetValue (it, ValueColorCol, valueColor);
			store.SetValue (it, ValueButtonVisibleCol, valueButton != null);
			store.SetValue (it, ViewerButtonVisibleCol, showViewerButton);
			
			if (!hasParent && PinnedWatch != null) {
				store.SetValue (it, PinIconCol, "md-pin-down");
				if (PinnedWatch.LiveUpdate)
					store.SetValue (it, LiveUpdateIconCol, liveIcon);
				else
					store.SetValue (it, LiveUpdateIconCol, noLiveIcon);
			}
			if (RootPinAlwaysVisible && (!hasParent && PinnedWatch ==null && AllowPinning))
				store.SetValue (it, PinIconCol, "md-pin-up");
			
			if (hasChildren) {
				// Add dummy node
				it = store.AppendValues (it, "", "", "", null, true);
				if (!ShowExpanders)
					ShowExpanders = true;
			}
		}
		
		public static string GetIcon (ObjectValueFlags flags)
		{
			if ((flags & ObjectValueFlags.Field) != 0 && (flags & ObjectValueFlags.ReadOnly) != 0)
				return "md-literal";
			
			string source;
			string stic = (flags & ObjectValueFlags.Global) != 0 ? "static-" : string.Empty;
			
			switch (flags & ObjectValueFlags.OriginMask) {
			case ObjectValueFlags.Property: source = "property"; break;
			case ObjectValueFlags.Type: source = "class"; stic = string.Empty; break;
			case ObjectValueFlags.Literal: return "md-literal";
			case ObjectValueFlags.Namespace: return "md-name-space";
			case ObjectValueFlags.Group: return "md-open-resource-folder";
			case ObjectValueFlags.Field: source = "field"; break;
			default: return "md-empty";
			}

			string access;
			switch (flags & ObjectValueFlags.AccessMask) {
			case ObjectValueFlags.Private: access = "private-"; break;
			case ObjectValueFlags.Internal: access = "internal-"; break;
			case ObjectValueFlags.InternalProtected:
			case ObjectValueFlags.Protected: access = "protected-"; break;
			default: access = string.Empty; break;
			}
			
			return "md-" + access + stic + source;
		}
		
		protected override bool OnTestExpandRow (TreeIter iter, TreePath path)
		{
			if (!restoringState) {
				if (!allowExpanding)
					return true;

				if (GetRowExpanded (path))
					return true;

				TreeIter parent;
				if (store.IterParent (out parent, iter)) {
					if (!GetRowExpanded (store.GetPath (parent)))
						return true;
				}
			}
			
			return base.OnTestExpandRow (iter, path);
		}
		
		protected override void OnRowCollapsed (TreeIter iter, TreePath path)
		{
			store.SetValue (iter, ExpandedCol, false);
			base.OnRowCollapsed (iter, path);
			if (compact)
				ColumnsAutosize ();
		}
		
		protected override void OnRowExpanded (TreeIter iter, TreePath path)
		{
			store.SetValue (iter, ExpandedCol, true);
			TreeIter it;
			
			if (store.IterChildren (out it, iter)) {
				ObjectValue val = (ObjectValue) store.GetValue (it, ObjectCol);
				if (val == null) {
					val = (ObjectValue) store.GetValue (iter, ObjectCol);
					bool first = true;
					
					foreach (ObjectValue cval in val.GetAllChildren ()) {
						SetValues (iter, it, null, cval);
						RegisterValue (cval, it);
						it = store.InsertNodeAfter (it);
					}
					
					store.Remove (ref it);
				}
			}
			
			base.OnRowExpanded (iter, path);
			
			if (compact)
				ColumnsAutosize ();
		}
		
		string GetIterPath (TreeIter iter)
		{
			StringBuilder sb = new StringBuilder ();
			do {
				string name = (string) store.GetValue (iter, NameCol);
				sb.Insert (0, "/" + name);
			} while (store.IterParent (out iter, iter));
			return sb.ToString ();
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
					Refresh ();
				}
			} else {
				string exp = (string) store.GetValue (it, NameCol);
				if (args.NewText == exp)
					return;
				
				int i = valueNames.IndexOf (exp);
				if (i == -1)
					return;

				if (args.NewText.Length != 0)
					valueNames [i] = args.NewText;
				else
					valueNames.RemoveAt (i);
				
				cachedValues.Remove (exp);
				Refresh ();
			}
		}
		
		bool editing;
		
		void OnValueEditing (object s, Gtk.EditingStartedArgs args)
		{
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			
			Gtk.Entry e = (Gtk.Entry) args.Editable;
			
			ObjectValue val = store.GetValue (it, ObjectCol) as ObjectValue;
			string strVal = val.Value;
			if (!string.IsNullOrEmpty (strVal))
				e.Text = strVal;
			
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
*/				if (val.Value != newVal)
					val.Value = newVal;
			} catch (Exception ex) {
				LoggingService.LogError ("Could not set value for object '" + val.Name + "'", ex);
			}
			store.SetValue (it, ValueCol, val.DisplayValue);

			// Update the color
			
			string newColor = null;
			
			string valPath = GetIterPath (it);
			string oldValue;
			if (oldValues.TryGetValue (valPath, out oldValue)) {
				if (oldValue != val.Value)
					newColor = modifiedColor;
			}
			
			store.SetValue (it, NameColorCol, newColor);
			store.SetValue (it, ValueColorCol, newColor);
		}
		
		void OnEditingCancelled (object s, EventArgs args)
		{
			OnEndEditing ();
		}
		
		void OnStartEditing (Gtk.EditingStartedArgs args)
		{
			editing = true;
			editEntry = (Gtk.Entry) args.Editable;
			editEntry.KeyPressEvent += OnEditKeyPress;
			editEntry.KeyReleaseEvent += HandleChanged;
			if (StartEditing != null)
				StartEditing (this, EventArgs.Empty);
		}

		void HandleChanged (object sender, EventArgs e)
		{
			Gtk.Entry entry = (Gtk.Entry)sender;
			if (!wasHandled) {
				string text = ctx == null ? editEntry.Text : editEntry.Text.Substring (Math.Max (0, Math.Min (ctx.TriggerOffset, editEntry.Text.Length)));
				CompletionWindowManager.UpdateWordSelection (text);
				CompletionWindowManager.PostProcessKeyEvent (key, keyChar, modifierState);
				PopupCompletion (entry);
			}
		}

		void OnEndEditing ()
		{
			editing = false;
			editEntry.KeyPressEvent -= OnEditKeyPress;
			editEntry.KeyReleaseEvent -= HandleChanged;

			CompletionWindowManager.HideWindow ();
			currentCompletionData = null;
			if (EndEditing != null)
				EndEditing (this, EventArgs.Empty);
		}
		bool wasHandled = false;
		CodeCompletionContext ctx;
		Gdk.Key key;
		char keyChar;
		Gdk.ModifierType modifierState;
		uint keyValue;

		[GLib.ConnectBeforeAttribute]
		void OnEditKeyPress (object s, Gtk.KeyPressEventArgs args)
		{
			wasHandled = false;
			key = args.Event.Key;
			keyChar =  (char)args.Event.Key;
			modifierState = args.Event.State;
			keyValue = args.Event.KeyValue;
			if (currentCompletionData != null) {
				wasHandled  = CompletionWindowManager.PreProcessKeyEvent (key, keyChar, modifierState);
				args.RetVal = wasHandled ;
			}
		}

		void PopupCompletion (Entry entry)
		{
			Gtk.Application.Invoke (delegate {
				char c = (char)Gdk.Keyval.ToUnicode (keyValue);
				if (currentCompletionData == null && IsCompletionChar (c)) {
					string exp = entry.Text.Substring (0, entry.CursorPosition);
					currentCompletionData = GetCompletionData (exp);
					if (currentCompletionData != null) {
						DebugCompletionDataList dataList = new DebugCompletionDataList (currentCompletionData);
						ctx = ((ICompletionWidget)this).CreateCodeCompletionContext (entry.CursorPosition - currentCompletionData.ExpressionLength);
						CompletionWindowManager.ShowWindow (null, c, dataList, this, ctx);
					}
					else
						currentCompletionData = null;
				}
			});
		}
		
		TreeIter lastPinIter;
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			TreePath path;
			if (!editing && AllowPinning && GetPathAtPos ((int)evnt.X, (int)evnt.Y, out path)) {
				TreeIter it;
				if (path.Depth > 1 || PinnedWatch == null) {
					store.GetIter (out it, path);
					if (!it.Equals (lastPinIter)) {
						store.SetValue (it, PinIconCol, "md-pin-up");
						CleanPinIcon ();
						if (path.Depth > 1 || !RootPinAlwaysVisible)
							lastPinIter = it;
					}
				}
			}
			return base.OnMotionNotifyEvent (evnt);
		}
		
		void CleanPinIcon ()
		{
			if (!lastPinIter.Equals (TreeIter.Zero)) {
				store.SetValue (lastPinIter, PinIconCol, null);
				lastPinIter = TreeIter.Zero;
			}
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			if (!editing)
				CleanPinIcon ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			// Ignore if editing a cell, or if not editable
			if (!AllowEditing || !AllowAdding || editing)
				return base.OnKeyPressEvent (evnt);
			
			// Delete the current item with any delete key
			switch (evnt.Key) {
			case Gdk.Key.Delete:
			case Gdk.Key.KP_Delete:
			case Gdk.Key.BackSpace:
				if (Selection.CountSelectedRows () > 0) {
					List<TreeRowReference> selected = new List<TreeRowReference> ();
					bool deleted = false;
					string expression;
					ObjectValue val;
					TreeIter iter;

					// get a list of the selected rows (in reverse order so that we delete children before parents)
					foreach (var path in Selection.GetSelectedRows ())
						selected.Insert (0, new TreeRowReference (Model, path));

					foreach (var row in selected) {
						if (!Model.GetIter (out iter, row.Path))
							continue;

						val = (ObjectValue)store.GetValue (iter, ObjectCol);
						expression = GetFullExpression (iter);

						// Lookup and remove
						if (val != null && values.Contains (val)) {
							RemoveValue (val);
							deleted = true;
						} else if (!string.IsNullOrEmpty (expression) && valueNames.Contains (expression)) {
							RemoveExpression (expression);
							deleted = true;
						}
					}

					if (deleted)
						return true;
				}
				break;
			}
			return base.OnKeyPressEvent (evnt);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			allowStoreColumnSizes = true;
			bool res = base.OnButtonPressEvent (evnt);
			TreePath path;
			TreeViewColumn col;
			CellRenderer cr;
			
			//HACK: show context menu in release event instead of show event to work around gtk bug
			if (evnt.TriggersContextMenu ()) {
			//	ShowPopup (evnt);
				return true;
			}
			
			if (evnt.Button == 1 && GetCellAtPos ((int)evnt.X, (int)evnt.Y, out path, out col, out cr)) {
				TreeIter it;
				store.GetIter (out it, path);
				if (cr == crpViewer) {
					ObjectValue val = (ObjectValue) store.GetValue (it, ObjectCol);
					DebuggingService.ShowValueVisualizer (val);
				}
				else if (!editing) {
					if (cr == crpButton) {
						RefreshRow (it);
					} else if (cr == crpPin) {
						TreeIter pi;
						if (PinnedWatch != null && !store.IterParent (out pi, it))
							RemovePinnedWatch (it);
						else
							CreatePinnedWatch (it);
					} else if (cr == crpLiveUpdate) {
						TreeIter pi;
						if (PinnedWatch != null && !store.IterParent (out pi, it)) {
							DebuggingService.SetLiveUpdateMode (PinnedWatch, !PinnedWatch.LiveUpdate);
							if (PinnedWatch.LiveUpdate)
								store.SetValue (it, LiveUpdateIconCol, liveIcon);
							else
								store.SetValue (it, LiveUpdateIconCol, noLiveIcon);
						}
					}
				}
			}
			
			return res;
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			allowStoreColumnSizes = false;
			var res = base.OnButtonReleaseEvent (evnt);
			
			//HACK: show context menu in release event instead of show event to work around gtk bug
			if (evnt.IsContextMenuButton ()) {
				ShowPopup (evnt);
				return true;
			}
			return res;
		}
		
		protected override bool OnPopupMenu ()
		{
			ShowPopup (null);
			return true;
		}
		
		void ShowPopup (Gdk.EventButton evt)
		{
			IdeApp.CommandService.ShowContextMenu (this, evt, menuSet, this);
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			TreePath[] selected = Selection.GetSelectedRows ();
			TreeIter iter;
			
			if (selected == null || selected.Length == 0)
				return;

			if (selected.Length == 1) {
				object focus = IdeApp.Workbench.RootWindow.Focus;

				if (focus is Gtk.Editable) {
					((Gtk.Editable) focus).CopyClipboard ();
					return;
				}
			}

			var values = new List<string> ();
			var names = new List<string> ();
			var types = new List<string> ();
			int maxValue = 0;
			int maxName = 0;

			for (int i = 0; i < selected.Length; i++) {
				if (!store.GetIter (out iter, selected[i]))
					continue;

				string value = (string) store.GetValue (iter, ValueCol);
				string name = (string) store.GetValue (iter, NameCol);
				string type = (string) store.GetValue (iter, TypeCol);

				maxValue = Math.Max (maxValue, value.Length);
				maxName = Math.Max (maxName, name.Length);

				values.Add (value);
				names.Add (name);
				types.Add (type);
			}

			var str = new StringBuilder ();
			for (int i = 0; i < values.Count; i++) {
				if (i > 0)
					str.AppendLine ();

				str.Append (names[i]);
				if (names[i].Length < maxName)
					str.Append (new string (' ', maxName - names[i].Length));
				str.Append ('\t');
				str.Append (values[i]);
				if (values[i].Length < maxValue)
					str.Append (new string (' ', maxValue - values[i].Length));
				str.Append ('\t');
				str.Append (types[i]);
			}

			Clipboard.Get (Gdk.Selection.Clipboard).Text = str.ToString ();
		}
		
		[CommandHandler (EditCommands.Delete)]
		[CommandHandler (EditCommands.DeleteKey)]
		protected void OnDelete ()
		{
			foreach (TreePath tp in Selection.GetSelectedRows ()) {
				TreeIter it;
				if (store.GetIter (out it, tp)) {
					string exp = (string) store.GetValue (it, NameCol);
					cachedValues.Remove (exp);
					valueNames.Remove (exp);
				}
			}
			Refresh ();
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		[CommandUpdateHandler (EditCommands.DeleteKey)]
		protected void OnUpdateDelete (CommandInfo cinfo)
		{
			if (editing) {
				cinfo.Bypass = true;
				return;
			}
			
			if (!allowAdding) {
				cinfo.Visible = false;
				return;
			}
			TreePath[] sel = Selection.GetSelectedRows ();
			if (sel.Length == 0) {
				cinfo.Enabled = false;
				return;
			}
			foreach (TreePath tp in sel) {
				if (tp.Depth > 1) {
					cinfo.Enabled = false;
					return;
				}
			}
		}
		
		[CommandHandler (DebugCommands.AddWatch)]
		protected void OnAddWatch ()
		{
			List<string> exps = new List<string> ();
			foreach (TreePath tp in Selection.GetSelectedRows ()) {
				TreeIter it;
				if (store.GetIter (out it, tp)) {
					string exp = GetFullExpression (it);
					exps.Add (exp);
				}
			}
			foreach (string s in exps) {
				DebuggingService.AddWatch (s);
			}
		}

		[CommandUpdateHandler (DebugCommands.AddWatch)]
		protected void OnUpdateAddWatch (CommandInfo cinfo)
		{
			cinfo.Enabled = Selection.GetSelectedRows ().Length > 0;
		}
		
		[CommandHandler (EditCommands.Rename)]
		protected void OnRename ()
		{
			TreeIter it;
			if (store.GetIter (out it, Selection.GetSelectedRows ()[0]))
				SetCursor (store.GetPath (it), Columns[0], true);
		}
		
		[CommandUpdateHandler (EditCommands.Rename)]
		protected void OnUpdateRename (CommandInfo cinfo)
		{
			cinfo.Visible = allowAdding;
			cinfo.Enabled = Selection.GetSelectedRows ().Length == 1;
		}
		
		protected override void OnRowActivated (TreePath path, TreeViewColumn column)
		{
			base.OnRowActivated (path, column);
			TreeIter it;
			TreePath[] sel = Selection.GetSelectedRows ();
			if (store.GetIter (out it, sel[0])) {
				ObjectValue val = (ObjectValue) store.GetValue (it, ObjectCol);
				if (val != null && val.Name == DebuggingService.DebuggerSession.EvaluationOptions.CurrentExceptionTag)
					DebuggingService.ShowExceptionCaughtDialog ();
			}
		}
		
		
		bool GetCellAtPos (int x, int y, out TreePath path, out TreeViewColumn col, out CellRenderer cellRenderer)
		{
			int cx, cy;
			if (GetPathAtPos (x, y, out path, out col, out cx, out cy)) {
				GetCellArea (path, col);
				foreach (CellRenderer cr in col.CellRenderers) {
					int xo, w;
					col.CellGetPosition (cr, out xo, out w);
					if (cr.Visible && cx >= xo && cx < xo + w) {
						cellRenderer = cr;
						return true;
					}
				}
			}
			cellRenderer = null;
			return false;
		}
		
		string GetFullExpression (TreeIter it)
		{
			TreePath path = store.GetPath (it);
			string exp = "";
			
			while (path.Depth != 1) {
				ObjectValue val = (ObjectValue)store.GetValue (it, ObjectCol);
				exp = val.ChildSelector + exp;
				if (!store.IterParent (out it, it))
					break;
				path = store.GetPath (it);
			}

			string name = (string) store.GetValue (it, NameCol);

			return name + exp;
		}

		public void CreatePinnedWatch (TreeIter it)
		{
			string exp = GetFullExpression (it);
			
			PinnedWatch watch = new PinnedWatch ();
			if (PinnedWatch != null) {
				CollapseAll ();
				watch.File = PinnedWatch.File;
				watch.Line = PinnedWatch.Line;
				watch.OffsetX = PinnedWatch.OffsetX;
				watch.OffsetY = PinnedWatch.OffsetY + SizeRequest ().Height + 5;
			}
			else {
				watch.File = PinnedWatchFile;
				watch.Line = PinnedWatchLine;
				watch.OffsetX = -1; // means that the watch should be placed at the line coordinates defined by watch.Line
				watch.OffsetY = -1;
			}
			watch.Expression = exp;
			DebuggingService.PinnedWatches.Add (watch);
			if (PinStatusChanged != null)
				PinStatusChanged (this, EventArgs.Empty);
		}
		
		public void RemovePinnedWatch (TreeIter it)
		{
			DebuggingService.PinnedWatches.Remove (PinnedWatch);
			if (PinStatusChanged != null)
				PinStatusChanged (this, EventArgs.Empty);
		}
		
		bool IsCompletionChar (char c)
		{
			return (char.IsLetterOrDigit (c) || char.IsPunctuation (c) || char.IsSymbol (c) || char.IsWhiteSpace (c));
		}
		

		#region ICompletionWidget implementation 
		
		CodeCompletionContext ICompletionWidget.CurrentCodeCompletionContext {
			get {
				return ((ICompletionWidget)this).CreateCodeCompletionContext (editEntry.Position);
			}
		}
		
		public event EventHandler CompletionContextChanged;

		protected virtual void OnCompletionContextChanged (EventArgs e)
		{
			EventHandler handler = this.CompletionContextChanged;
			if (handler != null)
				handler (this, e);
		}
		
		string ICompletionWidget.GetText (int startOffset, int endOffset)
		{
			string text = editEntry.Text;

			if (startOffset < 0 || endOffset < 0 || startOffset > endOffset || startOffset >= text.Length)
				return "";

			int length = Math.Min (endOffset - startOffset, text.Length - startOffset);

			return text.Substring (startOffset, length);
		}
		
		void ICompletionWidget.Replace (int offset, int count, string text)
		{
			if (count > 0)
				editEntry.Text = editEntry.Text.Remove (offset, count);
			if (!string.IsNullOrEmpty (text))
				editEntry.Text = editEntry.Text.Insert (offset, text);
		}
		
		int ICompletionWidget.CaretOffset {
			get {
				return editEntry.Position;
			}
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
			c.TriggerWordLength = currentCompletionData.ExpressionLength;
			
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
		
		string ICompletionWidget.GetCompletionText (CodeCompletionContext ctx)
		{
			return editEntry.Text.Substring (ctx.TriggerOffset, ctx.TriggerWordLength);
		}
		
		void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
		{
			int sp = editEntry.Position - partial_word.Length;
			editEntry.DeleteText (sp, sp + partial_word.Length);
			editEntry.InsertText (complete_word, ref sp);
			editEntry.Position = sp; // sp is incremented by InsertText
		}
		
		void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int offset)
		{
			int sp = editEntry.Position - partial_word.Length;
			editEntry.DeleteText (sp, sp + partial_word.Length);
			editEntry.InsertText (complete_word, ref sp);
			editEntry.Position = sp + offset; // sp is incremented by InsertText
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
			ObjectValue[] values = new ObjectValue [names.Length];
			List<string> list = new List<string> ();
			
			for (int n=0; n<names.Length; n++) {
				ObjectValue val;
				if (cachedValues.TryGetValue (names [n], out val))
					values [n] = val;
				else
					list.Add (names[n]);
			}

			ObjectValue[] qvalues;
			if (frame != null)
				qvalues = frame.GetExpressionValues (list.ToArray (), true);
			else {
				qvalues = new ObjectValue [list.Count];
				for (int n=0; n<qvalues.Length; n++)
					qvalues [n] = ObjectValue.CreateUnknown (list [n]);
			}

			int kv = 0;
			for (int n=0; n<values.Length; n++) {
				if (values [n] == null) {
					values [n] = qvalues [kv++];
					cachedValues [names[n]] = values [n];
				}
			}
			
			return values;
		}
		
		Mono.Debugging.Client.CompletionData GetCompletionData (string exp)
		{
			if (frame != null)
				return frame.GetExpressionCompletionData (exp);
			else
				return null;
		}
		
		internal void SetCustomFont (Pango.FontDescription font)
		{
			crtExp.FontDesc = crtType.FontDesc = crtValue.FontDesc = font;
		}
	}
	
	class DebugCompletionDataList: List<ICSharpCode.NRefactory.Completion.ICompletionData>, ICompletionDataList
	{
		public bool IsSorted { get; set; }
		public DebugCompletionDataList (Mono.Debugging.Client.CompletionData data)
		{
			IsSorted = false;
			foreach (CompletionItem it in data.Items)
				Add (new DebugCompletionData (it));
			AutoSelect =true;
		}
		public bool AutoSelect { get; set; }
		public string DefaultCompletionString {
			get {
				return string.Empty;
			}
		}

		public bool AutoCompleteUniqueMatch {
			get { return false; }
		}
		
		public bool AutoCompleteEmptyMatch {
			get { return false; }
		}
		public bool AutoCompleteEmptyMatchOnCurlyBrace {
			get { return false; }
		}
		public bool CloseOnSquareBrackets {
			get {
				return false;
			}
		}
		
		public CompletionSelectionMode CompletionSelectionMode {
			get;
			set;
		}
		static List<ICompletionKeyHandler> keyHandler = new List<ICompletionKeyHandler> ();
		public IEnumerable<ICompletionKeyHandler> KeyHandler { get { return keyHandler;} }

		public void OnCompletionListClosed (EventArgs e)
		{
			EventHandler handler = this.CompletionListClosed;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler CompletionListClosed;
	}
	
	class DebugCompletionData : MonoDevelop.Ide.CodeCompletion.CompletionData
	{
		CompletionItem item;
		
		public DebugCompletionData (CompletionItem item)
		{
			this.item = item;
		}
		
		public override IconId Icon {
			get {
				return ObjectValueTreeView.GetIcon (item.Flags);
			}
		}
		
		public override string DisplayText {
			get {
				return item.Name;
			}
		}
		
		public override string CompletionText {
			get {
				return item.Name;
			}
		}
	}
}
