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
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.Ide.Gui.Dialogs {

	public partial class EncapsulateFieldDialog : Gtk.Dialog {
		IClass declaringType;

		ListStore store;
		ListStore visibilityStore;
		int selected;

		private const int colCheckedIndex = 0;
		private const int colFieldNameIndex = 1;
		private const int colPropertyNameIndex = 2;
		private const int colVisibilityIndex = 3;
		private const int colReadOnlyIndex = 4;
		private const int colFieldIndex = 5;

		public EncapsulateFieldDialog (IParserContext ctx, IClass declaringType)
			: this (declaringType, null)
		{}

		public EncapsulateFieldDialog (IParserContext ctx, IField field)
			: this (field.DeclaringType, field)
		{}

		private EncapsulateFieldDialog (IClass declaringType, IField field)
		{
			this.declaringType = declaringType;
			this.Build ();

			Title = GettextCatalog.GetString ("Encapsulate fields...");
			buttonOk.Sensitive = true;
			store = new ListStore (typeof (bool), typeof(string), typeof (string), typeof (string), typeof (bool), typeof (IField));
			visibilityStore = new ListStore (typeof (string));

			CellRendererToggle cbRenderer = new CellRendererToggle ();
			cbRenderer.Activatable = true;
			cbRenderer.Toggled += OnSelectedToggled;
			TreeViewColumn cbCol = new TreeViewColumn ();
			cbCol.Title = "";
			cbCol.PackStart (cbRenderer, true);
			cbCol.AddAttribute (cbRenderer, "active", colCheckedIndex);
			treeview.AppendColumn (cbCol);

			CellRendererText fieldRenderer = new CellRendererText ();
			TreeViewColumn fieldCol = new TreeViewColumn ();
			fieldCol.Title = GettextCatalog.GetString ("Field");
			fieldCol.PackStart (fieldRenderer, true);
			fieldCol.AddAttribute (fieldRenderer, "text", colFieldNameIndex);
			treeview.AppendColumn (fieldCol);

			CellRendererText propertyRenderer = new CellRendererText ();
			propertyRenderer.Editable = true;
			propertyRenderer.Edited += new EditedHandler (OnPropertyEdited);
			TreeViewColumn propertyCol = new TreeViewColumn ();
			propertyCol.Title = GettextCatalog.GetString ("Property");
			propertyCol.PackStart (propertyRenderer, true);
			propertyCol.AddAttribute (propertyRenderer, "text", colPropertyNameIndex);
			treeview.AppendColumn (propertyCol);

			CellRendererCombo visiComboRenderer = new CellRendererCombo ();
			visiComboRenderer.Model = visibilityStore;
			visiComboRenderer.Editable = true;
			visiComboRenderer.Edited += new EditedHandler (OnVisibilityEdited);
			visiComboRenderer.HasEntry = false;
			visiComboRenderer.TextColumn = 0;

			TreeViewColumn visiCol = new TreeViewColumn ();
			visiCol.Title = GettextCatalog.GetString ("Visibility");
			visiCol.PackStart (visiComboRenderer, true);
			visiCol.AddAttribute (visiComboRenderer, "text", colVisibilityIndex);
			treeview.AppendColumn (visiCol);

			CellRendererToggle roRenderer = new CellRendererToggle ();
			roRenderer.Activatable = true;
			roRenderer.Toggled += new ToggledHandler (OnReadOnlyToggled);
			TreeViewColumn roCol = new TreeViewColumn ();
			roCol.Title = GettextCatalog.GetString ("Read only");
			roCol.PackStart (roRenderer, true);
			roCol.AddAttribute (roRenderer, "active", colReadOnlyIndex);
			treeview.AppendColumn (roCol);

			visibilityStore.AppendValues ("Public");
			visibilityStore.AppendValues ("Private");
			visibilityStore.AppendValues ("Protected");
			visibilityStore.AppendValues ("Internal");

			treeview.Model = store;

			foreach (IField ifield in declaringType.Fields) {
				string propertyName = GeneratePropertyName (ifield.Name);
				string errorMsg;
				bool enabled = field != null && (field.Name == ifield.Name) &&
					IsValidPropertyName (propertyName, out errorMsg);

				store.AppendValues (enabled, ifield.Name, GeneratePropertyName (ifield.Name),
						"Public", false, ifield);
			}

			store.SetSortColumnId (colFieldNameIndex, SortType.Ascending);
			buttonSelectAll.Clicked += OnSelectAllClicked;
			buttonUnselectAll.Clicked += OnUnselectAllClicked;
			buttonOk.Clicked += OnOKClicked;
			buttonCancel.Clicked += OnCancelClicked;
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

			TrySetPropertyName (iter, args.NewText);
		}

		bool TrySetPropertyName (TreeIter iter, string name)
		{
			string error;
			if (IsValidPropertyName (name, out error)) {
				store.SetValue (iter, colPropertyNameIndex, name);
				labelError.Text = String.Empty;
				return true;
			} else {
				labelError.Text = error;
				return false;
			}
		}

		bool IsValidPropertyName (string name, out string error_msg)
		{
			IMember member;
			int i;

			// Don't allow the user to click OK unless there is a new name
			if (name.Length == 0) {
				error_msg = GettextCatalog.GetString ("Property name must be non-empty.");
				return false;
			}

			// Check that this member name isn't already used by properties
			for (i = 0; i < declaringType.Properties.Count; i++) {
				member = declaringType.Properties[i];
				if (member.Name == name) {
					error_msg = GettextCatalog.GetString ("Property name conflicts with an existing property name.");
					return false;
				}
			}

			// Check that this member name isn't already used by fields
			for (i = 0; i < declaringType.Fields.Count; i++) {
				member = declaringType.Fields[i];
				if (member.Name == name) {
					error_msg = GettextCatalog.GetString ("Property name conflicts with an existing field name.");
					return false;
				}
			}

			// Check that this member name isn't already used by methods
			for (i = 0; i < declaringType.Methods.Count; i++) {
				member = declaringType.Methods[i];
				if (member.Name == name) {
					error_msg = GettextCatalog.GetString ("Property name conflicts with an existing method name.");
					return false;
				}
			}

			// Check that this member name isn't already used by events
			for (i = 0; i < declaringType.Events.Count; i++) {
				member = declaringType.Events[i];
				if (member.Name == name) {
					error_msg = GettextCatalog.GetString ("Property name conflicts with an existing event name.");
					return false;
				}
			}

			error_msg = String.Empty;
			return true;
		}

		void OnEntryActivated (object sender, EventArgs e)
		{
			if (buttonOk.Sensitive)
				buttonOk.Click ();
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
			if (!old_value) {
				// selecting
				string name = (string) store.GetValue (iter, colPropertyNameIndex);
				if (!TrySetPropertyName (iter, name))
					return;
			}

			store.SetValue (iter, colCheckedIndex, !old_value);
		}

		private void OnReadOnlyToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
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
		}

		void SelectAll (bool select)
		{
			TreeIter iter;
			if (!store.GetIterFirst (out iter))
				return;

			do {
				if (select) {
					string propertyName = (string) store.GetValue (iter, colPropertyNameIndex);
					if (!TrySetPropertyName (iter, propertyName))
						continue;
				}
				store.SetValue (iter, colCheckedIndex, select);
			} while (store.IterNext (ref iter));
		}

		void OnCancelClicked (object sender, EventArgs e)
		{
			((Widget) this).Destroy ();
		}

		void OnOKClicked (object sender, EventArgs e)
		{
			TreeIter iter;
			if (!store.GetIterFirst (out iter))
				return;

			CodeRefactorer refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (this.Title, null);

			do {
				bool selected = (bool) store.GetValue (iter, colCheckedIndex);
				if (!selected)
					continue;

				string propertyName = (string) store.GetValue (iter, colPropertyNameIndex);
				string visibility = (string) store.GetValue (iter, colVisibilityIndex);
				bool read_only = (bool) store.GetValue (iter, colReadOnlyIndex);
				IField field = (IField) store.GetValue (iter, colFieldIndex);

				refactorer.EncapsulateField (
						monitor, declaringType, field, propertyName,
						StringToMemberAttributes (visibility), radioUpdateAll.Active, !read_only);
			} while (store.IterNext (ref iter));

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
