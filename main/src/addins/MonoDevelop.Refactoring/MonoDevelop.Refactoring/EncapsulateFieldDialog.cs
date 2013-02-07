// EncapsulateFieldDialog.cs
//
// Author:
//   Jeffrey Stedfast  <fejj@novell.com>
//   Ankit Jain  <jankit@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.CodeDom;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Ide;
using Mono.TextEditor;
using System.Linq;
using System.Collections.Generic;
using Mono.TextEditor.PopupWindow;

namespace MonoDevelop.Refactoring {

	public partial class EncapsulateFieldDialog : Gtk.Dialog
	{
		IType declaringType;
		ListStore store;
		ListStore visibilityStore;
		MonoDevelop.Ide.Gui.Document editor;
		
		private const int colCheckedIndex = 0;
		private const int colFieldNameIndex = 1;
		private const int colPropertyNameIndex = 2;
		private const int colVisibilityIndex = 3;
		private const int colReadOnlyIndex = 4;
		private const int colFieldIndex = 5;

		public EncapsulateFieldDialog (MonoDevelop.Ide.Gui.Document editor, ITypeResolveContext ctx, IType declaringType)
			: this (editor, declaringType, null)
		{}

		public EncapsulateFieldDialog (MonoDevelop.Ide.Gui.Document editor, ITypeResolveContext ctx, IField field)
			: this (editor, field.DeclaringType, field)
		{}

		private EncapsulateFieldDialog (MonoDevelop.Ide.Gui.Document editor, IType declaringType, IField field)
		{
			this.editor = editor;
			this.declaringType = declaringType;
			this.Build ();

			Title = GettextCatalog.GetString ("Encapsulate Fields");
			buttonOk.Sensitive = true;
			store = new ListStore (typeof (bool), typeof(string), typeof (string), typeof (string), typeof (bool), typeof (IField));
			visibilityStore = new ListStore (typeof (string));

			// Column #1
			CellRendererToggle cbRenderer = new CellRendererToggle ();
			cbRenderer.Activatable = true;
			cbRenderer.Toggled += OnSelectedToggled;
			TreeViewColumn cbCol = new TreeViewColumn ();
			cbCol.Title = "";
			cbCol.PackStart (cbRenderer, false);
			cbCol.AddAttribute (cbRenderer, "active", colCheckedIndex);
			treeview.AppendColumn (cbCol);

			// Column #2
			CellRendererText fieldRenderer = new CellRendererText ();
			fieldRenderer.Weight = (int) Pango.Weight.Bold;
			TreeViewColumn fieldCol = new TreeViewColumn ();
			fieldCol.Title = GettextCatalog.GetString ("Field");
			fieldCol.Expand = true;
			fieldCol.PackStart (fieldRenderer, true);
			fieldCol.AddAttribute (fieldRenderer, "text", colFieldNameIndex);
			treeview.AppendColumn (fieldCol);

			// Column #3
			CellRendererText propertyRenderer = new CellRendererText ();
			propertyRenderer.Editable = true;
			propertyRenderer.Edited += new EditedHandler (OnPropertyEdited);
			TreeViewColumn propertyCol = new TreeViewColumn ();
			propertyCol.Title = GettextCatalog.GetString ("Property");
			propertyCol.Expand = true;
			propertyCol.PackStart (propertyRenderer, true);
			propertyCol.AddAttribute (propertyRenderer, "text", colPropertyNameIndex);
			propertyCol.SetCellDataFunc (propertyRenderer, new TreeCellDataFunc (RenderPropertyName));
			treeview.AppendColumn (propertyCol);

			// Column #4
			CellRendererCombo visiComboRenderer = new CellRendererCombo ();
			visiComboRenderer.Model = visibilityStore;
			visiComboRenderer.Editable = true;
			visiComboRenderer.Edited += new EditedHandler (OnVisibilityEdited);
			visiComboRenderer.HasEntry = false;
			visiComboRenderer.TextColumn = 0;

			TreeViewColumn visiCol = new TreeViewColumn ();
			visiCol.Title = GettextCatalog.GetString ("Visibility");
			visiCol.PackStart (visiComboRenderer, false);
			visiCol.AddAttribute (visiComboRenderer, "text", colVisibilityIndex);
			treeview.AppendColumn (visiCol);

			// Column #5
			CellRendererToggle roRenderer = new CellRendererToggle ();
			roRenderer.Activatable = true;
			roRenderer.Xalign = 0.0f;
			roRenderer.Toggled += new ToggledHandler (OnReadOnlyToggled);
			TreeViewColumn roCol = new TreeViewColumn ();
			roCol.Title = GettextCatalog.GetString ("Read only");
			roCol.PackStart (roRenderer, false);
			roCol.AddAttribute (roRenderer, "active", colReadOnlyIndex);
			treeview.AppendColumn (roCol);

			visibilityStore.AppendValues ("Public");
			visibilityStore.AppendValues ("Private");
			visibilityStore.AppendValues ("Protected");
			visibilityStore.AppendValues ("Internal");

			treeview.Model = store;

			foreach (IField ifield in declaringType.Fields) {
				bool enabled = field != null && (field.Name == ifield.Name);
				string propertyName = GeneratePropertyName (ifield.Name);
				store.AppendValues (enabled, ifield.Name, propertyName,
				                    "Public", ifield.IsReadonly || ifield.IsLiteral, ifield);

				if (enabled)
					CheckAndUpdateConflictMessage (propertyName, false);
			}

			store.SetSortColumnId (colFieldNameIndex, SortType.Ascending);
			buttonSelectAll.Clicked += OnSelectAllClicked;
			buttonUnselectAll.Clicked += OnUnselectAllClicked;
			buttonOk.Clicked += OnOKClicked;
			buttonCancel.Clicked += OnCancelClicked;

			UpdateOKButton ();
		}

		string GeneratePropertyName (string fieldName)
		{
			StringBuilder builder = new StringBuilder (fieldName.Length);
			bool upper = true;
			int i = 0;

			// Field names are commonly prefixed with "m" or "m_" by devs from c++ land.
			if (fieldName[0] == 'm' && fieldName.Length > 1 &&
			    (fieldName[1] == '_' || Char.IsUpper (fieldName[1])))
				i++;

			while (i < fieldName.Length) {
				if (fieldName[i] == '_') {
					// strip _'s and uppercase the next letter
					upper = true;
				} else if (Char.IsLetter (fieldName[i])) {
					builder.Append (upper ? Char.ToUpper (fieldName[i]) : fieldName[i]);
					upper = false;
				} else {
					builder.Append (fieldName[i]);
				}

				i++;
			}

			return builder.ToString ();
		}

		void OnPropertyEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (!store.GetIterFromString (out iter, args.Path))
				return;

			store.SetValue (iter, colPropertyNameIndex, args.NewText);
			if (!CheckAndUpdateConflictMessage (iter, true))
				// unselect this field
				store.SetValue (iter, colCheckedIndex, false);

			UpdateOKButton ();
		}

		void RenderPropertyName (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			bool selected = (bool) store.GetValue (iter, colCheckedIndex);
			string propertyName = (string) store.GetValue (iter, colPropertyNameIndex);
			string error;

			CellRendererText cellRendererText = (CellRendererText) cell;
			if (!selected || IsValidPropertyName (propertyName, out error))
				cellRendererText.Foreground = "black";
			else
				cellRendererText.Foreground = "red";

			cellRendererText.Text = propertyName;
		}

		// @clearOnValid: clear the message label if propertyName is valid
		bool CheckAndUpdateConflictMessage (TreeIter iter, bool clearOnValid)
		{
			return CheckAndUpdateConflictMessage ((string) store.GetValue (iter, colPropertyNameIndex), 
			                                      clearOnValid);
		}

		// @clearOnValid: clear the message label if propertyName is valid
		bool CheckAndUpdateConflictMessage (string name, bool clearOnValid)
		{
			string error;
			if (IsValidPropertyName (name, out error)) {
				if (clearOnValid)
					SetErrorMessage (null);
				return true;
			} else {
				SetErrorMessage (error);
				return false;
			}
		}

		void SetErrorMessage (string message)
		{
			if (String.IsNullOrEmpty (message)) {
				labelError.Text = String.Empty;
				imageError.Clear ();
			} else {
				labelError.Text = message;
				imageError.SetFromStock (MonoDevelop.Ide.Gui.Stock.Error, IconSize.Menu);
			}
		}

		bool IsValidPropertyName (string name, out string error_msg)
		{
			// Don't allow the user to click OK unless there is a new name
			if (name.Length == 0) {
				error_msg = GettextCatalog.GetString ("Property name must be non-empty.");
				return false;
			}
			foreach (IMember member in declaringType.Members) {
				if (member.Name == name) {
					error_msg = GettextCatalog.GetString ("Property name conflicts with an existing member name.");
					return false;
				}
			}
			error_msg = String.Empty;
			return true;
		}

		private void OnVisibilityEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path))
				store.SetValue (iter, colVisibilityIndex, args.NewText);
		}

		private void OnSelectedToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (!store.GetIterFromString (out iter, args.Path))
				return;

			bool old_value = (bool) store.GetValue (iter, colCheckedIndex);
			store.SetValue (iter, colCheckedIndex, !old_value);

			if (old_value)
				SetErrorMessage (null);
			else
				CheckAndUpdateConflictMessage (iter, true);
			UpdateOKButton ();
		}

		void UpdateOKButton ()
		{
			TreeIter iter;
			if (!store.GetIterFirst (out iter))
				return;

			bool atleast_one_selected = false;
			do {
				bool selected = (bool) store.GetValue (iter, colCheckedIndex);
				if (!selected)
					continue;

				atleast_one_selected = true;

				string propertyName = (string) store.GetValue (iter, colPropertyNameIndex);
				string error;
				if (!IsValidPropertyName (propertyName, out error)) {
					buttonOk.Sensitive = false;
					return;
				}
			} while (store.IterNext (ref iter));

			buttonOk.Sensitive = atleast_one_selected;
		}

		private void OnReadOnlyToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				IField ifield = (IField) store.GetValue (iter, colFieldIndex);
				if (ifield.IsReadonly || ifield.IsLiteral)
					return;

				bool value = (bool) store.GetValue (iter, colReadOnlyIndex);
				store.SetValue (iter, colReadOnlyIndex, !value);
			}
		}

		void OnSelectAllClicked (object sender, EventArgs e)
		{
			SelectAll (true);
		}

		void OnUnselectAllClicked (object sender, EventArgs e)
		{
			SelectAll (false);
			SetErrorMessage (null);
		}

		void SelectAll (bool select)
		{
			TreeIter iter;
			if (!store.GetIterFirst (out iter))
				return;

			// clear any old error message
			SetErrorMessage (null);

			bool has_error = false;
			do {
				if (select && !CheckAndUpdateConflictMessage (iter, false))
					has_error = true;
				store.SetValue (iter, colCheckedIndex, select);
			} while (store.IterNext (ref iter));

			if (has_error)
				SetErrorMessage (GettextCatalog.GetString ("One or more property names conflict with existing members of the class"));
			UpdateOKButton ();
		}

		void OnCancelClicked (object sender, EventArgs e)
		{
			((Widget) this).Destroy ();
		}
		class FieldData {
			public IField Field { get; set; }
			public string PropertyName { get; set; }
			public bool ReadOnly { get; set; }
			public Modifiers Modifiers { get; set; }
			
			public FieldData (IField field, string propertyName, bool readOnly, Modifiers modifiers)
			{
				this.Field = field;
				this.PropertyName = propertyName;
				this.ReadOnly = readOnly;
				this.Modifiers = modifiers;
			}
		}
		
		void OnOKClicked (object sender, EventArgs e)
		{
			TreeIter iter;
			if (!store.GetIterFirst (out iter))
				return;
			
			List<FieldData> data = new List<FieldData> ();
			
			do {
				bool selected = (bool) store.GetValue (iter, colCheckedIndex);
				if (!selected)
					continue;

				string propertyName = (string) store.GetValue (iter, colPropertyNameIndex);
				string visibility = (string) store.GetValue (iter, colVisibilityIndex);
				bool read_only = (bool) store.GetValue (iter, colReadOnlyIndex);
				IField field = (IField) store.GetValue (iter, colFieldIndex);
				Modifiers mod = Modifiers.None;
				if (visibility.ToUpper () == "PUBLIC")
					mod = Modifiers.Public;
				if (visibility.ToUpper () == "PRIVATE")
					mod = Modifiers.Private;
				if (visibility.ToUpper () == "PROTECTED")
					mod = Modifiers.Protected;
				if (visibility.ToUpper () == "INTERNAL")
					mod = Modifiers.Internal;
				data.Add (new FieldData (field, propertyName, read_only, mod));
			} while (store.IterNext (ref iter));
			
			InsertionCursorEditMode mode = new InsertionCursorEditMode (editor.Editor.Parent, CodeGenerationService.GetInsertionPoints (editor, declaringType));
			ModeHelpWindow helpWindow = new ModeHelpWindow ();
			helpWindow.TransientFor = IdeApp.Workbench.RootWindow;
			helpWindow.TitleText = GettextCatalog.GetString ("<b>Encapsulate Field -- Targeting</b>");
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Key</b>"), GettextCatalog.GetString ("<b>Behavior</b>")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Up</b>"), GettextCatalog.GetString ("Move to <b>previous</b> target point.")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Down</b>"), GettextCatalog.GetString ("Move to <b>next</b> target point.")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Enter</b>"), GettextCatalog.GetString ("<b>Declare new property</b> at target point.")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Esc</b>"), GettextCatalog.GetString ("<b>Cancel</b> this refactoring.")));
			mode.HelpWindow = helpWindow;
			mode.CurIndex = mode.InsertionPoints.Count - 1;
			int idx = -1, i = 0;
			TextLocation lTextLocation = TextLocation.Empty;
			foreach (IMember member in declaringType.Members) {
				if (lTextLocation != member.Location && data.Any (d => d.Field.Location == member.Location))
					idx = i;
				lTextLocation = member.Location;
				i++;
			}
			if (idx >= 0)
				mode.CurIndex = idx + 1;
			mode.StartMode ();
			DesktopService.RemoveWindowShadow (helpWindow);
			mode.Exited += delegate(object s, InsertionCursorEventArgs args) {
				if (args.Success) {
					CodeGenerator generator =  CodeGenerator.CreateGenerator (editor.Editor.Document.MimeType, editor.Editor.TabsToSpaces, editor.Editor.Options.TabSize, editor.Editor.EolMarker);
					StringBuilder code = new StringBuilder ();
					for (int j = 0; j < data.Count; j++) {
						if (j > 0) {
							code.AppendLine ();
							code.AppendLine ();
						}
						var f = data[j];
						code.Append (generator.CreateFieldEncapsulation (declaringType, f.Field, f.PropertyName, f.Modifiers, f.ReadOnly));
					}
					args.InsertionPoint.Insert (editor.Editor, code.ToString ());
				}
			};
			((Widget) this).Destroy ();
		}

		static MemberAttributes StringToMemberAttributes (string visibility)
		{
			switch (visibility) {
			case "Public":
				return MemberAttributes.Public;
			case "Private":
				return MemberAttributes.Private;
			case "Protected":
				return MemberAttributes.Family;
			case "Internal":
				return MemberAttributes.Assembly;
			default:
				throw new ArgumentException ("Unknown visibility : " + visibility);
			}
		}

	}
}
