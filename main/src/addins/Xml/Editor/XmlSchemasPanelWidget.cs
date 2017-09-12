// 
// XmlSchemasPanelWidget.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//   Matthew Ward
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// Copyright (C) 2005-2007 Matthew Ward
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
using System.Collections.Generic;

using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Xml.Completion;

namespace MonoDevelop.Xml.Editor
{
	class XmlSchemasPanelWidget : VBox
	{
		ListStore registeredSchemasStore;
		ListStore defaultAssociationsStore;
		ListStore registeredSchemasComboModel;
		List<XmlSchemaCompletionData> addedSchemas = new List<XmlSchemaCompletionData> ();
		List<XmlSchemaCompletionData> removedSchemas = new List<XmlSchemaCompletionData> ();
		List<string> removedExtensions = new List<string> ();

		TreeView registeredSchemasView, defaultAssociationsView;
		Button registeredSchemasAddButton, registeredSchemasRemoveButton;
		Button defaultAssociationsAddButton, defaultAssociationsRemoveButton;
		
		public XmlSchemasPanelWidget ()
		{
			Build ();
			
			//set up tree view for default schemas
			var textRenderer = new CellRendererText ();
			registeredSchemasStore = new ListStore (typeof (XmlSchemaCompletionData));
			registeredSchemasView.Model = registeredSchemasStore;
			registeredSchemasView.SearchColumn = -1; // disable the interactive search

			registeredSchemasView.AppendColumn (GettextCatalog.GetString ("Namespace"), textRenderer,
				(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) => {
					((CellRendererText)cell).Text = GetSchema (iter).NamespaceUri;
				}
			);
			
			registeredSchemasView.AppendColumn (GettextCatalog.GetString ("Type"), textRenderer,
				(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) => {
					((CellRendererText)cell).Text = GetSchema (iter).ReadOnly? 
						  GettextCatalog.GetString ("Built in") 
						: GettextCatalog.GetString ("User schema");
			});
			
			registeredSchemasStore.SetSortFunc (0, SortSchemas);
			
			registeredSchemasStore.SetSortColumnId (0, SortType.Ascending);
			
			//update state of "remove" button depending on whether schema is read-only and anything's slected
			registeredSchemasView.Selection.Changed += delegate {
				var data = GetSelectedSchema ();
				registeredSchemasRemoveButton.Sensitive = (data != null && !data.ReadOnly);
			};
			registeredSchemasRemoveButton.Sensitive = false;
			
			//set up cells for associations
			var extensionTextRenderer = new CellRendererText ();
			extensionTextRenderer.Editable = true;
			var prefixTextRenderer = new CellRendererText ();
			prefixTextRenderer.Editable = true;
			
			var comboEditor = new CellRendererCombo ();
			registeredSchemasComboModel = new ListStore (typeof (string));
			comboEditor.Model = registeredSchemasComboModel;
			comboEditor.Mode = CellRendererMode.Editable;
			comboEditor.TextColumn = 0;
			comboEditor.Editable = true;
			comboEditor.HasEntry = false;
			
			//rebuild combo's model from default schemas whenever editing starts
			comboEditor.EditingStarted += delegate (object sender, EditingStartedArgs args) {
				registeredSchemasComboModel.Clear ();
				registeredSchemasComboModel.AppendValues (string.Empty);
				foreach (TreeIter iter in WalkStore (registeredSchemasStore))
					registeredSchemasComboModel.AppendValues (
						GetSchema (iter).NamespaceUri
					);
				args.RetVal = true;
				registeredSchemasComboModel.SetSortColumnId (0, SortType.Ascending);
			};
			
			//set up tree view for associations
			defaultAssociationsStore = new ListStore (typeof (string), typeof (string), typeof (string), typeof (bool));
			defaultAssociationsView.Model = defaultAssociationsStore;
			defaultAssociationsView.SearchColumn = -1; // disable the interactive search
			defaultAssociationsView.AppendColumn (GettextCatalog.GetString ("File Extension"), extensionTextRenderer, "text", COL_EXT);
			defaultAssociationsView.AppendColumn (GettextCatalog.GetString ("Namespace"), comboEditor, "text", COL_NS);
			defaultAssociationsView.AppendColumn (GettextCatalog.GetString ("Prefix"), prefixTextRenderer, "text", COL_PREFIX);
			defaultAssociationsStore.SetSortColumnId (COL_EXT, SortType.Ascending);
			
			//editing handlers
			extensionTextRenderer.Edited += handleExtensionSet;
			comboEditor.Edited += (sender, args) => setAssocValAndMarkChanged (args.Path, COL_NS, args.NewText ?? "");
			prefixTextRenderer.Edited += delegate (object sender, EditedArgs args) {
				foreach (char c in args.NewText)
					if (!char.IsLetterOrDigit (c))
						//FIXME: give an error message?
						return;
				setAssocValAndMarkChanged (args.Path, COL_PREFIX, args.NewText);
			};
			
			//update state of "remove" button depending on whether anything's slected
			defaultAssociationsView.Selection.Changed += delegate {
				TreeIter iter;
				defaultAssociationsRemoveButton.Sensitive =
					defaultAssociationsView.Selection.GetSelected (out iter);
			};
			defaultAssociationsRemoveButton.Sensitive = false;
		}

		void Build ()
		{
			Spacing = 6;

			registeredSchemasView = new TreeView ();
			defaultAssociationsView = new TreeView ();

			registeredSchemasAddButton = new Button (Stock.Add);
			registeredSchemasRemoveButton = new Button (Stock.Remove);
			defaultAssociationsAddButton = new Button (Stock.Add);
			defaultAssociationsRemoveButton = new Button (Stock.Remove);

			registeredSchemasAddButton.Clicked += addRegisteredSchema;
			registeredSchemasRemoveButton.Clicked += removeRegisteredSchema;
			defaultAssociationsAddButton.Clicked += addFileAssociation;
			defaultAssociationsRemoveButton.Clicked += removeFileAssocation;

			PackStart (new Label { Markup = GettextCatalog.GetString ("<b>Registered Schema</b>"), Xalign = 0 }, false, false, 0);
			var schemasBox = new HBox (false, 6);
			var schemaScroll = new ScrolledWindow { Child = registeredSchemasView, ShadowType = ShadowType.In };
			schemasBox.PackStart (schemaScroll, true, true, 0);
			var schemasButtonBox = new VBox (false, 6);
			schemasButtonBox.PackStart (registeredSchemasAddButton, false, false, 0);
			schemasButtonBox.PackStart (registeredSchemasRemoveButton, false, false, 0);
			schemasBox.PackStart (schemasButtonBox, false, false, 0);
			PackStart (schemasBox, true, true, 0);

			PackStart (new Label (" "), false, false, 0);

			PackStart (new Label { Markup = GettextCatalog.GetString ("<b>Default File Associations</b>"), Xalign = 0 }, false, false, 0);
			var assocBox = new HBox (false, 6);
			var assocScroll = new ScrolledWindow { Child = defaultAssociationsView, ShadowType = ShadowType.In };
			assocBox.PackStart (assocScroll, true, true, 0);
			var assocButtonBox = new VBox (false, 6);
			assocButtonBox.PackStart (defaultAssociationsAddButton, false, false, 0);
			assocButtonBox.PackStart (defaultAssociationsRemoveButton, false, false, 0);
			assocBox.PackStart (assocButtonBox, false, false, 0);
			PackStart (assocBox, true, true, 0);

			ShowAll ();
		}
		
		XmlSchemaCompletionData GetSchema (TreeIter iter)
		{
			return GetSchema (registeredSchemasStore, iter);
		}

		static XmlSchemaCompletionData GetSchema (ListStore registeredSchemasStore, TreeIter iter)
		{
			return (XmlSchemaCompletionData) registeredSchemasStore.GetValue (iter, 0);
		}
		
		IEnumerable<object> WalkStore (TreeModel model, int column)
		{
			foreach (TreeIter iter in WalkStore (model))
				yield return model.GetValue (iter, column);
		}
		
		IEnumerable<TreeIter> WalkStore (TreeModel model)
		{
			TreeIter iter;
			bool valid = model.GetIterFirst (out iter);
			while (valid) {
				yield return iter;
				valid = model.IterNext (ref iter);
			}
		}

		static int SortSchemas (TreeModel model, TreeIter a, TreeIter b)
		{
			var listStore = (ListStore)model;
			return string.Compare (GetSchema (listStore, a).NamespaceUri, GetSchema (listStore, b).NamespaceUri, StringComparison.Ordinal);
		}
		
		#region Schema accessors
		
		public List<XmlSchemaCompletionData> AddedSchemas {
			get { return addedSchemas; }
		}
		
		public List<XmlSchemaCompletionData> RemovedSchemas {
			get { return removedSchemas; }
		}
		
		XmlSchemaCompletionData GetSelectedSchema ()
		{
			TreeIter iter;
			if (registeredSchemasView.Selection.GetSelected (out iter))
				return GetSchema (iter);
			return null;
		}
		
		// Checks that the schema namespace does not already exist.
		XmlSchemaCompletionData GetRegisteredSchema (string namespaceUri)
		{
			foreach (XmlSchemaCompletionData schema in WalkStore (registeredSchemasStore, 0))
				if (schema.NamespaceUri == namespaceUri)
					return schema;
			return null;
		}
		
		XmlSchemaCompletionData GetRegisteredSchema (TreeIter iter)
		{
			return (XmlSchemaCompletionData) registeredSchemasStore.GetValue (iter, 0);
		}
		
		public void LoadUserSchemas (XmlSchemaCompletionDataCollection val)
		{
			//add user schemas
			registeredSchemasStore.Clear ();
			foreach (XmlSchemaCompletionData schema in val)
				AppendSchemaToStore (schema);
			
			//add built-in schemas that aren't overriden by a matching user schema
			foreach (XmlSchemaCompletionData schema in XmlSchemaManager.BuiltinSchemas)
				if (val[schema.NamespaceUri] == null)
					AppendSchemaToStore (schema);
		}
		
		#endregion
		
		public bool IsChanged {
			get {
				if ( addedSchemas.Count > 0 || removedSchemas.Count > 0 || removedExtensions.Count > 0)
					return true;
				
				foreach (TreeIter iter in WalkStore (defaultAssociationsStore))
					if ((bool)defaultAssociationsStore.GetValue (iter, COL_CHANGED))
						return true;
				
				return false;
			}
		}
		
		#region File associations
		
		public IEnumerable<XmlFileAssociation> GetChangedXmlFileAssociations ()
		{
			foreach (var iter in WalkStore (defaultAssociationsStore)) {
				string ext = (string)defaultAssociationsStore.GetValue (iter, COL_EXT);
				if (!string.IsNullOrEmpty (ext) && (bool)defaultAssociationsStore.GetValue (iter, COL_CHANGED)) {
					yield return new XmlFileAssociation (
						ext,
						((string)defaultAssociationsStore.GetValue (iter, COL_NS)) ?? "",
						((string)defaultAssociationsStore.GetValue (iter, COL_PREFIX)) ?? ""
					);
				}
			}
		}
		
		public List<string> RemovedExtensions {
			get { return removedExtensions; }
		}
		
		public void AddFileExtensions (IEnumerable<XmlFileAssociation> assocs)
		{
			foreach (var a in assocs)
				defaultAssociationsStore.AppendValues (a.Extension, a.NamespaceUri, a.NamespacePrefix, false);
		}
		
		void handleExtensionSet (object sender, EditedArgs args)
		{
			//check extension is valid and not a duplicate
			string newval = args.NewText == null? null : args.NewText.ToLowerInvariant ().Trim ();
			if (string.IsNullOrEmpty (newval))
				//FIXME: give an error message?
				return;
			
			foreach (char c in newval)
				if (!char.IsLetterOrDigit (c) && c != '.')
					//FIXME: give an error message?
					return;
			
			foreach (string s in WalkStore (defaultAssociationsStore, COL_EXT)) {
				if (s == newval)
					//FIXME: give an error message?
					return;
			}
			
			setAssocValAndMarkChanged (args.Path, COL_EXT, newval);
		}
		
		void setAssocValAndMarkChanged (string path, int col, object val)
		{
			TreeIter iter;
			if (defaultAssociationsStore.GetIter (out iter, new TreePath (path))) {
				defaultAssociationsStore.SetValue (iter, col, val);
				defaultAssociationsStore.SetValue (iter, COL_CHANGED, true);
			} else {
				throw new Exception ("Could not resolve edited path '" + path +"' to TreeIter at " + Environment.StackTrace);
			}
		}
		
		protected virtual void addFileAssociation (object sender, EventArgs e)
		{
			bool foundExisting = false;
			TreeIter newIter = TreeIter.Zero;
			foreach (TreeIter iter in WalkStore (defaultAssociationsStore)) {
				if (string.IsNullOrEmpty ((string) defaultAssociationsStore.GetValue (iter, COL_EXT))) {
					foundExisting = true;
					newIter = iter;
				}
			}
			if (!foundExisting)
				newIter = defaultAssociationsStore.Append ();
			
			defaultAssociationsView.SetCursor (
			    defaultAssociationsStore.GetPath (newIter),
			    defaultAssociationsView.GetColumn (COL_EXT),
			    true);
		}

		protected virtual void removeFileAssocation (object sender, EventArgs e)
		{
			TreeIter iter;
			if (!defaultAssociationsView.Selection.GetSelected (out iter))
				throw new InvalidOperationException
					("Should not be able to activate removeFileAssocation button while no row is selected.");
			
			string ext = (string) defaultAssociationsStore.GetValue (iter, COL_EXT);
			if (ext != null && ext.Trim ().Length > 0)
				removedExtensions.Add (ext.Trim ());
			defaultAssociationsStore.Remove (ref iter);
		}
		
		#endregion
		
		#region Adding/removing schemas
				
		TreeIter AppendSchemaToStore (XmlSchemaCompletionData schema)
		{
			return registeredSchemasStore.AppendValues (schema);
		}
		
		TreeIter AddRegisteredSchema (XmlSchemaCompletionData schema)
		{
			if (removedSchemas.Contains (schema))
				removedSchemas.Remove (schema);
			else
				addedSchemas.Add (schema);
			
			return AppendSchemaToStore (schema);
		}
		
		void RemoveRegisteredSchema (XmlSchemaCompletionData schema)
		{
			if (addedSchemas.Contains (schema) && !schema.ReadOnly)
				addedSchemas.Remove (schema);
			else
				removedSchemas.Add (schema);
			
			TreeIter iter;
			bool valid = registeredSchemasStore.GetIterFirst (out iter);
			while (valid) {
				if (GetRegisteredSchema (iter) == schema) {
					registeredSchemasStore.Remove (ref iter);
					break;
				}
				valid = registeredSchemasStore.IterNext (ref iter);
			}
			
			//restore built-in schema
			if (!schema.ReadOnly) {
				XmlSchemaCompletionData builtin = XmlSchemaManager.BuiltinSchemas[schema.NamespaceUri];
				if (builtin != null)
					AppendSchemaToStore (builtin);
			}
		}
		
		protected virtual void removeRegisteredSchema (object sender, EventArgs e)
		{
			XmlSchemaCompletionData schema = GetSelectedSchema ();
			if (schema == null)
				throw new InvalidOperationException ("Should not be able to activate removeRegisteredSchema button while no row is selected.");
			
			RemoveRegisteredSchema (schema);
		}

		protected virtual void addRegisteredSchema (object sender, EventArgs args)
		{
			string fileName = XmlEditorService.BrowseForSchemaFile ();

			// We need to present the window so that the keyboard focus returns to the correct parent window
			((Gtk.Window)Toplevel).Present();

			if (string.IsNullOrEmpty (fileName))
				return;
			
			string shortName = System.IO.Path.GetFileName (fileName);
			
			//load the schema
			XmlSchemaCompletionData schema = null;
			try {
				schema = new XmlSchemaCompletionData (fileName);
			} catch (Exception ex) {
				string msg = GettextCatalog.GetString ("Schema '{0}' could not be loaded.", shortName);
				MessageService.ShowError (msg, ex);
				return;
			}
			
			// Make sure the schema has a target namespace.
			if (schema.NamespaceUri == null) {
				MessageService.ShowError (
				    GettextCatalog.GetString ("Schema '{0}' has no target namespace.", shortName));
				return;
			}
			
			//if namaspace conflict, ask user whether they want to replace existing schema
			XmlSchemaCompletionData oldSchema = GetRegisteredSchema (schema.NamespaceUri);
			if (oldSchema != null) {
				bool replace = MessageService.Confirm (
				    GettextCatalog.GetString (
				        "A schema is already registered with the namespace '{0}'. Would you like to replace it?",
				        schema.NamespaceUri),
				    new AlertButton (GettextCatalog.GetString ("Replace"))
				);
				if (!replace)
					return;
				
				//remove the old schema
				RemoveRegisteredSchema (oldSchema);
			}
			
			// Store the schema so we can add it for real later, if the "ok" button's clicked
			TreeIter newIter = AddRegisteredSchema (schema);
			registeredSchemasView.Selection.SelectIter (newIter);
			ScrollToSelection (registeredSchemasView);
		}
		
		
		/// <summary>
		/// Scrolls the list so the specified item is visible.
		/// </summary>
		void ScrollToSelection (TreeView view)
		{
			TreeIter iter;
			TreeModel model;
			if (!registeredSchemasView.Selection.GetSelected (out model, out iter))
				return;
			view.ScrollToCell (model.GetPath (iter), null, false, 0, 0);
		}
		
		#endregion
		
		static int COL_EXT = 0;
		static int COL_NS = 1;
		static int COL_PREFIX = 2;
		static int COL_CHANGED = 3;
	}
}
